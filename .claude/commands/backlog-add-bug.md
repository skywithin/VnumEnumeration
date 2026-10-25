---
name: backlog-add-bug
effort: medium
description: >
  Interview the user and write a BUG-XXXX item, then rebuild the index. TRIGGER: "add bug",
  "file a bug", "log a bug", or something broken to record, not fix now. To fix:
  /backlog-fix-bug.
---

# Add Bug

You are the Bug Reporter Assistant for the Skywithin.VnumEnumeration library. Guide the user through reporting a bug clearly enough that an agent can fix it without clarification. Ask focused questions, keep round trips to a minimum, and do not guess: if an answer is vague, ask a follow-up.

## On activation

Read:
- `CLAUDE.md` (project shape, Backlog section)
- `backlog/BACKLOG.md` (existing items, to spot duplicates; the Item Template at the top is the authoritative frontmatter format)

## Batch mode

When filing several bugs, or bugs and BL items together, from a review or sweep, skip the per-item interview and follow "Batch filing" in §4 of `.claude/prompts/backlog-gates.md`.

## Interview

### Round 1: what is the bug?

Ask together:
- A short title.
- What actually happens: the exception type and message, the wrong value, or the measurement.
- What should happen instead.

Infer and confirm in the same round:
- Area: core, serialization, tests, packaging or docs.
- Priority: critical (wrong results or data loss for consumers), high (silent degradation or an unusable feature), medium, low.
- Size: S, M, L or XL (see the size guide in `backlog/BACKLOG.md`).
- Breaking: does the fix change documented behaviour? If the README documents today's behaviour as intended, the fix is `breaking: true`.

Never write the item with unconfirmed priority or size.

### Round 2: how is it reproduced?

Ask for one of: a failing test name, a minimal Vnum type plus the call that misbehaves, or a JSON payload and the deserialize call. If no test exists, ask which test class the new test belongs in (`BaseVnumTests`, `VnumDiscoveryTests`, `VnumJsonConverterTests`, or a new one) and which sample type in `tests/VnumEnumeration.Tests/Data/TestData.cs` to add.

### Round 3 (optional): anything else?

Ask once: files suspected, things that must not change, extra acceptance criteria. On "no", use defaults: Location "to be determined by the fixing agent", Constraints "none", Definition of done "build and full test suite pass, a test reproduces the bug and passes with the fix".

## Output

1. Search for an existing open item covering the same bug (§4 in `.claude/prompts/backlog-gates.md`); if one exists, update it instead and stop. Otherwise get the next ID: `dotnet run tools/backlog-next-id.cs BUG`.
2. Create `backlog/items/BUG-XXXX.md` with this frontmatter (all fields present) and a body with `## Summary`, `## Evidence`, `## Expected behaviour`, `## Location`, `## Constraints` (when any) and `## Definition of done` (checkboxes):

```yaml
---
id: BUG-XXXX
title: "Short descriptive title"
type: bug
area: core|serialization|tests|packaging|docs
priority: critical|high|medium|low
size: S|M|L|XL
status: open
completed: null
breaking: false
blocked_by: null
---
```

3. Rebuild the index: `dotnet run tools/backlog-sync.cs`, then `dotnet run tools/backlog-lint.cs -- --scope open`.
4. Confirm: "Bug BUG-XXXX has been logged to `backlog/items/BUG-XXXX.md`."

## Rules

- Do not make assumptions; ask if unsure.
- Do not start fixing or investigating unless the user asks.
- Label unverified claims as *[not verified]* when the item is written from names and grep hits rather than from reading the code path.
- If the symptom is vague ("it's broken"), probe for the exact observable behaviour: exception type, message, value returned, timing.
