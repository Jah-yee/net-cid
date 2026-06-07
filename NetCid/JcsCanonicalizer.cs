using System.Buffers;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace NetCid;

/// <summary>
/// JSON Canonicalization Scheme (JCS) per
/// <see href="https://www.rfc-editor.org/rfc/rfc8785">RFC 8785</see>.
/// Produces a deterministic byte-for-byte stable UTF-8 serialization of a JSON value
/// so that two writers of the same logical value produce identical bytes — and therefore
/// identical CIDs, hashes, or signatures.
/// </summary>
/// <remarks>
/// <para>
/// Supports objects, arrays, strings, IEEE-754 double-precision numbers (per ECMA-262
/// §6.1.6.1.20, as RFC 8785 §3.2.2.3 requires), <c>true</c>, <c>false</c>, and <c>null</c>.
/// <c>NaN</c> and <c>±infinity</c> throw <see cref="JcsFormatException"/>.
/// </para>
/// <para>Thread-safe — all members are static and stateless.</para>
/// </remarks>
public static class JcsCanonicalizer
{
    private const int StackBufferThreshold = 256;

    /// <summary>
    /// Canonicalize <paramref name="node"/> per RFC 8785. A <see langword="null"/>
    /// reference is treated as the JSON literal <c>null</c>.
    /// </summary>
    /// <returns>UTF-8 encoded canonical JSON bytes.</returns>
    /// <exception cref="JcsFormatException">
    /// The node contains a value JCS cannot represent deterministically — <c>NaN</c>
    /// or <c>±infinity</c>.
    /// </exception>
    public static byte[] Canonicalize(JsonNode? node)
    {
        var writer = new ArrayBufferWriter<byte>();
        Canonicalize(node, writer);
        return writer.WrittenSpan.ToArray();
    }

    /// <summary>
    /// Canonicalize a <see cref="JsonElement"/> per RFC 8785.
    /// </summary>
    /// <returns>UTF-8 encoded canonical JSON bytes.</returns>
    /// <exception cref="JcsFormatException">
    /// The element contains a value JCS cannot represent deterministically.
    /// </exception>
    public static byte[] Canonicalize(JsonElement element)
    {
        var writer = new ArrayBufferWriter<byte>();
        WriteElement(element, writer);
        return writer.WrittenSpan.ToArray();
    }

    /// <summary>
    /// Canonicalize <paramref name="node"/> and write the bytes directly into
    /// <paramref name="destination"/>. Useful for hashing pipelines that do not want
    /// an intermediate <c>byte[]</c> allocation.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="destination"/> is <see langword="null"/>.</exception>
    /// <exception cref="JcsFormatException">
    /// The node contains a value JCS cannot represent deterministically.
    /// </exception>
    public static void Canonicalize(JsonNode? node, IBufferWriter<byte> destination)
    {
        ArgumentNullException.ThrowIfNull(destination);

        if (node is null)
        {
            WriteRaw(destination, "null"u8);
            return;
        }

        // JsonNode may wrap raw .NET doubles/floats holding NaN/±infinity. The default
        // System.Text.Json serializer would throw a generic JsonException; surface a
        // JCS-specific message instead so callers can handle JcsFormatException uniformly.
        ValidateNoUnrepresentableNumbers(node);

        JsonDocument document;
        try
        {
            document = JsonSerializer.SerializeToDocument(node);
        }
        catch (JsonException ex)
        {
            throw new JcsFormatException(
                "JSON value cannot be serialized for canonicalization.", ex);
        }

        using (document)
        {
            WriteElement(document.RootElement, destination);
        }
    }

    private static void ValidateNoUnrepresentableNumbers(JsonNode node)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var (_, child) in obj)
                {
                    if (child is not null)
                    {
                        ValidateNoUnrepresentableNumbers(child);
                    }
                }
                break;

            case JsonArray arr:
                foreach (var item in arr)
                {
                    if (item is not null)
                    {
                        ValidateNoUnrepresentableNumbers(item);
                    }
                }
                break;

            case JsonValue val:
                if (val.TryGetValue<double>(out var d))
                {
                    if (double.IsNaN(d))
                    {
                        throw new JcsFormatException("JCS cannot represent NaN (RFC 8785 §3.2.2.3).");
                    }
                    if (double.IsPositiveInfinity(d))
                    {
                        throw new JcsFormatException("JCS cannot represent +infinity (RFC 8785 §3.2.2.3).");
                    }
                    if (double.IsNegativeInfinity(d))
                    {
                        throw new JcsFormatException("JCS cannot represent -infinity (RFC 8785 §3.2.2.3).");
                    }
                }
                else if (val.TryGetValue<float>(out var f))
                {
                    if (float.IsNaN(f))
                    {
                        throw new JcsFormatException("JCS cannot represent NaN (RFC 8785 §3.2.2.3).");
                    }
                    if (float.IsPositiveInfinity(f))
                    {
                        throw new JcsFormatException("JCS cannot represent +infinity (RFC 8785 §3.2.2.3).");
                    }
                    if (float.IsNegativeInfinity(f))
                    {
                        throw new JcsFormatException("JCS cannot represent -infinity (RFC 8785 §3.2.2.3).");
                    }
                }
                break;
        }
    }

    private static void WriteElement(JsonElement element, IBufferWriter<byte> writer)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                WriteObject(element, writer);
                break;
            case JsonValueKind.Array:
                WriteArray(element, writer);
                break;
            case JsonValueKind.String:
                WriteString(element.GetString()!, writer);
                break;
            case JsonValueKind.Number:
                WriteNumber(element, writer);
                break;
            case JsonValueKind.True:
                WriteRaw(writer, "true"u8);
                break;
            case JsonValueKind.False:
                WriteRaw(writer, "false"u8);
                break;
            case JsonValueKind.Null:
                WriteRaw(writer, "null"u8);
                break;
            case JsonValueKind.Undefined:
            default:
                throw new JcsFormatException(
                    $"JCS cannot canonicalize JSON value of kind '{element.ValueKind}'.");
        }
    }

    private static void WriteObject(JsonElement obj, IBufferWriter<byte> writer)
    {
        WriteByte(writer, (byte)'{');

        // RFC 8785 §3.2.3: object member names are sorted by UTF-16 code unit order,
        // which is exactly what string.CompareOrdinal compares.
        var properties = new List<JsonProperty>();
        foreach (var prop in obj.EnumerateObject())
        {
            properties.Add(prop);
        }

        properties.Sort(static (a, b) => string.CompareOrdinal(a.Name, b.Name));

        for (var i = 0; i < properties.Count; i++)
        {
            if (i > 0)
            {
                WriteByte(writer, (byte)',');
            }

            WriteString(properties[i].Name, writer);
            WriteByte(writer, (byte)':');
            WriteElement(properties[i].Value, writer);
        }

        WriteByte(writer, (byte)'}');
    }

    private static void WriteArray(JsonElement arr, IBufferWriter<byte> writer)
    {
        WriteByte(writer, (byte)'[');

        var first = true;
        foreach (var item in arr.EnumerateArray())
        {
            if (!first)
            {
                WriteByte(writer, (byte)',');
            }

            first = false;
            WriteElement(item, writer);
        }

        WriteByte(writer, (byte)']');
    }

    // RFC 8785 §3.2.2.3 requires every JSON number be quantized to an IEEE-754
    // double before ECMA-262 §6.1.6.1.20 (Number::toString) runs. Integer literals
    // with |x| ≤ 2^53 are exactly representable as doubles, so writing the integer
    // text directly produces the same bytes as the full helper without a float
    // round-trip. Above that boundary the integer text and the rounded double's
    // canonical form diverge — e.g. `9007199254740993` rounds to `9007199254740992`
    // — so we MUST fall through to the helper to keep two encoders byte-identical.
    private const long MaxSafeInteger = 9007199254740992L;
    private const ulong MaxSafeIntegerUnsigned = 9007199254740992UL;

    private static void WriteNumber(JsonElement num, IBufferWriter<byte> writer)
    {
        if (num.TryGetInt64(out var l) && l >= -MaxSafeInteger && l <= MaxSafeInteger)
        {
            WriteAscii(writer, l.ToString(CultureInfo.InvariantCulture));
            return;
        }

        if (num.TryGetUInt64(out var ul) && ul <= MaxSafeIntegerUnsigned)
        {
            WriteAscii(writer, ul.ToString(CultureInfo.InvariantCulture));
            return;
        }

        // Anything else — fractional, exponential, or an integer beyond ±2^53 — goes
        // through the IEEE-754 path. Integer literals so large they parse to ±∞ trip
        // the helper's defensive infinity check and throw with the documented message.
        var d = num.GetDouble();
        WriteAscii(writer, EcmaScriptNumber.ToCanonicalString(d));
    }

    private static void WriteString(string value, IBufferWriter<byte> writer)
    {
        WriteByte(writer, (byte)'"');

        // Encode the entire string to UTF-8 once, then walk the bytes. UTF-8 multibyte
        // continuation bytes all have the high bit set (>= 0x80), so the per-byte escape
        // table only acts on single-byte ASCII characters — multibyte sequences pass
        // through verbatim, which is exactly what JCS §3.2.2.2 requires.
        var maxBytes = Encoding.UTF8.GetMaxByteCount(value.Length);

        byte[]? rented = null;
        Span<byte> utf8 = maxBytes <= StackBufferThreshold
            ? stackalloc byte[StackBufferThreshold]
            : (rented = ArrayPool<byte>.Shared.Rent(maxBytes));

        try
        {
            var written = Encoding.UTF8.GetBytes(value, utf8);
            WriteEscapedUtf8(writer, utf8[..written]);
        }
        finally
        {
            if (rented is not null)
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }

        WriteByte(writer, (byte)'"');
    }

    private static void WriteEscapedUtf8(IBufferWriter<byte> writer, ReadOnlySpan<byte> utf8)
    {
        // Walk the UTF-8 bytes and escape only what RFC 8785 §3.2.2.2 strictly requires:
        // the seven short escapes (\" \\ \b \t \n \f \r) and \u00XX for control bytes
        // below U+0020. Solidus is NOT escaped. Bytes >= 0x80 are emitted as raw UTF-8.
        Span<byte> u = stackalloc byte[6] { (byte)'\\', (byte)'u', (byte)'0', (byte)'0', 0, 0 };

        var start = 0;
        for (var i = 0; i < utf8.Length; i++)
        {
            var b = utf8[i];

            ReadOnlySpan<byte> escape;
            switch (b)
            {
                case 0x22: escape = "\\\""u8; break;
                case 0x5C: escape = "\\\\"u8; break;
                case 0x08: escape = "\\b"u8; break;
                case 0x09: escape = "\\t"u8; break;
                case 0x0A: escape = "\\n"u8; break;
                case 0x0C: escape = "\\f"u8; break;
                case 0x0D: escape = "\\r"u8; break;
                default:
                    if (b < 0x20)
                    {
                        if (i > start)
                        {
                            WriteRaw(writer, utf8.Slice(start, i - start));
                        }

                        u[4] = HexNybble((b >> 4) & 0xF);
                        u[5] = HexNybble(b & 0xF);
                        WriteRaw(writer, u);
                        start = i + 1;
                    }
                    continue;
            }

            if (i > start)
            {
                WriteRaw(writer, utf8.Slice(start, i - start));
            }
            WriteRaw(writer, escape);
            start = i + 1;
        }

        if (start < utf8.Length)
        {
            WriteRaw(writer, utf8[start..]);
        }
    }

    private static byte HexNybble(int n) => (byte)(n < 10 ? '0' + n : 'a' + (n - 10));

    private static void WriteByte(IBufferWriter<byte> writer, byte b)
    {
        var span = writer.GetSpan(1);
        span[0] = b;
        writer.Advance(1);
    }

    private static void WriteRaw(IBufferWriter<byte> writer, ReadOnlySpan<byte> bytes)
    {
        var span = writer.GetSpan(bytes.Length);
        bytes.CopyTo(span);
        writer.Advance(bytes.Length);
    }

    private static void WriteAscii(IBufferWriter<byte> writer, string ascii)
    {
        var span = writer.GetSpan(ascii.Length);
        for (var i = 0; i < ascii.Length; i++)
        {
            span[i] = (byte)ascii[i];
        }

        writer.Advance(ascii.Length);
    }
}
