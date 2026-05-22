using System.Buffers;
using System.Text;
using System.Text.Json.Nodes;
using NetCid;

// 1. Stable serialization — two writers with different key insertion orders
//    produce identical canonical bytes (RFC 8785).
var writerA = new JsonObject
{
    ["seq"]         = 1,
    ["op"]          = "wallet.mint_identity",
    ["identity_id"] = "id_42",
    ["params_hash"] = "bafyparams",
    ["prev_cid"]    = "genesis",
};

var writerB = new JsonObject
{
    ["prev_cid"]    = "genesis",
    ["params_hash"] = "bafyparams",
    ["identity_id"] = "id_42",
    ["op"]          = "wallet.mint_identity",
    ["seq"]         = 1,
};

var canonicalA = JcsCanonicalizer.Canonicalize(writerA);
var canonicalB = JcsCanonicalizer.Canonicalize(writerB);

Console.WriteLine("== Stable serialization ==");
Console.WriteLine($"writer A canonical: {Encoding.UTF8.GetString(canonicalA)}");
Console.WriteLine($"writer B canonical: {Encoding.UTF8.GetString(canonicalB)}");
Console.WriteLine($"bytes equal:        {canonicalA.AsSpan().SequenceEqual(canonicalB)}");
Console.WriteLine();

// 2. JCS → CID in one call. Use Cid.FromCanonicalJson for the common case.
var entryCid = Cid.FromCanonicalJson(writerA);
Console.WriteLine("== JCS -> CID (one-shot) ==");
Console.WriteLine($"Cid.FromCanonicalJson: {entryCid}");
Console.WriteLine();

// 3. Two-step path for pipelines that need the canonical bytes for signing.
//    Hand the bytes to your signing key or hash primitive of choice.
var bytesForSigning = JcsCanonicalizer.Canonicalize(writerA);
Console.WriteLine("== Two-step (canonicalize -> sign) ==");
Console.WriteLine($"bytes ready for external signer: {bytesForSigning.Length} bytes");
Console.WriteLine();

// 4. Zero-allocation hot path for audit-chain verify, where the same canonical
//    bytes are hashed and compared per entry. Write directly into an
//    IBufferWriter<byte> and reuse the buffer.
Console.WriteLine("== Zero-allocation IBufferWriter overload ==");
var pooled = new ArrayBufferWriter<byte>(initialCapacity: 256);
JcsCanonicalizer.Canonicalize(writerA, pooled);
var pooledCid = Cid.FromContent(pooled.WrittenSpan);
Console.WriteLine($"written: {pooled.WrittenCount} bytes  cid: {pooledCid}");
Console.WriteLine();

// 5. Negative cases — JCS cannot represent NaN or fractional numbers in v1.
//    JcsFormatException carries an actionable message.
Console.WriteLine("== Negative cases ==");
try
{
    JcsCanonicalizer.Canonicalize(JsonValue.Create(double.NaN));
}
catch (JcsFormatException ex)
{
    Console.WriteLine($"NaN rejected:        {ex.Message}");
}

try
{
    JcsCanonicalizer.Canonicalize(JsonNode.Parse("1.5"));
}
catch (JcsFormatException ex)
{
    Console.WriteLine($"fractional rejected: {ex.Message}");
}
