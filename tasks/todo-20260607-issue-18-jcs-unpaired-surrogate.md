# Issue #18 — JCS silently replaces invalid UTF-16 (unpaired surrogates) with U+FFFD

**Labels:** security, bug, jcs · **Milestone:** 1.6.0 · **Priority:** Low

## Problem (as filed)
`WriteString` uses the framework `Encoding.UTF8`, whose replacement fallback silently
rewrites an unpaired surrogate to U+FFFD (`EF BF BD`) instead of rejecting it. Two
malformed inputs can collapse to the same canonical bytes with no error — breaking the
determinism / collision-resistance guarantee that underpins CIDs, hashes, and signatures.

## Investigation findings (empirically verified against current `main`)

I reproduced the behavior before planning. The reality is broader than the issue text, and
**the issue's proposed fix (changing only `WriteString` to a throwing `StrictUtf8`) does NOT
fix the primary repro.** Two distinct `System.Text.Json` behaviors are in play:

| # | Input | Entry | Current result | Notes |
|---|-------|-------|----------------|-------|
| 1 | `JsonValue.Create("\uD800")` | JsonNode | `22 EF BF BD 22` (silent U+FFFD) | **VULNERABLE** — the issue's repro |
| 2 | `JsonValue.Create("\uDC00")` | JsonNode | `22 EF BF BD 22` (silent) | **VULNERABLE** |
| 3 | raw key `obj["\uD800"]=1` | JsonNode | `{"<U+FFFD>":1}` (silent) | **VULNERABLE** — issue omits keys |
| 4 | parse `"\uD800"` → `Canonicalize(element)` | JsonElement | throws `InvalidOperationException` | wrong exception type |
| 5 | parse `{"\uD800":1}` → element | JsonElement | throws `InvalidOperationException` | wrong exception type |
| 6 | `JsonNode.Parse("\"\\uD800\"")` | JsonNode | throws `InvalidOperationException` | wrong exception type |
| 7 | `JsonValue.Create("�")` (legit) | JsonNode | `22 EF BF BD 22` (OK) | **must stay OK** — not a surrogate |
| 8 | `é`, emoji pair `😀` | both | correct bytes | **must stay byte-identical** |

**Why the issue's fix fails cases 1–3:** on the JsonNode path the unpaired surrogate is
replaced with U+FFFD *upstream* by `JsonSerializer.SerializeToDocument`'s internal
`Utf8JsonWriter` (default encoder replaces ill-formed UTF-16). By the time `WriteString`
runs, `element.GetString()` already returns a **valid** U+FFFD, so a strict encoder there
sees nothing wrong. On the JsonElement path `element.GetString()` / `prop.Name` *throw*
`InvalidOperationException` **before** `WriteString` is ever reached. So `WriteString` never
sees a raw surrogate through any public entry point — a `StrictUtf8` guard there is dead code.

There is no single downstream choke point: the corruption (JsonNode) and the throw
(JsonElement) both happen inside STJ, upstream of `WriteString`. The fix must sit at the two
points where strings *enter* the canonicalizer.

## Design (correct, two symmetric guards)

**Goal:** every unpaired surrogate — in a string value OR an object member name, via the
JsonNode OR JsonElement entry — throws `JcsFormatException`; all valid input (including a
legitimately-supplied U+FFFD, accented chars, and valid surrogate *pairs*) is byte-for-byte
unchanged.

All edits are in `NetCid/JcsCanonicalizer.cs`.

1. **Add a surrogate-scan helper** (raw .NET strings):
   ```csharp
   private const string UnpairedSurrogateMessage =
       "JSON string contains invalid UTF-16 (unpaired surrogate); cannot canonicalize.";

   private static void ThrowIfUnpairedSurrogate(string value)
   {
       for (var i = 0; i < value.Length; i++)
       {
           if (char.IsHighSurrogate(value[i]))
           {
               if (i + 1 >= value.Length || !char.IsLowSurrogate(value[i + 1]))
                   throw new JcsFormatException(UnpairedSurrogateMessage);
               i++;                       // valid pair — skip the low surrogate
           }
           else if (char.IsLowSurrogate(value[i]))
           {
               throw new JcsFormatException(UnpairedSurrogateMessage);
           }
       }
   }
   ```

2. **JsonNode path** — extend the existing pre-serialization tree walk (runs *before*
   `SerializeToDocument`, so it sees the raw surrogate). Rename
   `ValidateNoUnrepresentableNumbers` → `ValidateNode` (it now validates numbers *and*
   strings *and* member names) and update its doc-comment + call site. In it:
   - object case: validate each `key` with `ThrowIfUnpairedSurrogate(key)` (fixes case 3);
   - value case: after the existing `double`/`float` NaN/∞ checks, add
     `else if (val.TryGetValue<string>(out var s) && s is not null) ThrowIfUnpairedSurrogate(s);`
     (fixes cases 1–2; raw strings round-trip through `TryGetValue<string>` without throwing).
   - Wrap the top-level `ValidateNode(node, 0)` call in
     `catch (InvalidOperationException ex) → throw new JcsFormatException(UnpairedSurrogateMessage, ex)`.
     This converts the *element-backed* (parsed) sub-cases inside a JsonNode tree (case 6 and
     a parsed surrogate key) — where `TryGetValue<string>` / `kv.Key` themselves throw
     `InvalidOperationException` — into the consistent `JcsFormatException`.

3. **JsonElement path** — wrap the `WriteElement(element, …, 0)` call in
   `Canonicalize(JsonElement, int)` (the single core overload all JsonElement entries funnel
   through) in `catch (InvalidOperationException ex) → throw JcsFormatException(…, ex)`. This
   converts cases 4–5 (`GetString()` / `prop.Name` throwing on parsed surrogates) to
   `JcsFormatException`. The local `ArrayBufferWriter` is discarded on throw, so no partial
   output escapes. (The only `InvalidOperationException` `WriteElement` can raise on a String
   /Object kind is this surrogate materialization — we dispatch by `ValueKind`, so it is a
   precise catch.)

   *Note:* the JsonNode path's own `WriteElement` (line ~181) operates on the already-cleaned
   `JsonDocument` (surrogates rejected in step 2 before serialization), so it cannot throw and
   needs no wrap. `WriteString` is left unchanged — no `StrictUtf8` (it would be untestable
   dead code given the two guards above).

### Why this is elegant / minimal
The pre-walk already exists for exactly this class of problem ("values STJ would silently
mishandle" — today NaN/±∞). Unpaired surrogates are the same shape, so they belong there.
The two `InvalidOperationException → JcsFormatException` wraps are one line each at the two
core overloads and unify the exception contract without threading checks through every helper.

## Tests to add (`NetCid.Tests/JcsCanonicalizerTests.cs`)
- `Unpaired_High_Surrogate_Throws` — `JsonValue.Create("\uD800")` ⇒ `JcsFormatException` (case 1).
- `Unpaired_Low_Surrogate_Throws` — `JsonValue.Create("\uDC00")` ⇒ `JcsFormatException` (case 2).
- `Unpaired_Surrogate_In_Object_Key_Throws` — raw `JsonObject` key ⇒ throws (case 3).
- `Unpaired_Surrogate_JsonElement_Value_Throws` — `CanonElement("\"\\uD800\"")` ⇒ throws (case 4).
- `Unpaired_Surrogate_JsonElement_Key_Throws` — `CanonElement("{\"\\uD800\":1}")` ⇒ throws (case 5).
- `Unpaired_Surrogate_Via_JsonNode_Parse_Throws` — `Canon("\"\\uD800\"")` ⇒ throws (case 6).
- `Unpaired_Surrogate_Nested_In_Array_Throws` — `["a", "\uD800"]` deep in tree ⇒ throws.
- `Surrogate_Error_Message_Mentions_UTF16` — assert message contains "UTF-16"/"surrogate".
- `Valid_Replacement_Char_U_FFFD_Still_Canonicalises` — `JsonValue.Create("�")` ⇒
  `22 EF BF BD 22`, **no throw** (case 7 — guards against over-rejection).
- Regression (already present, must stay green byte-for-byte): `é` (U+00E9 → `C3 A9`),
  emoji surrogate **pair** (`😀` → `F0 9F 98 80`). Confirm both still pass unchanged.

## Verification
1. `dotnet test` — full suite green; new surrogate tests pass; `é`/emoji regression byte-identical.
2. Confirm pre-fix the new value/key tests would FAIL (silent U+FFFD) — i.e. they actually
   exercise the failure mode, not just the policy (lessons 2026-06-07).
3. Build examples (`examples/jcs-interface`) — no API break.
4. **Adversarial review subagent** (AGENTS.md §2, non-negotiable): attempt to find an
   unpaired surrogate that still produces silent U+FFFD output through any public entry
   (`Canonicalize(JsonNode)`, `Canonicalize(JsonElement)`, the `IBufferWriter` overloads,
   `Cid.FromCanonicalJson`), and confirm no valid input regressed.

## Docs / changelog
- `CHANGELOG.md` → `## [Released]` → `### Security`: new entry crediting #18, noting that
  unpaired surrogates now throw `JcsFormatException` (string values and member names, both
  JsonNode and JsonElement paths) instead of silently collapsing to U+FFFD.
- `README.md` (the `JcsCanonicalizer` hardening paragraph, ~line 83): add a clause that it
  rejects invalid UTF-16 (unpaired surrogates) rather than substituting U+FFFD.
- No public API surface change → no `ARCHITECTURE.md`/SPEC change expected (confirm during review).

## Acceptance criteria
- [x] Unpaired surrogates (value or member name; JsonNode or JsonElement) throw `JcsFormatException`.
- [x] Valid strings — incl. legitimate U+FFFD, accents, and valid surrogate pairs — unchanged byte-for-byte.
- [x] No public entry point produces silent U+FFFD or leaks a non-`JcsFormatException` for bad UTF-16 *(within scope — see residual note below)*.
- [x] Tests, examples, docs, changelog updated; adversarial review passed.

---

## Review / Results

**Branch:** `feature/issue-18-jcs-unpaired-surrogate` (created after the user reminded me not to work on `main`).

**Note:** `origin/main` had advanced to `96932cd` (issue #17 duplicate-member-name feature) since session start; the fix was integrated alongside it — the new `InvalidOperationException` catch coexists with #17's `ArgumentException` (duplicate-key) catch on the JsonNode path.

### What shipped (`NetCid/JcsCanonicalizer.cs`)
- `ThrowIfUnpairedSurrogate(string)` helper + `UnpairedSurrogateMessage` const.
- `ValidateNoUnrepresentableNumbers` → renamed `ValidateNode`; now also validates object **member names** and string **values**, plus a **`char`-backed** value check (`TryGetValue<char>` + `char.IsSurrogate`) — added after adversarial review found the char bypass.
- JsonNode call site: `catch (InvalidOperationException) → JcsFormatException` (converts parsed/element-backed surrogate sub-cases), ahead of the existing duplicate-key `ArgumentException` catch.
- JsonElement overload: `WriteElement` wrapped in `catch (InvalidOperationException) → JcsFormatException`.
- XML `<remarks>` documents the guard and the `JsonValueCustomized<T>` limitation.

### Tests (`NetCid.Tests/JcsCanonicalizerTests.cs`) — 14 added; full suite **223 passed, 1 skipped + 6 integration**
- Verified the 9 assertion tests **fail pre-fix** (stashed source, ran, restored) — they exercise the real failure mode, not just policy.

### Adversarial review (two rounds)
- **Round 1** found a real DEFEAT: `char`-backed `JsonValue.Create('\uD800')` silently produced U+FFFD → **fixed** + tests added.
- **Round 2** confirmed the char fix closed, found a wider class: `JsonValueCustomized<T>` (`JsonValue.Create(rawObject/dictionary/collection)`). Empirically confirmed **unfixable** without rejecting valid object-backed nodes (STJ substitutes U+FFFD at the serialize step before any inspection; no STJ path preserves the raw surrogate). **Parsed-JSON threat model is unaffected** — `JsonNode.Parse`/`JsonElement`/`Cid.FromCanonicalJson` never produce `JsonValueCustomized<T>`.

### Residual limitation (user decision: document, don't reject)
A `JsonValue` wrapping a raw CLR object can still serialize a surrogate to U+FFFD. Documented in XML remarks, README, and CHANGELOG. Not a fix-by-rejection because that would also reject valid surrogate-free POCO-backed nodes.

### Docs
- `CHANGELOG.md` `[1.6.0] → Security`: #18 entry (incl. the residual-limitation note).
- `README.md`: surrogate clause in the `JcsCanonicalizer` hardening paragraph.
- `examples/jcs-interface/Program.cs`: added an unpaired-surrogate rejection case to the negative-cases demo (verified output).
- `tasks/lessons.md`: 4 new lessons (branch-first; verify the issue's own proposed fix; two-round adversarial review; type-enumeration validation leaks).
