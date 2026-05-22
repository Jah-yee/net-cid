# CID C# Library Delivery Plan

## Scope

- Build a production-ready C# library implementing the [multiformats CID specification](https://github.com/multiformats/cid)
- Provide full automated test coverage (unit + integration)
- Add documentation and release automation to NuGet
- Produce a security audit artifact and security-focused CI checks

## Plan

- [x] Verify spec requirements and lock compatibility targets (CIDv0/CIDv1, multibase forms, varint, multihash constraints)
- [x] Scaffold solution/projects for library, unit tests, and integration tests
- [x] Implement core primitives (varint, multibase codecs, multicodec registry, multihash model)
- [x] Implement CID model/parsing/encoding/conversion APIs with validation and error handling
- [x] Implement unit tests for each primitive and CID edge cases
- [x] Implement integration tests using known CID vectors and end-to-end round trips
- [x] Write developer and consumer documentation (README, API usage, release and support docs)
- [x] Add CI/CD pipelines for build/test/security scans and NuGet publishing
- [x] Run full verification locally (restore, build, unit tests, integration tests, pack)
- [x] Write review notes and security audit summary

## Verification Checklist

- [x] `dotnet restore` (completed via package listing/vulnerability checks)
- [x] `dotnet build -c Release`
- [x] `dotnet test -c Release --no-build`
- [x] `dotnet pack -c Release --no-build`
- [x] Security checks executed and documented

## Review

- Implemented a full `net10.0` CID library with CIDv0/CIDv1 parsing, encoding, conversion, varint, multibase, multihash helpers, and codec registries.
- Added comprehensive test coverage: 25 unit tests + 3 integration tests, all passing.
- Added release-ready packaging metadata and generated `.nupkg` + `.snupkg`.
- Added CI, security, CodeQL, and NuGet publish workflows under `.github/workflows/`.
- Completed security audit report in `SECURITY_AUDIT.md` with findings and remediation guidance.

---

# Follow-up: Security Findings + Examples

## Scope

- Resolve all findings in `SECURITY_AUDIT.md`
- Add `examples/` implementations guided by `js-multiformats/examples`

## Plan

- [x] Replace deprecated xUnit v2 packages with xUnit v3 in all test projects
- [x] Add explicit CID/multibase input size limits in public parsing APIs
- [x] Add tests that validate oversized input rejection behavior
- [x] Create `examples/` directory with C# examples mirroring:
  - cid-interface
  - multicodec-interface
  - multihash-interface
  - block-interface
- [x] Document how to run the examples
- [x] Re-run build/tests/package/dependency checks
- [x] Update `SECURITY_AUDIT.md` to show findings resolved

## Verification Checklist

- [x] `dotnet build NetCid/NetCid.csproj -c Release --no-restore --tl:off`
- [x] `dotnet build NetCid.Tests/NetCid.Tests.csproj -c Release --no-restore --tl:off`
- [x] `dotnet build NetCid.IntegrationTests/NetCid.IntegrationTests.csproj -c Release --no-restore --tl:off`
- [x] `dotnet test NetCid.Tests/NetCid.Tests.csproj -c Release --no-build --tl:off`
- [x] `dotnet test NetCid.IntegrationTests/NetCid.IntegrationTests.csproj -c Release --no-build --tl:off`
- [x] `dotnet list NetCid.sln package --deprecated`
- [x] `dotnet list NetCid.sln package --vulnerable --include-transitive`
- [x] `dotnet run --project examples/cid-interface/CidInterfaceExample.csproj -c Release --no-build`
- [x] `dotnet run --project examples/multicodec-interface/MulticodecInterfaceExample.csproj -c Release --no-build`
- [x] `dotnet run --project examples/multihash-interface/MultihashInterfaceExample.csproj -c Release --no-build`
- [x] `dotnet run --project examples/block-interface/BlockInterfaceExample.csproj -c Release --no-build`

## Follow-up Review

- Migrated tests from `xunit` v2 to `xunit.v3` and updated `xunit.runner.visualstudio`.
- Added enforced input-size limits to CID and multibase parsing APIs with customizable overloads.
- Added oversized-input tests (unit coverage now 30 tests passing).
- Added four runnable C# examples under `examples/` mirroring js-multiformats example topics.
- Updated `SECURITY_AUDIT.md` to reflect resolved findings and clean dependency scan results.

---

# Task: Contributor Guide

## Scope

- Create `contributors.md` with practical onboarding and contribution workflow guidance.
- Align recommendations with established patterns from successful open source projects.

## Plan

- [x] Draft contributor guide sections tailored to this repository.
- [x] Add local setup, testing, and PR quality expectations.
- [x] Add contributor checklists and security/reporting paths.
- [x] Review for clarity and repo-command accuracy.

## Verification Checklist

- [x] `contributors.md` exists at repository root.
- [x] All command examples run against existing project paths.
- [x] Content references existing repository files only.

## Review

- Added `contributors.md` with contributor guidance modeled on successful OSS practices (small focused PRs, verification discipline, and clear review context).
- Included repo-specific setup and verification commands aligned with existing solution and example projects.
- Documented PR quality expectations, contribution checklist, commit guidance, security reporting path, and licensing note.

---

# Task: Adopt SimpleBase with NetCid Validation Wrapper

## Scope

- Replace internal base32/base36/base58 encode/decode algorithms with `SimpleBase`.
- Preserve NetCid strict decoding and validation semantics for CID-facing parsing.
- Keep public API and error behavior stable for existing callers.

## Plan

- [x] Add `SimpleBase` package reference to `NetCid/NetCid.csproj`.
- [x] Refactor `NetCid/Multibase.cs` to delegate encoding/decoding primitives to `SimpleBase`.
- [x] Keep wrapper validation for:
  - supported prefixes only (`b`, `B`, `k`, `K`, `z`)
  - base32 padding rejection and trailing-bit validation
  - case-specific base36 validation
  - max input length checks and CID-specific exception mapping
- [x] Add/adjust tests for strict validation behavior when using `SimpleBase`.
- [x] Run unit and integration tests and capture results.

## Verification Checklist

- [x] `dotnet test NetCid.Tests/NetCid.Tests.csproj -c Release`
- [x] `dotnet test NetCid.IntegrationTests/NetCid.IntegrationTests.csproj -c Release`

## Review

- Added `SimpleBase` `5.6.0` as the only runtime package dependency in `NetCid/NetCid.csproj`.
- Replaced custom base32/base36/base58 encode/decode primitives in `NetCid/Multibase.cs` with `SimpleBase` coders.
- Preserved strict wrapper validation behavior for CID parsing:
  - rejects base32 padding and invalid non-zero trailing bits
  - enforces case-specific base36 alphabets based on multibase prefix
  - enforces prefix and input-length restrictions unchanged
- Added regression tests in `NetCid.Tests/MultibaseTests.cs` for trailing-bit rejection and base36 case-strict decoding.
- Verified with tests:
  - `dotnet test NetCid.Tests/NetCid.Tests.csproj -c Release` (33 passed)
  - `dotnet test NetCid.IntegrationTests/NetCid.IntegrationTests.csproj -c Release` (3 passed)

---

# Task: Security Audit Round 2 (Post-SimpleBase Integration)

## Scope

- Perform a fresh end-to-end security audit after introducing `SimpleBase`.
- Re-validate parser hardening, dependency posture, and runtime verification.
- Produce an updated detailed audit report.

## Plan

- [x] Re-scan source for security-sensitive paths (`Cid`, `Multibase`, `Varint`, `MultihashDigest`).
- [x] Run dependency checks (vulnerable + deprecated package scans).
- [x] Run clean release build and full test suite.
- [x] Execute targeted negative/fuzz-style validation checks for parser exception safety.
- [x] Update `SECURITY_AUDIT.md` with dated findings and conclusions.

## Verification Checklist

- [x] `dotnet list NetCid.sln package --vulnerable --include-transitive`
- [x] `dotnet list NetCid.sln package --deprecated`
- [x] `dotnet build NetCid/NetCid.csproj -c Release --no-restore --tl:off -warnaserror`
- [x] `dotnet build NetCid.Tests/NetCid.Tests.csproj -c Release --no-restore --tl:off`
- [x] `dotnet build NetCid.IntegrationTests/NetCid.IntegrationTests.csproj -c Release --no-restore --tl:off`
- [x] `dotnet build examples/cid-interface/CidInterfaceExample.csproj -c Release --no-restore --tl:off`
- [x] `dotnet build examples/multicodec-interface/MulticodecInterfaceExample.csproj -c Release --no-restore --tl:off`
- [x] `dotnet build examples/multihash-interface/MultihashInterfaceExample.csproj -c Release --no-restore --tl:off`
- [x] `dotnet build examples/block-interface/BlockInterfaceExample.csproj -c Release --no-restore --tl:off`
- [x] `dotnet test NetCid.Tests/NetCid.Tests.csproj -c Release`
- [x] `dotnet test NetCid.IntegrationTests/NetCid.IntegrationTests.csproj -c Release`

## Review

- Manually reviewed security-sensitive parsers and validation paths across `Cid`, `Multibase`, `Varint`, and `MultihashDigest`.
- Confirmed vulnerability and deprecation scans are clean after introducing `SimpleBase`.
- Verified release builds for library, tests, and examples succeeded with zero warnings/errors in audited commands.
- Executed fuzz-style malformed-input probes (ASCII `100k` plus Unicode `20k`) against multibase and CID APIs with no unexpected exception behavior.
- Updated `SECURITY_AUDIT.md` with a new detailed Round 2 report and final release-readiness conclusion.

---

# Task: Add base64url multibase encoding and key-type multicodec support

## Scope

- Add base64url (`u`) multibase encoding to eliminate NetDid's duplicated encoding logic.
- Add 7 key-type multicodec constants used by DID methods and Verifiable Credentials.
- Add `Multicodec.Prefix()` / `Decode()` / `TryDecode()` convenience API for varint-tagged byte buffers.
- Add examples and architecture documentation.
- Bump version to 1.2.0.

## Plan

- [x] Add `Base64Url` to `MultibaseEncoding` enum
- [x] Wire base64url encode/decode into `Multibase` class (using `System.Buffers.Text.Base64Url`)
- [x] Add 7 key-type constants to `Multicodec` (ed25519-pub, p256-pub, secp256k1-pub, etc.)
- [x] Add `Multicodec.Prefix()` / `Decode()` / `TryDecode()` methods
- [x] Add base64url tests to `MultibaseTests.cs`
- [x] Create `MulticodecTests.cs` with key-type lookup and Prefix/Decode tests
- [x] Create `examples/multibase-interface` example
- [x] Create `examples/did-key-interface` example
- [x] Create `ARCHITECTURE.md` with objectives, requirements, and design
- [x] Bump version to 1.2.0 and update README.md

## Verification Checklist

- [x] `dotnet build NetCid.sln -c Release` — 0 warnings, 0 errors
- [x] `dotnet test NetCid.Tests/NetCid.Tests.csproj -c Release` — 67 passed
- [x] `dotnet test NetCid.IntegrationTests/NetCid.IntegrationTests.csproj -c Release` — 3 passed
- [x] `dotnet run --project examples/multibase-interface/MultibaseInterfaceExample.csproj` — runs successfully
- [x] `dotnet run --project examples/did-key-interface/DidKeyInterfaceExample.csproj` — runs successfully

## Review

- Added base64url multibase encoding using .NET's built-in `System.Buffers.Text.Base64Url` (RFC 4648 §5, no padding).
- Added 7 key-type multicodec constants: `secp256k1-pub`, `bls12-381-g1-pub`, `bls12-381-g2-pub`, `x25519-pub`, `ed25519-pub`, `p256-pub`, `p384-pub`.
- Added `Multicodec.Prefix()` / `Decode()` / `TryDecode()` convenience API using existing `Varint` primitives.
- Added 7 new multibase tests and 12 new multicodec tests (total: 67 unit + 3 integration, all passing).
- Created two new examples: `multibase-interface` (encoding comparison) and `did-key-interface` (did:key construction workflow).
- Created `ARCHITECTURE.md` documenting objectives, spec conformance, module design, encoding flows, and design decisions.
- Bumped version from 1.1.0 to 1.2.0.

---

# Task: Audit compliance with multiformats specifications

## Scope

- Review the library implementation and test coverage against the upstream multiformats specifications used by this repo:
  - CID
  - multibase
  - multicodec
  - multihash
  - unsigned-varint
- Identify concrete compliance gaps, ambiguous behaviors, and missing verification coverage.
- Document audit findings with severity, evidence, and follow-up recommendations.

## Plan

- [x] Read the core library files and tests to inventory implemented multiformats behavior.
- [x] Cross-check each implemented behavior against the upstream multiformats specifications and reference expectations.
- [x] Verify compliance claims with local tests and targeted build/test execution where needed.
- [x] Write the audit review section with findings, supporting evidence, and residual risks.

## Verification Checklist

- [x] Review implementation in `NetCid/`
- [x] Review tests in `NetCid.Tests/` and `NetCid.IntegrationTests/`
- [x] Compare observed behavior with multiformats spec requirements
- [x] Run targeted verification commands for any behavior that needs execution proof

## Review

- Verified `dotnet test NetCid.sln -c Release --tl:off` passes (`67` unit tests, `3` integration tests).
- Confirmed CID parsing/encoding logic aligns with the upstream CID decoding algorithm: CIDv0 string heuristic, CIDv0 byte detection, CIDv1 varint layout, reserved version rejection, and default string encodings.
- Confirmed unsigned-varint implementation enforces the multiformats practical maximum (`9` bytes / `63` bits) and rejects non-minimal encodings.
- Found 2 compliance issues:
  - `Multibase` decodes `base36`/`base36upper` as case-sensitive, but the multibase registry defines both as case-insensitive. The non-compliant behavior is implemented in `NetCid/Multibase.cs` and reinforced by tests in `NetCid.Tests/MultibaseTests.cs`.
  - `Multicodec` uses `bls12-381-g1-pub` / `bls12-381-g2-pub`, but the official multicodec registry names are `bls12_381-g1-pub` / `bls12_381-g2-pub`. The incorrect names appear in `NetCid/Multicodec.cs` and are reinforced by tests in `NetCid.Tests/MulticodecTests.cs`.
- Residual scope note: the repo implements a documented CID-focused subset of multibase, multicodec, and multihash rather than the full registries. I treated that as intentional scope, not a compliance defect, except where implemented behavior diverges from the upstream registry/spec.

---

# Task: Turn audit findings into GitHub issues

## Scope

- Convert the two multiformats compliance findings into actionable GitHub issues.
- Include direct file references, spec references, concrete repro steps, expected vs actual behavior, and interoperability impact.
- Create the issues in GitHub if local auth/tooling allows it; otherwise prepare publication-ready issue bodies.

## Plan

- [ ] Check repo remote and GitHub CLI/auth availability.
- [ ] Draft issue 1 for non-compliant `base36` case-sensitivity with a concrete break example.
- [ ] Draft issue 2 for incorrect BLS multicodec registry names with a concrete break example.
- [ ] Create the issues in GitHub or capture the blocker and save ready-to-post text.
- [ ] Record the created issue links or fallback artifacts in this review section.

## Verification Checklist

- [ ] Confirm target GitHub repository
- [ ] Confirm `gh` availability/auth status
- [ ] Validate each issue body includes reproduction steps and impact
- [ ] Verify created issue URLs if publication succeeds

## Review

- Pending

---

# Task: Add JCS (RFC 8785) JSON canonicalization (Issue #9) — COMPLETED

## Scope

- Implement JSON Canonicalization Scheme (RFC 8785) so consumers can content-address JSON values with stable byte output and pair naturally with `Cid.FromContent`.
- Add a `JcsCanonicalizer` static class with three overloads (`JsonNode?`, `JsonElement`, and `JsonNode? + IBufferWriter<byte>`).
- Add `JcsFormatException : FormatException` for JCS-specific errors.
- Add `Cid.FromCanonicalJson(JsonNode?, codec, hashCode)` convenience overload that canonicalizes + builds a CID in one call.
- Bump package version to `1.4.0` and document the addition.

## v1 type-coverage decision (matches the issue's recommendation **b**)

- Supported: objects (sorted-key), arrays (order-preserved), strings (RFC 8785 §3.2.2.2 escapes, raw UTF-8 above U+007F), integer numbers within `[long.MinValue, ulong.MaxValue]`, `true`, `false`, `null`.
- Throws `JcsFormatException`: fractional / exponential numbers, `NaN`, `±∞`, integers outside `[long.MinValue, ulong.MaxValue]`, `JsonValueKind.Undefined`.
- Rationale: every consumer named in the issue (wallet audit log, ZCAP-LD `zcap_cid`, VC DataIntegrity proofs over JCS) only needs integers + strings + bools + objects + arrays. ECMAScript-style fractional output is deferred to a follow-up issue.

## Implementation strategy

- `JsonElement` walker is the single source of truth for writing canonical bytes — sorts object keys by `string.CompareOrdinal` (UTF-16 code unit order = JCS §3.2.3), preserves array order, writes numbers from `TryGetInt64` / `TryGetUInt64`, and emits strings via UTF-8 with byte-level escape pass (`\"`, `\\`, `\b`, `\t`, `\n`, `\f`, `\r`, `\u00XX` for U+0000..U+001F). All bytes ≥ 0x80 pass through as raw UTF-8.
- `JsonNode?` overloads pre-walk to catch NaN/±∞ stored as raw .NET doubles (the System.Text.Json serializer would otherwise throw a generic `JsonException`), then `JsonSerializer.SerializeToDocument` into a `JsonDocument` and delegate to the `JsonElement` walker.
- `IBufferWriter<byte>` overload writes directly to the writer with `stackalloc`-backed UTF-8 staging for small strings and `ArrayPool<byte>` fallback for large ones, avoiding per-call `byte[]` allocation in hot paths (audit chain verification).
- `-0` normalises to `0` (parsed as integer by `TryGetInt64`, formatted back via `ToString(CultureInfo.InvariantCulture)`).

## Plan

- [x] Add `NetCid/JcsFormatException.cs`.
- [x] Add `NetCid/JcsCanonicalizer.cs` with the three public overloads + private walker helpers.
- [x] Add `Cid.FromCanonicalJson` overload in `NetCid/Cid.cs`.
- [x] Add `NetCid.Tests/JcsCanonicalizerTests.cs` (30 tests).
- [x] Add `NetCid.IntegrationTests/JcsCidRoundTripTests.cs` (3 tests).
- [x] Bump `Version` to `1.4.0` in `NetCid/NetCid.csproj`.
- [x] Add a `1.4.0` entry in `CHANGELOG.md` referencing issue #9.
- [x] Add a JCS bullet + usage snippet to README.

## Verification Checklist

- [x] `dotnet build NetCid.sln -c Release --tl:off` — 0 warnings, 0 errors
- [x] `dotnet test NetCid.Tests/NetCid.Tests.csproj -c Release --no-build` — 105/105 pass (75 prior + 30 new)
- [x] `dotnet test NetCid.IntegrationTests/NetCid.IntegrationTests.csproj -c Release --no-build` — 6/6 pass (3 prior + 3 new)

## Review

- Implemented `JcsCanonicalizer` with a `JsonElement` walker as the canonical core. `JsonNode` overloads pre-validate raw .NET `NaN`/`±∞` doubles (which `JsonSerializer.SerializeToDocument` otherwise rejects with a generic `JsonException`), then serialize to a `JsonDocument` and delegate to the same walker — keeping one well-tested code path for canonical bytes.
- Object key sort uses `string.CompareOrdinal` (UTF-16 code unit ordering, identical to RFC 8785 §3.2.3). Integers go through `TryGetInt64`/`TryGetUInt64`, formatted with `InvariantCulture`; this auto-normalises `-0` to `0` and covers the full `[long.MinValue, ulong.MaxValue]` range used by audit chains.
- String escaper UTF-8-encodes the whole string into a stackalloc / `ArrayPool<byte>` buffer (256-byte cutoff), then byte-walks: only the seven short escapes plus `\u00XX` for bytes `< 0x20` need replacement. Bytes `>= 0x80` (all UTF-8 multi-byte continuations and lead bytes) pass through as raw bytes, which matches JCS §3.2.2.2's requirement that characters above U+007F stay as raw UTF-8 — verified with the `é` and emoji surrogate-pair tests.
- Added `Cid.FromCanonicalJson(JsonNode?, codec = Raw, hashCode = Sha2_256)` that writes canonical bytes straight into an `ArrayBufferWriter<byte>` and feeds the resulting span to `FromContent`, avoiding an intermediate `byte[]`. The round-trip integration test proves it produces the same `Cid` as the two-step `FromContent(JcsCanonicalizer.Canonicalize(...))` form.
- v1 type scope deliberately throws `JcsFormatException` for fractional/exponential numbers, `NaN`, `±∞`, oversized integers, and `JsonValueKind.Undefined`. Each exception message is actionable (range hint or follow-up pointer) so a downstream consumer hitting it knows exactly why.
- Verified locally: full release build clean (0 warnings, 0 errors); 105 unit + 6 integration tests green.
