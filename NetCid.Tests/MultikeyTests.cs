namespace NetCid.Tests;

public sealed class MultikeyTests
{
    public static TheoryData<ulong, int> KeyCodecsAndLengths => new()
    {
        { Multicodec.Ed25519Pub, 32 },
        { Multicodec.X25519Pub, 32 },
        { Multicodec.Secp256k1Pub, 33 },
        { Multicodec.P256Pub, 33 },
        { Multicodec.P384Pub, 49 },
        { Multicodec.P521Pub, 67 },
        { Multicodec.Bls12381G1Pub, 48 },
        { Multicodec.Bls12381G2Pub, 96 },
    };

    [Theory]
    [MemberData(nameof(KeyCodecsAndLengths))]
    public void Encode_Decode_RoundTrips_ForEveryKeyCodec(ulong codec, int expectedLength)
    {
        var rawKey = DeterministicKey(expectedLength);

        var multibase = Multikey.Encode(codec, rawKey);

        Assert.StartsWith("z", multibase, StringComparison.Ordinal);

        var (decodedCodec, decodedKey) = Multikey.Decode(multibase);
        Assert.Equal(codec, decodedCodec);
        Assert.Equal(rawKey, decodedKey);
    }

    [Theory]
    [MemberData(nameof(KeyCodecsAndLengths))]
    public void TryDecode_Returns_True_ForRoundTrippedEncoding(ulong codec, int expectedLength)
    {
        var rawKey = DeterministicKey(expectedLength);
        var multibase = Multikey.Encode(codec, rawKey);

        Assert.True(Multikey.TryDecode(multibase, out var decodedCodec, out var decodedKey));
        Assert.Equal(codec, decodedCodec);
        Assert.Equal(rawKey, decodedKey);
    }

    [Fact]
    public void Encode_Throws_OnUnknownCodec()
    {
        var ex = Assert.Throws<ArgumentException>(
            () => Multikey.Encode(Multicodec.Raw, new byte[32]));

        Assert.Equal("keyCodec", ex.ParamName);
    }

    [Fact]
    public void Encode_Throws_OnContentCodecDagPb()
    {
        Assert.Throws<ArgumentException>(
            () => Multikey.Encode(Multicodec.DagPb, new byte[32]));
    }

    [Theory]
    [MemberData(nameof(KeyCodecsAndLengths))]
    public void Encode_Throws_OnWrongKeyLength(ulong codec, int expectedLength)
    {
        var wrong = new byte[expectedLength + 1];

        var ex = Assert.Throws<ArgumentException>(
            () => Multikey.Encode(codec, wrong));

        Assert.Equal("rawKey", ex.ParamName);
    }

    [Fact]
    public void TryDecode_Returns_False_OnWrongLengthPayload()
    {
        // Build base58btc(varint(Ed25519Pub) || 16-byte payload) — codec is valid, length is not.
        var prefixed = Multicodec.Prefix(Multicodec.Ed25519Pub, new byte[16]);
        var multibase = Multibase.Encode(prefixed, MultibaseEncoding.Base58Btc, includePrefix: true);

        Assert.False(Multikey.TryDecode(multibase, out var codec, out var raw));
        Assert.Equal(0UL, codec);
        Assert.Null(raw);
    }

    [Fact]
    public void TryDecode_Returns_False_OnNonBase58Multibase()
    {
        // Same prefixed bytes, but encoded as base32 ('b' prefix) instead of base58btc.
        var prefixed = Multicodec.Prefix(Multicodec.Ed25519Pub, DeterministicKey(32));
        var base32 = Multibase.Encode(prefixed, MultibaseEncoding.Base32Lower, includePrefix: true);

        Assert.False(Multikey.TryDecode(base32, out _, out _));
    }

    [Fact]
    public void TryDecode_Returns_False_OnBase64UrlMultibase()
    {
        // Multikey is base58btc only — base64url ('u') must be rejected even with a valid key payload.
        var prefixed = Multicodec.Prefix(Multicodec.Ed25519Pub, DeterministicKey(32));
        var base64url = Multibase.Encode(prefixed, MultibaseEncoding.Base64Url, includePrefix: true);

        Assert.False(Multikey.TryDecode(base64url, out _, out _));
    }

    [Fact]
    public void TryDecode_Returns_False_OnContentCodecPrefix()
    {
        // Multicodec.Raw (0x55) is a content codec, not a key type. Reject even with a 32-byte payload.
        var prefixed = Multicodec.Prefix(Multicodec.Raw, DeterministicKey(32));
        var multibase = Multibase.Encode(prefixed, MultibaseEncoding.Base58Btc, includePrefix: true);

        Assert.False(Multikey.TryDecode(multibase, out _, out _));
    }

    [Fact]
    public void TryDecode_Returns_False_OnEmptyOrNullInput()
    {
        Assert.False(Multikey.TryDecode(string.Empty, out _, out _));
        Assert.False(Multikey.TryDecode(null!, out _, out _));
    }

    [Fact]
    public void Decode_Throws_CidFormatException_OnInvalidInput()
    {
        Assert.Throws<CidFormatException>(() => Multikey.Decode("not-a-multikey"));
    }

    [Fact]
    public void Encode_ProducesMultikeySignaturePrefix_ForEd25519()
    {
        // Every 32-byte Ed25519 publicKeyMultibase begins with "z6Mk" because the
        // varint multicodec prefix is [0xED, 0x01] for ed25519-pub (0xED).
        var rawKey = DeterministicKey(32);
        var multibase = Multikey.Encode(Multicodec.Ed25519Pub, rawKey);

        Assert.StartsWith("z6Mk", multibase, StringComparison.Ordinal);
    }

    [Fact]
    public void Decode_KnownEd25519Vector_RoundTripsTo32ByteKey()
    {
        // Self-contained known vector: deterministic 32-byte raw key encoded with the new API,
        // decoded with TryDecode, then asserted byte-equal. Catches any prefix-handling regression.
        var rawKey = new byte[32];
        for (var i = 0; i < rawKey.Length; i++)
        {
            rawKey[i] = (byte)i;
        }

        var multibase = Multikey.Encode(Multicodec.Ed25519Pub, rawKey);

        Assert.True(Multikey.TryDecode(multibase, out var codec, out var decoded));
        Assert.Equal(Multicodec.Ed25519Pub, codec);
        Assert.NotNull(decoded);
        Assert.Equal(32, decoded!.Length);
        Assert.Equal(rawKey, decoded);
    }

    private static byte[] DeterministicKey(int length)
    {
        var bytes = new byte[length];
        for (var i = 0; i < length; i++)
        {
            bytes[i] = (byte)(i + 1);
        }

        return bytes;
    }
}
