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

// 5. IEEE-754 numbers — RFC 8785 §3.2.2.3 mandates the ECMA-262
//    Number::toString algorithm for fractional and exponential values.
//    Real Verifiable Credentials carry money amounts, geo coordinates,
//    and scores; all of them now canonicalize without losing precision.
Console.WriteLine("== IEEE-754 numbers (RFC 8785 / ECMA-262) ==");
foreach (var sample in new[] { "1.5", "0.1", "1e-7", "1e21", "5e-324", "333333333.3333332897" })
{
    var canonical = JcsCanonicalizer.Canonicalize(JsonNode.Parse(sample));
    Console.WriteLine($"{sample,-26} -> {Encoding.UTF8.GetString(canonical)}");
}
Console.WriteLine();

// 6. Negative cases — JCS cannot represent NaN or ±infinity (they have no
//    JSON syntax), it rejects duplicate object member names (RFC 8785 builds on
//    I-JSON / RFC 7493, which forbids them), and it rejects strings with invalid UTF-16
//    (unpaired surrogates) rather than silently substituting U+FFFD. JcsFormatException
//    carries an actionable message.
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
    JcsCanonicalizer.Canonicalize(JsonValue.Create(double.PositiveInfinity));
}
catch (JcsFormatException ex)
{
    Console.WriteLine($"+infinity rejected:  {ex.Message}");
}

try
{
    // RFC 8785 / I-JSON (RFC 7493) forbid duplicate member names; JsonDocument would
    // otherwise preserve both "a" members and produce ambiguous, non-canonical output.
    JcsCanonicalizer.Canonicalize(JsonNode.Parse("{\"a\":1,\"a\":2}"));
}
catch (JcsFormatException ex)
{
    Console.WriteLine($"duplicate key rejected: {ex.Message}");
}

try
{
    // An unpaired surrogate has no UTF-8 representation; rather than let System.Text.Json
    // silently rewrite it to U+FFFD (which would let two distinct malformed inputs collapse
    // to the same canonical bytes), JcsCanonicalizer rejects it.
    JcsCanonicalizer.Canonicalize(JsonValue.Create("\uD800"));
}
catch (JcsFormatException ex)
{
    Console.WriteLine($"unpaired surrogate rejected: {ex.Message}");
}
