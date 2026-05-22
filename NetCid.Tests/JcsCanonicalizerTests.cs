using System.Buffers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace NetCid.Tests;

public sealed class JcsCanonicalizerTests
{
    private static string Canon(JsonNode? node)
        => Encoding.UTF8.GetString(JcsCanonicalizer.Canonicalize(node));

    private static string Canon(string json)
        => Encoding.UTF8.GetString(JcsCanonicalizer.Canonicalize(JsonNode.Parse(json)));

    private static string CanonElement(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return Encoding.UTF8.GetString(JcsCanonicalizer.Canonicalize(doc.RootElement));
    }

    [Fact]
    public void Null_Reference_Serializes_As_Null_Literal()
    {
        Assert.Equal("null", Canon((JsonNode?)null));
    }

    [Fact]
    public void Primitive_True_False_Null()
    {
        Assert.Equal("true", Canon(JsonValue.Create(true)));
        Assert.Equal("false", Canon(JsonValue.Create(false)));
        Assert.Equal("null", Canon("null"));
    }

    [Fact]
    public void Object_Keys_Sort_By_Code_Unit_Pure_Ascii()
    {
        Assert.Equal(
            "{\"a\":2,\"b\":1}",
            Canon("{\"b\":1,\"a\":2}"));
    }

    [Fact]
    public void Object_Keys_Sort_By_Code_Unit_Mixed_Case()
    {
        // 'A'(0x41) < 'a'(0x61) by UTF-16 code unit, so uppercase keys come first.
        Assert.Equal(
            "{\"A\":1,\"a\":2,\"b\":3}",
            Canon("{\"b\":3,\"a\":2,\"A\":1}"));
    }

    [Fact]
    public void Object_Keys_Sort_Is_Independent_At_Each_Nesting_Level()
    {
        Assert.Equal(
            "{\"a\":{\"x\":1,\"y\":2},\"b\":[3,2,1]}",
            Canon("{\"b\":[3,2,1],\"a\":{\"y\":2,\"x\":1}}"));
    }

    [Fact]
    public void Arrays_Preserve_Order()
    {
        // RFC 8785 §3.2.2.1: arrays keep insertion order.
        Assert.Equal("[3,1,2]", Canon("[3,1,2]"));
    }

    [Fact]
    public void Whitespace_Is_Stripped()
    {
        Assert.Equal(
            "{\"a\":1,\"b\":[1,2]}",
            Canon("  { \"b\" : [ 1 , 2 ] , \"a\" : 1 }  "));
    }

    [Fact]
    public void Integer_Numbers_Strip_To_Plain_Digits()
    {
        Assert.Equal("0", Canon("0"));
        Assert.Equal("1", Canon("1"));
        Assert.Equal("-1", Canon("-1"));
        Assert.Equal("9007199254740992", Canon("9007199254740992"));  // > 2^53, still a valid long
    }

    [Fact]
    public void Negative_Zero_Normalises_To_Zero()
    {
        // RFC 8785 §3.2.2.3 references the ECMAScript ToString algorithm, which collapses -0 to "0".
        Assert.Equal("0", Canon("-0"));
    }

    [Fact]
    public void Large_Positive_Integer_Uses_UInt64()
    {
        // Beyond long.MaxValue (9223372036854775807) but within ulong range.
        Assert.Equal("18446744073709551615", Canon("18446744073709551615"));  // ulong.MaxValue
    }

    [Fact]
    public void String_With_Seven_Short_Escapes()
    {
        var node = JsonValue.Create("\"\\\b\f\n\r\t");
        Assert.Equal("\"\\\"\\\\\\b\\f\\n\\r\\t\"", Canon(node));
    }

    [Fact]
    public void String_With_Control_Bytes_Uses_LowercaseHex_u00XX()
    {
        // U+0001 → \u0001, U+001F → \u001f (lowercase hex per JCS §3.2.2.2).
        var node = JsonValue.Create("\u0001\u001F");
        Assert.Equal("\"\\u0001\\u001f\"", Canon(node));
    }

    [Fact]
    public void String_With_Solidus_Is_NOT_Escaped()
    {
        Assert.Equal("\"/path/to\"", Canon(JsonValue.Create("/path/to")));
    }

    [Fact]
    public void String_With_Characters_Above_U007F_Stays_Raw_Utf8()
    {
        // "é" is U+00E9, UTF-8 bytes C3 A9. JCS forbids \u00E9 escaping above U+007F.
        var bytes = JcsCanonicalizer.Canonicalize(JsonValue.Create("é"));
        Assert.Equal(new byte[] { 0x22, 0xC3, 0xA9, 0x22 }, bytes);
    }

    [Fact]
    public void String_With_Emoji_Surrogate_Pair_Becomes_4_Byte_Utf8()
    {
        // U+1F600 grinning face. UTF-8: F0 9F 98 80.
        var bytes = JcsCanonicalizer.Canonicalize(JsonValue.Create("\uD83D\uDE00"));
        Assert.Equal(new byte[] { 0x22, 0xF0, 0x9F, 0x98, 0x80, 0x22 }, bytes);
    }

    [Fact]
    public void Audit_Entry_Example_Matches_Expected_Canonical_Form()
    {
        // Representative of net-wallet-mcp audit log entries (issue #9).
        var entry = new JsonObject
        {
            ["seq"] = 1,
            ["ts"] = "2026-05-21T18:00:00Z",
            ["op"] = "wallet.mint_identity",
            ["identity_id"] = "id_42",
            ["params_hash"] = "bafyparams",
            ["result_hash"] = "bafyresult",
            ["prev_cid"] = "genesis",
        };

        const string expected =
            "{\"identity_id\":\"id_42\",\"op\":\"wallet.mint_identity\","
            + "\"params_hash\":\"bafyparams\",\"prev_cid\":\"genesis\","
            + "\"result_hash\":\"bafyresult\",\"seq\":1,"
            + "\"ts\":\"2026-05-21T18:00:00Z\"}";

        Assert.Equal(expected, Canon(entry));
    }

    [Fact]
    public void Idempotence_Reparsing_Canonical_Form_Reproduces_Same_Bytes()
    {
        var node = JsonNode.Parse("{\"b\":[3,1,2],\"a\":{\"y\":\"é\",\"x\":1}}");
        var first = JcsCanonicalizer.Canonicalize(node);
        var second = JcsCanonicalizer.Canonicalize(JsonNode.Parse(Encoding.UTF8.GetString(first)));
        Assert.Equal(first, second);
    }

    [Fact]
    public void JsonElement_Overload_Matches_JsonNode_Overload()
    {
        const string raw = "{\"b\":2,\"a\":{\"y\":2,\"x\":1}}";
        var fromNode = JcsCanonicalizer.Canonicalize(JsonNode.Parse(raw));
        var fromElement = Encoding.UTF8.GetBytes(CanonElement(raw));
        Assert.Equal(fromNode, fromElement);
    }

    [Fact]
    public void IBufferWriter_Overload_Writes_Same_Bytes_As_Array_Overload()
    {
        var node = JsonNode.Parse("{\"b\":2,\"a\":1}");
        var expected = JcsCanonicalizer.Canonicalize(node);

        var writer = new ArrayBufferWriter<byte>();
        JcsCanonicalizer.Canonicalize(node, writer);

        Assert.Equal(expected, writer.WrittenSpan.ToArray());
    }

    [Fact]
    public void IBufferWriter_Overload_Rejects_Null_Destination()
    {
        Assert.Throws<ArgumentNullException>(
            () => JcsCanonicalizer.Canonicalize((JsonNode?)null, null!));
    }

    [Fact]
    public void NaN_Throws_JcsFormatException()
    {
        var node = JsonValue.Create(double.NaN);
        var ex = Assert.Throws<JcsFormatException>(() => JcsCanonicalizer.Canonicalize(node));
        Assert.Contains("NaN", ex.Message);
    }

    [Fact]
    public void Positive_Infinity_Throws_JcsFormatException()
    {
        var node = JsonValue.Create(double.PositiveInfinity);
        var ex = Assert.Throws<JcsFormatException>(() => JcsCanonicalizer.Canonicalize(node));
        Assert.Contains("infinity", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Negative_Infinity_Throws_JcsFormatException()
    {
        var node = JsonValue.Create(double.NegativeInfinity);
        var ex = Assert.Throws<JcsFormatException>(() => JcsCanonicalizer.Canonicalize(node));
        Assert.Contains("infinity", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Fractional_Number_Throws_JcsFormatException_With_Followup_Hint()
    {
        var ex = Assert.Throws<JcsFormatException>(() => Canon("1.5"));
        Assert.Contains("fractional", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Exponential_Number_Throws_JcsFormatException()
    {
        Assert.Throws<JcsFormatException>(() => Canon("1e10"));
    }

    [Fact]
    public void Integer_Outside_UInt64_Range_Throws_With_Range_Hint()
    {
        // 21 digits > ulong.MaxValue (20 digits).
        var ex = Assert.Throws<JcsFormatException>(() => Canon("184467440737095516160"));
        Assert.Contains("range", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Integer_Below_Int64_Minimum_Throws_With_Range_Hint()
    {
        // long.MinValue is -9223372036854775808; this is one less than that.
        var ex = Assert.Throws<JcsFormatException>(() => Canon("-9223372036854775809"));
        Assert.Contains("range", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Empty_Object_And_Empty_Array_Canonicalise()
    {
        Assert.Equal("{}", Canon("{}"));
        Assert.Equal("[]", Canon("[]"));
    }

    [Fact]
    public void Empty_String_Canonicalises()
    {
        Assert.Equal("\"\"", Canon(JsonValue.Create("")));
    }

    [Fact]
    public void Large_String_Above_Stack_Threshold_Uses_Pool_And_Still_Canonicalises()
    {
        // Force the ArrayPool path (string > 256 bytes UTF-8).
        var big = new string('a', 1024);
        var expected = "\"" + big + "\"";
        Assert.Equal(expected, Canon(JsonValue.Create(big)));
    }
}
