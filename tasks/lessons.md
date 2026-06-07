# Lessons

## 2026-02-20

- When a user asks to "fix all findings" from a security report, do not stop at recommendations. Resolve each finding in code/dependencies, re-run security commands, and update the report status to resolved.
- When the user references an external examples guide, inspect that guide directly and mirror its example taxonomy in the local `examples/` directory.

## 2026-05-21

- AGENTS.md "Plan First / Verify Plan" is non-negotiable. Write the plan to `tasks/todo.md` and report it to the user for verification **before** any implementation or file write — even when the issue body looks self-explanatory and the autonomous-bug-fixing principle would otherwise apply. Implementation-first is a correction-worthy violation.

## 2026-06-07

- RFC 8785 §3.2.2.3 mandates ECMA-262 §6.1.6.1.20 (`Number.prototype.toString`), which operates on IEEE-754 doubles. Any "integer fast path" that emits an integer literal verbatim is correct only for |x| ≤ 2^53 (the largest integer exactly representable as a double). Outside that range, the fast path silently violates the spec: a 22-digit integer and the same value re-typed with a `.0` suffix produce different canonical bytes, breaking the determinism guarantee. Gate or drop the fast path; do not assume "integer in CLR range" ⇒ "ECMA-identical output". Reason: caught by adversarial review on issue #13; previously masked by tests that pinned the buggy verbatim output.
- Adversarial review is non-negotiable per AGENTS.md §2 — launch it before declaring a non-trivial change done. The agent caught a high-severity correctness bug after green tests, green build, green example. "All tests pass" is necessary but not sufficient; the test suite can lock in a bug as readily as it can catch one.
