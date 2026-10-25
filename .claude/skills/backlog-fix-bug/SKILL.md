---
name: backlog-fix-bug
effort: high
description: >
  Fix a BUG-XXXX from backlog/items/: reproduce with a test, find the root cause, fix within
  constraints, build + test, mark done. TRIGGER: "fix bug", "resolve bug", a BUG-XXXX ID to
  fix. To file a new bug use /backlog-add-bug.
args:
  - name: bug_id
    description: "Optional bug ID (e.g. BUG-0001). If omitted, the highest-priority open unblocked bug is selected."
    required: false
---

# Fix Bug

## Step 0: pre-flight baseline

Follow §1 in `.claude/prompts/backlog-gates.md` (full suite; STOP if red).

## Step 1: identify the bug

- With a `bug_id`: read `backlog/items/<bug_id>.md`. If it does not exist or is not `open`, report and stop. If `blocked_by` is set, warn and stop on no.
- Without one: read `backlog/BACKLOG.md`, skip blocked bugs, pick the highest priority (critical > high > medium > low; lowest ID within a priority). None open: say so and stop.

Display: "Fixing BUG-XXXX: [Title] ([Area] / breaking: [yes/no])".

## Step 2: read the context

From the item collect Summary, Evidence, Expected behaviour, Location, Constraints and Definition of done. Read every Location file in full. The library is two source files, so trace the path yourself; do not spawn an explorer agent.

## Step 3: reproduce first

Write the failing test before changing production code: a sample type in `tests/VnumEnumeration.Tests/Data/TestData.cs` if one is needed, and a test in the class the item names (or the closest existing one). Run it and confirm it fails for the reason the item describes. If it passes, the item's diagnosis is wrong: stop, report, and update the item.

## Step 4: root cause and plan

Present:

```
## Root cause for BUG-XXXX
<one paragraph: what the code does, why that produces the symptom>

## Fix
1. <file>: <change>
## Decision points
- <options the item left open, with the one you propose>
## Docs
- README.md: <sections, or "none">
```

Ask: "Root cause found. Proceed with the fix? (yes / no / adjust)".

## Step 5: fix

Apply the minimal change that resolves the bug within the Constraints. Do not refactor around it. Keep documented exception types and message formats unless the item changes them, and keep XML doc comments on public members.

## Step 6: verify

Run the reproducing test, then follow §2 in `.claude/prompts/backlog-gates.md` (full build + full suite, no new warnings). If it cannot be made green, STOP.

## Step 7: public surface

Follow §6 in `.claude/prompts/backlog-gates.md`: README updated if the fix changes documented behaviour (and the item should then be `breaking: true`); CLAUDE.md Architecture updated if the internals it describes changed.

## Step 8: update the backlog

Follow §3 in `.claude/prompts/backlog-gates.md`: tick the Definition of done, add `## Resolution (date)` (root cause in one sentence, what changed, anything decided differently from the item), then `dotnet run tools/backlog-set-field.cs BUG-XXXX status done`.

Then check other open items that cite the files you changed:

```bash
grep -rl "<FileName>" backlog/items | xargs grep -l "^status: \(open\|in-progress\)"
```

Correct any that cite members, lines or behaviour the fix removed or replaced.

## Step 9: look for siblings

Ask whether the same shape exists elsewhere in the two source files (the same guard, the same dead branch, the same missing override). If so, say so in the report; file a BUG item for each confirmed instance (§4 IDs) rather than fixing it silently.

## Step 10: commit

Follow §5 in `.claude/prompts/backlog-gates.md`: branch off `main` if needed, propose `fix(<area>): <short title> (BUG-XXXX)`, and commit only after the user confirms.

## Step 11: report

- Bug ID and title
- Root cause (one sentence)
- Files changed
- Tests added or updated
- Build and test status
- Siblings found, if any

Close with: "BUG-XXXX is fixed. Run `/backlog-status` for the next item."
