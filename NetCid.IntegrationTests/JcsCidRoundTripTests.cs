using System.Text.Json.Nodes;

namespace NetCid.IntegrationTests;

public sealed class JcsCidRoundTripTests
{
    [Fact]
    public void FromCanonicalJson_Equals_FromContent_Over_Canonical_Bytes()
    {
        var node = new JsonObject
        {
            ["seq"] = 1,
            ["op"] = "wallet.mint_identity",
            ["params_hash"] = "bafyparams",
            ["prev_cid"] = "genesis",
        };

        var convenience = Cid.FromCanonicalJson(node);

        var canonical = JcsCanonicalizer.Canonicalize(node);
        var twoStep = Cid.FromContent(canonical, Multicodec.Raw, MultihashCode.Sha2_256);

        Assert.Equal(twoStep, convenience);
        Assert.Equal(CidVersion.V1, convenience.Version);
        Assert.Equal(Multicodec.Raw, convenience.Codec);
    }

    [Fact]
    public void Two_Writers_Of_Same_Logical_Value_Produce_Same_Cid()
    {
        // Different key insertion orders, same logical object → identical CID.
        var writerA = new JsonObject
        {
            ["b"] = 2,
            ["a"] = new JsonObject { ["y"] = 2, ["x"] = 1 },
        };

        var writerB = new JsonObject
        {
            ["a"] = new JsonObject { ["x"] = 1, ["y"] = 2 },
            ["b"] = 2,
        };

        var cidA = Cid.FromCanonicalJson(writerA);
        var cidB = Cid.FromCanonicalJson(writerB);

        Assert.Equal(cidA, cidB);
    }

    [Fact]
    public void FromCanonicalJson_Honours_Custom_Codec_And_HashCode()
    {
        var node = new JsonObject { ["k"] = "v" };

        var cid = Cid.FromCanonicalJson(node, Multicodec.DagJson, MultihashCode.Sha2_512);

        Assert.Equal(Multicodec.DagJson, cid.Codec);
        Assert.Equal(MultihashCode.Sha2_512, cid.Multihash.Code);
        Assert.Equal(64, cid.Multihash.DigestLength);
    }
}
