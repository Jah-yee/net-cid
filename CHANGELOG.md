# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Released]

## [1.6.0] - 2026-06-07

### Added

- Full RFC 8785 §3.2.2.3 / ECMA-262 §6.1.6.1.20 (`Number.prototype.toString`) support in `JcsCanonicalizer` — JSON values containing fractional, exponential, or out-of-`ulong` numbers (monetary amounts, geo coordinates, scores in Verifiable Credentials) now canonicalize, resolving the v1 follow-up tracked from 1.4.0 ([#13](https://github.com/moisesja/net-cid/issues/13))
- Internal `EcmaScriptNumber.ToCanonicalString(double)` helper implementing the ECMA-262 §6.1.6.1.20 algorithm against .NET's shortest-round-trip digit string
- `jcs-number-conformance` workflow that downloads and SHA-256-pins cyberphone's `es6testfile100m.txt.gz` (100M-vector RFC 8785 conformance set) and runs it on PRs that touch the number formatter, plus `workflow_dispatch` and a weekly backstop
- Deterministic-seed 100k bit-pattern fuzz in `EcmaScriptNumberTests` that runs in every CI build

### Changed

- JSON integer literals with magnitude greater than 2<sup>53</sup> (the largest integer exactly representable as a double) now round to the nearest IEEE-754 double before serialization, as RFC 8785 §3.2.2.3 / ECMA-262 §6.1.6.1.20 require. Previously such literals were either emitted verbatim (when they fit in `long`/`ulong`) or threw `JcsFormatException` "outside the supported range". Concretely:
  - `"9007199254740993"` (= 2<sup>53</sup>+1) now canonicalizes as `"9007199254740992"` (was `"9007199254740993"`).
  - `"18446744073709551615"` (`ulong.MaxValue`) now canonicalizes as `"18446744073709552000"` (was `"18446744073709551615"`).
  - `"1000000000000000000000"` (> `ulong.MaxValue`) now canonicalizes as `"1e+21"` (was a `JcsFormatException`).
  - The same value written as `9007199254740993` or `9007199254740993.0` now yields identical bytes — the determinism guarantee the v1 fast path silently broke.
  - Literals so large they parse to `±∞` (e.g. a 400-digit integer) still throw the existing infinity error.

## [1.5.0] - 2026-05-22

### Added

- `Multicodec.P521Pub` (`0x1202`) and the `"p521-pub"` name mapping, completing the NIST P-curve public-key set alongside the existing `P256Pub` / `P384Pub` ([#11](https://github.com/moisesja/net-cid/issues/11))

## [1.4.0] - 2026-05-21

### Added

- `JcsCanonicalizer` — RFC 8785 JSON Canonicalization Scheme (JCS) producing a deterministic UTF-8 serialization of any supported JSON value, with overloads for `JsonNode?`, `JsonElement`, and direct `IBufferWriter<byte>` writes ([#9](https://github.com/moisesja/net-cid/issues/9))
- `Cid.FromCanonicalJson(JsonNode?, codec, hashCode)` convenience overload that canonicalizes JSON and computes the resulting CID in one call
- `JcsFormatException` for values JCS cannot represent deterministically (`NaN`, `±infinity`, fractional/exponential numbers in v1, out-of-range integers)

### Notes

- v1 scope covers objects, arrays, strings, integer-valued numbers within `[long.MinValue, ulong.MaxValue]`, booleans, and null. Fractional/IEEE 754 numbers throw `JcsFormatException` — full support landed in [1.6.0](#160---2026-06-06) ([#13](https://github.com/moisesja/net-cid/issues/13)).

## [1.3.0] - 2026-03-15

### Added

- `Multihash.Encode(ulong hashFunctionCode, ReadOnlySpan<byte> digest)` for constructing spec-compliant multihash bytes: `varint(code) || varint(digestLength) || digest` ([#7](https://github.com/moisesja/net-cid/issues/7))
- `Multihash.Decode` and `Multihash.TryDecode` for parsing multihash byte sequences back into code and digest

## [1.2.1] - 2026-03-08

### Fixed

- Base36 decoding is now case-insensitive per the multibase spec, allowing mixed-case payloads (e.g., from DNS systems) to decode correctly ([#3](https://github.com/moisesja/net-cid/issues/3))
- BLS public-key multicodec names corrected from `bls12-381-g1-pub` / `bls12-381-g2-pub` to `bls12_381-g1-pub` / `bls12_381-g2-pub` to match the official multicodec registry ([#5](https://github.com/moisesja/net-cid/issues/5))

## [1.2.0] - 2026-03-08

### Added

- Base64url multibase encoding and decoding (prefix `u`)
- Key-type multicodec constants and name lookups (secp256k1, BLS12-381, x25519, ed25519, P-256, P-384)
- `Multicodec.Prefix` and `Multicodec.Decode` for multicodec-prefixed byte buffers

## [1.1.0] - 2025-11-01

### Added

- Base36 multibase encoding and decoding (prefixes `k` and `K`)

## [1.0.0] - 2025-10-01

### Added

- Initial release with CIDv0 and CIDv1 support
- Base32 and Base58btc multibase encoding/decoding
- SHA-256 and SHA-512 multihash support
- Core multicodec constants (raw, dag-pb, dag-cbor, etc.)

[Unreleased]: https://github.com/moisesja/net-cid/compare/v1.6.0...HEAD
[1.6.0]: https://github.com/moisesja/net-cid/compare/v1.5.0...v1.6.0
[1.5.0]: https://github.com/moisesja/net-cid/compare/v1.4.0...v1.5.0
[1.4.0]: https://github.com/moisesja/net-cid/compare/v1.3.0...v1.4.0
[1.3.0]: https://github.com/moisesja/net-cid/compare/v1.2.1...v1.3.0
[1.2.1]: https://github.com/moisesja/net-cid/compare/v1.2.0...v1.2.1
[1.2.0]: https://github.com/moisesja/net-cid/compare/v1.1.0...v1.2.0
[1.1.0]: https://github.com/moisesja/net-cid/compare/v1.0.0...v1.1.0
[1.0.0]: https://github.com/moisesja/net-cid/releases/tag/v1.0.0
