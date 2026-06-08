## Implemented Specifications

`NetCid` implements the multiformats encoding stack plus RFC 8785 JSON canonicalization. The table below records, for each specification, what this library implements, the version/reference it targets, the body that governs it, and that specification's standardization status. Statuses are summarized as of the "last reviewed" date and may change — the multiformats specifications in particular are living documents.

| Specification                          | Implemented in `NetCid`                                                                                                                                         | Version / reference                                                                     | Governing body                                                                  | Status                                                                                                                 |
| -------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------- |
| **CID** (Content IDentifier)           | `Cid` — CIDv0 and CIDv1 create, parse, decode, encode, version conversion                                                                                       | Living spec — `multiformats/cid`                                                        | Multiformats project (community-maintained; originated at Protocol Labs / IPFS) | (https://github.com/multiformats/cid. Widely deployed (IPFS / IPLD).                                                   |
| **Multibase**                          | `Multibase` — base32 (lower/upper), base36 (lower/upper), base58btc, base64url                                                                                  | Living spec — `multiformats/multibase`; last IETF I-D `draft-multiformats-multibase-08` | Multiformats project, co-developed with the W3C Credentials Community Group     | Living specification. Last IETF Internet-Draft (Informational) expired Feb 2024; no longer active in the IETF process. |
| **Multicodec**                         | `Multicodec` — common content-type codecs and the key-type codecs (ed25519, x25519, secp256k1, BLS12-381 G1/G2, P-256/384/521), plus a varint prefix/decode API | Living registry — `multiformats/multicodec` (`table.csv`)                               | Multiformats project                                                            | Living specification / registry, unversioned; not on any formal standards track.                                       |
| **Multihash**                          | `Multihash` / `MultihashDigest` — multihash wire format (`varint(code) ‖ varint(len) ‖ digest`); SHA-256 and SHA-512 hashing helpers                            | Living spec — `multiformats/multihash`; last IETF I-D `draft-multiformats-multihash-07` | Multiformats project, co-developed with the W3C Credentials Community Group     | Living specification. Last IETF Internet-Draft (Informational) expired Feb 2024; no longer active in the IETF process. |
| **Unsigned Varint**                    | `Varint` — encode/decode, max 9-byte (63-bit) encoding, minimal-form validation                                                                                 | Living spec — `multiformats/unsigned-varint`                                            | Multiformats project                                                            | Living specification, unversioned; not on any formal standards track.                                                  |
| **JCS** (JSON Canonicalization Scheme) | `JcsCanonicalizer` — RFC 8785 canonical UTF-8 serialization; `Cid.FromCanonicalJson`                                                                            | **RFC 8785** (June 2020)                                                                | IETF / RFC Editor — Independent Submission stream                               | Published, stable. Informational; not Internet Standards Track and not endorsed by the IETF standards process.         |

### Conformance notes

- **Multibase, Multicodec, and Multihash are implemented as CID-focused subsets** of their full registries, by design. `NetCid` covers the encodings, codecs, and hash functions needed for content addressing and decentralized-identity key encoding, not every entry in the upstream tables.
- **Multihash hashing helpers** are provided for SHA-256 and SHA-512. The multihash wire format itself round-trips any function code; only these two have built-in digest computation.
- **JCS number support is RFC 8785-complete** as of 1.6.0: integers, fractional values, scientific notation, and integers beyond ±2<sup>53</sup> all canonicalize via the ECMA-262 §6.1.6.1.20 (`Number.prototype.toString`) algorithm required by RFC 8785 §3.2.2.3. The only numeric values that still throw `JcsFormatException` are `NaN` and `±∞`, which the spec forbids.
- **CID versions 2 and 3 are rejected as reserved**, per the CID specification.
- The **key-type multicodecs** identify public keys whose formats are defined elsewhere (Ed25519/X25519: RFC 8032 / RFC 7748; NIST P-curves: FIPS 186; secp256k1: SEC 2; BLS12-381: IRTF CFRG work). `NetCid` encodes their multicodec tags; it does not implement those cryptographic specifications.

### References

- CID — https://github.com/multiformats/cid
- Multibase — https://github.com/multiformats/multibase · https://datatracker.ietf.org/doc/draft-multiformats-multibase/
- Multicodec — https://github.com/multiformats/multicodec
- Multihash — https://github.com/multiformats/multihash · https://datatracker.ietf.org/doc/draft-multiformats-multihash/
- Unsigned Varint — https://github.com/multiformats/unsigned-varint
- JCS — https://www.rfc-editor.org/rfc/rfc8785

_Last reviewed: 2026-06-07._
