namespace NetCid;

/// <summary>
/// Helpers for the W3C Controlled Identifiers 1.0 Multikey verification method
/// and the equivalent <c>did:key</c> identifier encoding:
/// <c>publicKeyMultibase = base58btc( varint(keyCodec) || rawPublicKey )</c>.
/// </summary>
public static class Multikey
{
    /// <summary>
    /// Encode a raw public key of the given key-type multicodec as a Multikey
    /// <c>publicKeyMultibase</c> string (base58btc, leading <c>'z'</c>).
    /// </summary>
    /// <param name="keyCodec">One of the eight supported key-type multicodecs.</param>
    /// <param name="rawKey">Raw public-key bytes. Edwards/Montgomery curves use their raw
    /// 32-byte encoding; NIST P-curves and secp256k1 use compressed-point form; BLS keys
    /// use their canonical 48-/96-byte serialization.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="keyCodec"/> is not a supported key-type multicodec, or
    /// <paramref name="rawKey"/> has the wrong length for that codec.
    /// </exception>
    public static string Encode(ulong keyCodec, ReadOnlySpan<byte> rawKey)
    {
        EnsureKnownKeyType(keyCodec, nameof(keyCodec));
        EnsureKeyLength(keyCodec, rawKey.Length, nameof(rawKey));
        var prefixed = Multicodec.Prefix(keyCodec, rawKey);
        return Multibase.Encode(prefixed, MultibaseEncoding.Base58Btc, includePrefix: true);
    }

    /// <summary>
    /// Decode a Multikey <c>publicKeyMultibase</c> string into its key-type multicodec
    /// and raw public key. Validates the multibase prefix, multicodec category, and
    /// per-codec key length.
    /// </summary>
    /// <exception cref="CidFormatException">The input is not a valid Multikey value.</exception>
    public static (ulong KeyCodec, byte[] RawKey) Decode(string publicKeyMultibase)
    {
        if (!TryDecode(publicKeyMultibase, out var codec, out var raw))
        {
            throw new CidFormatException("Invalid publicKeyMultibase value.");
        }

        return (codec, raw!);
    }

    /// <summary>
    /// Try to decode a Multikey <c>publicKeyMultibase</c> string. Returns <see langword="false"/>
    /// for any non-base58btc multibase, any non-key-type multicodec, or any payload whose raw
    /// length does not match the codec.
    /// </summary>
    public static bool TryDecode(string publicKeyMultibase, out ulong keyCodec, out byte[]? rawKey)
    {
        keyCodec = 0;
        rawKey = null;

        if (!Multibase.TryDecode(publicKeyMultibase, out var bytes, out var encoding))
        {
            return false;
        }

        if (encoding != MultibaseEncoding.Base58Btc)
        {
            return false;
        }

        if (!Multicodec.TryDecode(bytes, out var codec, out var raw))
        {
            return false;
        }

        if (!IsKnownKeyType(codec))
        {
            return false;
        }

        if (raw is null || !KeyLengthMatches(codec, raw.Length))
        {
            return false;
        }

        keyCodec = codec;
        rawKey = raw;
        return true;
    }

    /// <summary>Expected raw-key byte length for a supported Multikey codec, or -1 if unknown.</summary>
    private static int ExpectedLength(ulong codec) => codec switch
    {
        Multicodec.Ed25519Pub => 32,
        Multicodec.X25519Pub => 32,
        Multicodec.Secp256k1Pub => 33,
        Multicodec.P256Pub => 33,
        Multicodec.P384Pub => 49,
        Multicodec.P521Pub => 67,
        Multicodec.Bls12381G1Pub => 48,
        Multicodec.Bls12381G2Pub => 96,
        _ => -1,
    };

    private static bool IsKnownKeyType(ulong codec) => ExpectedLength(codec) >= 0;

    private static bool KeyLengthMatches(ulong codec, int length) => ExpectedLength(codec) == length;

    private static void EnsureKnownKeyType(ulong codec, string paramName)
    {
        if (IsKnownKeyType(codec))
        {
            return;
        }

        var name = Multicodec.TryGetName(codec, out var n) ? $"{n} (0x{codec:X})" : $"0x{codec:X}";
        throw new ArgumentException(
            $"Multicodec {name} is not a supported Multikey key type. " +
            "Supported codecs: ed25519-pub, x25519-pub, secp256k1-pub, p256-pub, p384-pub, p521-pub, bls12_381-g1-pub, bls12_381-g2-pub.",
            paramName);
    }

    private static void EnsureKeyLength(ulong codec, int actual, string paramName)
    {
        var expected = ExpectedLength(codec);
        if (expected == actual)
        {
            return;
        }

        Multicodec.TryGetName(codec, out var name);
        throw new ArgumentException(
            $"Raw key length {actual} does not match the expected {expected} bytes for {name ?? $"0x{codec:X}"}.",
            paramName);
    }
}
