# Issue #16 — Bound JCS recursion depth (fix uncatchable StackOverflow DoS)

**Issue:** S1 — JCS has no recursion-depth limit → uncatchable StackOverflowException (DoS)
**Labels:** security, bug, jcs · **Milestone:** 1.6.0

## Problem

`JcsCanonicalizer` canonicalizes untrusted credential JSON but enforces no depth limit. Two
unbounded recursions (`ValidateNoUnrepresentableNumbers` on the `JsonNode` tree, runs first;
`WriteElement/WriteObject/WriteArray` on the `JsonElement` tree) overflow the stack on deeply
nested input. `StackOverflowException` is uncatchable in .NET → process crash → DoS.

## Plan

- [x] Add `private const int MaxDepth = 64` + shared `ThrowIfTooDeep(depth)` guard
- [x] Thread `depth` through `ValidateNoUnrepresentableNumbers` (entry passes 0)
- [x] Thread `depth` through `WriteElement`/`WriteObject`/`WriteArray` (both entries pass 0)
- [x] Cached `JsonSerializerOptions { MaxDepth = MaxDepth + 1 }` so our guard is the single
      source of truth at the boundary (STJ can't reject a node we accept with a different error)
- [x] XML docs: class `<remarks>` para + `<exception>` text on the public overloads
- [x] 6 tests (at-limit + over + far-over for `JsonNode`; at-limit + over + far-over for `JsonElement`)
- [x] Docs: CHANGELOG `### Security`, ARCHITECTURE + README "Input Limits"
- [x] `dotnet build` + `dotnet test` green
- [x] Adversarial-verification Workflow; resolve confirmed findings
- [x] Capture lesson from the confirmed finding in `tasks/lessons.md`

## Acceptance criteria (from issue)

- [x] Deep input throws `JcsFormatException`; never `StackOverflowException`
- [x] Default limit documented in XML docs; boundary (at-limit) test green
- [x] Both overloads (`JsonNode` + `JsonElement`) covered

## Review

**Outcome:** Done. Both unbounded recursions (`ValidateNoUnrepresentableNumbers` on the
`JsonNode` tree, which runs first; `WriteElement/WriteObject/WriteArray` on the `JsonElement`
tree) now route through one shared `ThrowIfTooDeep(depth)` guard at `MaxDepth = 64`. The
`JsonNode` path additionally serializes with cached `JsonSerializerOptions { MaxDepth = 65 }`
so a node at exactly the limit can never trip `System.Text.Json`'s default depth limit with a
non-`JcsFormatException` error; validation runs first, so the serializer only ever sees
depth ≤ 64.

**Files changed:**
- `NetCid/JcsCanonicalizer.cs` — const, shared guard, `depth` threaded through both walks,
  cached serialize options, XML docs (class `<remarks>` + three `<exception>` blocks).
- `NetCid.Tests/JcsCanonicalizerTests.cs` — 6 tests (`NestArrays` helper; at-limit/over/
  far-over for both overloads).
- `CHANGELOG.md`, `ARCHITECTURE.md`, `README.md` — documented the 64-level depth cap.

**Verification:** Unit suite 190 passed / 1 skipped (unrelated external-data conformance test);
integration suite 6/6; full solution Release build clean (0 warnings). Adversarial-verification
Workflow (8 agents) independently rebuilt, ran a separate-process runtime harness proving
200,000-deep input (array, object, `IBufferWriter` overload, and `JsonElement`) throws
`JcsFormatException` with the process exiting cleanly (exit 0), and the depth-64 boundary
canonicalizes.

**Adversarial findings:** 2 raised, 1 refuted, 1 confirmed (LOW, test-fidelity gap — addressed):
the original `JsonElement` over-limit test used depth 70, which exercises the policy gate but
would not overflow the stack on its own (the `JsonElement` `WriteElement` path overflows ~2,500
frames deep, far shallower than the `JsonNode` path which has the `SerializeToDocument`
boundary). Added `JsonElement_Far_Over_Limit_Throws_Without_StackOverflow` (depth 100,000) as
the genuine no-crash proof for that path, and clarified the boundary test's intent. The
production fix itself was confirmed sound by the runtime harness — the gap was test coverage only.

**Assembly/package version bump:** not done (release-time concern).

## Follow-on: optional canonical-output-byte cap (requested after initial review)

Implemented the issue's optional output guard for parity with the CID/Multibase input limits.

- `public const int JcsCanonicalizer.DefaultMaxOutputByteLength = 1_048_576` (1 MiB, maintainer's
  chosen default) + `maxOutputBytes` overloads on all three `Canonicalize` methods. Enforced via a
  single counting `LimitedBufferWriter : IBufferWriter<byte>` that throws `JcsFormatException`
  before committing the byte that would cross the limit (so a caller's `IBufferWriter` never
  exceeds it). `ValidateOutputLimit` rejects `maxOutputBytes < 1` with `ArgumentOutOfRangeException`.
  Default-on → documented potentially-breaking change.
- `Cid.FromCanonicalJson` gained an optional `maxOutputBytes` parameter (source-compatible).
- Tests: exact boundary (at/over/one-byte-over), `JsonElement` + `IBufferWriter` overloads,
  partial-commit ≤ limit, arg validation, default-cap-triggers (>1 MiB), and override-lifts-cap
  (`int.MaxValue`). Docs updated (CHANGELOG Added + Security, ARCHITECTURE, README).

**Adversarial workflow (7 agents):** real-execution harness proved the exact boundary, no-bypass
partial-commit, default-on enforcement, override path, arg validation, and integer-overflow safety.
3 findings raised, **0 refuted, 3 confirmed** — all resolved: (1) MEDIUM `Cid.FromCanonicalJson`
silently inherited the cap with no override → added the parameter + docs; (2) LOW no test pinned
the default-on cap → added `Default_Cap_Triggers_On_Output_Over_One_MiB`; (3) LOW no override/
overflow-safety test → added `Output_Above_Default_Succeeds_When_Limit_Raised`. Two reusable
lessons captured in `tasks/lessons.md`.

**Final verification:** Release solution build clean (0 warnings); unit 199 passed / 1 skipped;
integration 6/6.
