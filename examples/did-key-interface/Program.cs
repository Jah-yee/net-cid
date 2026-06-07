using NetCid;

// --- Simulate an Ed25519 public key (32 bytes) ---

var rawPublicKey = new byte[32];
Random.Shared.NextBytes(rawPublicKey);

Console.WriteLine("=== did:key construction ===\n");
Console.WriteLine($"  Raw Ed25519 public key: {Convert.ToHexString(rawPublicKey).ToLowerInvariant()}");

// --- Step 1: Encode as Multikey publicKeyMultibase in one call ---

var multibaseEncoded = Multikey.Encode(Multicodec.Ed25519Pub, rawPublicKey);
var didKey = $"did:key:{multibaseEncoded}";
Console.WriteLine($"  publicKeyMultibase:     {multibaseEncoded}");
Console.WriteLine($"  did:key identifier:     {didKey}");

// --- Step 2: Decode it back ---

Console.WriteLine("\n=== Decoding did:key ===\n");

var multibasePart = didKey["did:key:".Length..];
var (codec, recoveredKey) = Multikey.Decode(multibasePart);
Multicodec.TryGetName(codec, out var codecName);
Console.WriteLine($"  Key type codec:         0x{codec:X} ({codecName})");
Console.WriteLine($"  Recovered public key:   {Convert.ToHexString(recoveredKey).ToLowerInvariant()}");
Console.WriteLine($"  Keys match:             {rawPublicKey.AsSpan().SequenceEqual(recoveredKey)}");

// --- Step 3: base64url encoding for Data Integrity proofs ---
// (Multikey itself is base58btc only; this shows the underlying primitives for non-Multikey use.)

Console.WriteLine("\n=== Alternative encoding (base64url for Data Integrity) ===\n");

var prefixed = Multicodec.Prefix(Multicodec.Ed25519Pub, rawPublicKey);
var base64UrlEncoded = Multibase.Encode(prefixed, MultibaseEncoding.Base64Url, includePrefix: true);
Console.WriteLine($"  base64url (multibase):  {base64UrlEncoded}");

// Round-trip verification (cannot use Multikey.Decode — it rejects non-base58btc)
var decodedFromBase64Url = Multibase.Decode(base64UrlEncoded, out var encoding2);
var (codec2, recoveredKey2) = Multicodec.Decode(decodedFromBase64Url);
Console.WriteLine($"  Detected encoding:      {encoding2}");
Console.WriteLine($"  Round-trip matches:     {rawPublicKey.AsSpan().SequenceEqual(recoveredKey2)}");
Console.WriteLine($"  Multikey rejects it:    {!Multikey.TryDecode(base64UrlEncoded, out _, out _)}");
