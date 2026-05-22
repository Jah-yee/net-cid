# Lessons

## 2026-02-20

- When a user asks to "fix all findings" from a security report, do not stop at recommendations. Resolve each finding in code/dependencies, re-run security commands, and update the report status to resolved.
- When the user references an external examples guide, inspect that guide directly and mirror its example taxonomy in the local `examples/` directory.

## 2026-05-21

- AGENTS.md "Plan First / Verify Plan" is non-negotiable. Write the plan to `tasks/todo.md` and report it to the user for verification **before** any implementation or file write — even when the issue body looks self-explanatory and the autonomous-bug-fixing principle would otherwise apply. Implementation-first is a correction-worthy violation.
