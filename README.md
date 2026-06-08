# NetCid

`NetCid` is a C# (`net10.0`) implementation of the [multiformats CID specification](https://github.com/multiformats/cid).

## Features

- CIDv0 and CIDv1 parsing, encoding, and round-tripping
- CID conversion (`ToV0`, `ToV1`)
- Binary CID decode/encode
- Unsigned varint codec (multiformats-compatible, max 9-byte encoding)
- Multihash model + SHA-256 / SHA-512 hash helpers
- `Multihash.Encode` / `Decode` for spec-compliant multihash wire format (`varint(code) || varint(digestLength) || digest`)
- Multibase support for:
  - `base58btc` (`z`)
  - `base32` lower/upper (`b` / `B`)
  - `base36` lower/upper (`k` / `K`)
  - `base64url` (`u`)
- Multicodec constants for common CID codecs (`raw`, `dag-pb`, `dag-cbor`, etc.)
- Multicodec key-type constants (`ed25519-pub`, `p256-pub`, `secp256k1-pub`, etc.)
- Multicodec prefix/decode API for varint-tagged byte buffers
- `Multikey` — encode/decode W3C Controlled Identifiers `publicKeyMultibase` (base58btc(varint(keyCodec) ‖ rawKey)) with per-codec key-length validation; one call replaces the manual `Multicodec.Prefix` + `Multibase.Encode` dance for `did:key` construction
- `JcsCanonicalizer` — RFC 8785 JSON Canonicalization Scheme for stable content-addressing of JSON values, and `Cid.FromCanonicalJson` convenience

For the full list of specifications this library implements, the version/reference each targets, the governing body, and that specification's standardization status, see [`net-cid-implemented-specs.md`](net-cid-implemented-specs.md).

## Install

```bash
dotnet add package NetCid
```

## Quick Start

```csharp
using NetCid;
using System.Text;

// Parse existing CIDs
var v0 = Cid.Parse("QmdfTbBqBPQ7VNxZEYEj14VmRuZBkqFbiwReogJgS1zR1n");
var v1 = Cid.Parse("bafkreidon73zkcrwdb5iafqtijxildoonbwnpv7dyd6ef3qdgads2jc4su");

// Convert versions
var v0AsV1 = v0.ToV1();
var v1AsV0 = v0AsV1.ToV0();

// Build from content bytes
var content = Encoding.UTF8.GetBytes("hello world");
var cid = Cid.FromContent(content, codec: Multicodec.Raw, hashCode: MultihashCode.Sha2_256);

// Serialize
string text = cid.ToString(); // CIDv1 defaults to base32 lower
byte[] bytes = cid.ToByteArray();

// Content-address a JSON value with stable bytes (JCS / RFC 8785)
var entry = new System.Text.Json.Nodes.JsonObject
{
    ["seq"] = 1,
    ["op"]  = "wallet.mint_identity",
};
var jsonCid = Cid.FromCanonicalJson(entry);
```

## Specification Notes

Implementation follows the CID spec behavior, including:

- CIDv0 is always `dag-pb` + `sha2-256(32)`
- CIDv1 binary layout: `<cidv1-varint><codec-varint><multihash>`
- CIDv0 string form has no multibase prefix
- CID versions `2` and `3` are treated as reserved/invalid

## Input Limits

Parsing APIs enforce default size limits to reduce memory-pressure risk from untrusted input:

- `Cid.DefaultMaxInputStringLength`
- `Cid.DefaultMaxInputByteLength`
- `Multibase.DefaultMaxInputLength`

Overloads on parse/decode methods let callers provide custom limits when needed.

References:

- https://github.com/multiformats/cid
- https://multiformats.readthedocs.io/en/latest/api/multiformats.cid.html

## Development

```bash
dotnet restore NetCid.sln
dotnet build NetCid.sln -c Release
dotnet test NetCid.Tests/NetCid.Tests.csproj -c Release
dotnet test NetCid.IntegrationTests/NetCid.IntegrationTests.csproj -c Release
```

## Examples

Reference examples are available under `examples/` and mirror the `js-multiformats` example set:

- `examples/cid-interface`
- `examples/multicodec-interface`
- `examples/multihash-interface`
- `examples/block-interface`
- `examples/multibase-interface`
- `examples/did-key-interface`
- `examples/jcs-interface`

See `examples/README.md` for run commands.

## Contributing

See `contributors.md` for contributor workflow, quality checklist, and PR expectations.

## CI / Release

- CI workflow: `.github/workflows/ci.yml`
- Security workflows: `.github/workflows/security.yml`, `.github/workflows/codeql.yml`
- NuGet publish workflow: `.github/workflows/release.yml`

`release.yml` pushes packages when a tag like `v1.2.3` is pushed (or manual dispatch) and requires `NUGET_API_KEY` repository secret.

## Security

- Responsible disclosure: see `SECURITY.md`
- Security review and findings: see `SECURITY_AUDIT.md`
# test
