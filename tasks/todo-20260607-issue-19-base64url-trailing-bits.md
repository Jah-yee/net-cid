# Issue #19 (S4) — base64url decode non-canonical trailing-bit validation

Branch: `feature/issue-19-base64url-trailing-bits` (off `origin/main` @ 1e9880a)

## Step 0 result (runtime probe, pre-plan)
`System.Buffers.Text.Base64Url.DecodeFromChars` on net10.0 **rejects** non-canonical trailing
bits (`"AB"`,`"AP"`,`"AAB"`,`"AAC"` → throw; `"AA"`→[00], `"AQ"`→[01], `"AAA"`→[0000] accept).
The issue's premise (decoder masks) conflates legacy `Convert.FromBase64String` (masks) with the
newer `Base64Url` (validates). `DecodeBase64Url` already rethrows `FormatException` as
`CidFormatException`, so `Multibase.Decode("uAB")` / `Cid.Parse("uAB")` already throw today.
→ Issue's **downgrade branch**: tests + docs, no decode-logic change.

## Tasks
- [x] 1. Sync onto latest `origin/main`; branch `feature/issue-19-...` before first edit
- [x] 2. Tests in `NetCid.Tests/MultibaseTests.cs`:
    - [x] `Decode_ThrowsOnInvalidBase64UrlTrailingBits` `[Theory]`: `uAB`,`uAP`,`uAAB`,`uAAC`
    - [x] `Decode_RejectsBase64UrlMalleabilityCollision`: `uAA`→[00] accepts, `uAB` throws
    - [x] `Decode_AcceptsCanonicalShortBase64UrlPayload` `[Theory]`: `uAA`→0x00, `uAQ`→0x01
- [x] 3. Explanatory comment in `NetCid/Multibase.cs` `DecodeBase64Url` (why no accumulator)
- [x] 4. `SECURITY_AUDIT.md`: add base64url bullet to "Multibase strictness retained" matrix only
       (Change-Context list is the SimpleBase-swap narrative — base64url is BCL, leave it). Coord D1/#20.
- [x] 5. `CHANGELOG.md`: Security bullet under 1.6.0 referencing #19 (no behavior change). Heading → D3/#22.
- [x] 6. `dotnet build` clean
- [x] 7. `dotnet test` full suite green
- [x] 8. Adversarial review (≥2 rounds — lesson from #18): try to find any non-canonical base64url
       string `Multibase.Decode`/`Cid.Parse` accepts (2/3-char groups, 1-char invalid remainder,
       multi-group, case/Unicode/whitespace). Flip to implementing the accumulator if a hole is found.

## Review

**Outcome: downgrade confirmed correct — tests + docs only, no decode-logic change.**

Changes (47 insertions vs `origin/main` @ 1e9880a, branch `feature/issue-19-base64url-trailing-bits`):
- `NetCid.Tests/MultibaseTests.cs` (+38): 3 new tests / 7 cases — `Decode_ThrowsOnInvalidBase64UrlTrailingBits`
  (`uAB`,`uAP`,`uAAB`,`uAAC`), `Decode_RejectsBase64UrlMalleabilityCollision` (`uAA`→[00] vs `uAB` throws),
  `Decode_AcceptsCanonicalShortBase64UrlPayload` (`uAA`→0x00, `uAQ`→0x01).
- `NetCid/Multibase.cs` (+7): comment in `DecodeBase64Url` explaining why there is no
  `ValidateBase64UrlTrailingBits` accumulator (framework decoder validates; contrast base32/SimpleBase).
- `SECURITY_AUDIT.md` (+1): base64url bullet in the "Multibase strictness retained" matrix (coord D1/#20).
- `CHANGELOG.md` (+1): Security bullet under 1.6.0 (heading left to D3/#22).

Verification:
- `dotnet build` clean (0 warnings/0 errors). `dotnet test` green: 230 passed / 1 skipped (env-gated
  conformance) in NetCid.Tests + 6 in IntegrationTests. New base64url cases: 7/7 pass.

Adversarial review (2 independent rounds, AGENTS.md §2; scratch under /tmp, no worktree changes):
- Round 1 (brute-force/collision): ~871k decode attempts incl. exhaustive 2/3-char space (266,240),
  collision sweep (466,240), `Cid.TryParse` over all 262,144 three-char strings. 0 non-canonical accepted,
  0 collisions, 0 value mismatches — NetCid accepts exactly the canonical set.
- Round 2 (edge/API surface): invalid-length, whitespace/control/Unicode-homoglyph injection (rejected by
  the alphabet allowlist), case-sensitivity (no silent normalization), end-to-end `Cid.Parse` with a real
  68-byte sha2-512 CID (16 accepted final chars, each → unique bytes, 0 collisions), and API completeness
  (`DecodeBase64Url` is the only base64url path; `Multikey` requires base58btc; no `Convert.FromBase64String`).
- Both could NOT break the claim. Both flagged the same caveat: the guarantee rests on the framework
  decoder's contract (`System.Buffers.Text.Base64Url.DecodeFromChars` validates, not masks), not NetCid's
  own code — which is exactly what the comment + pinning tests + audit note guard against (regression
  tripwire). The issue's defense-in-depth alternative (a NetCid-owned accumulator) remains available if the
  owner later wants the guarantee independent of the BCL; not implemented (issue's Step-0 guidance + Simplicity First).

Acceptance criteria:
- [x] Non-canonical base64url payloads throw `CidFormatException`; canonical payloads / existing round-trips unaffected.
- [x] `SECURITY_AUDIT.md` strictness claim updated to include base64url (coordinated with D1/#20).
