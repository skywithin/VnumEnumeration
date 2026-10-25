---
name: backlog-process-item
effort: high
description: >
  Implement a non-bug BL-XXXX item: investigate, plan for approval, implement, build + test,
  update README, mark done. TRIGGER: "process backlog item", "work on BL-XXXX". BUG items use
  /backlog-fix-bug; new items use /backlog-add-item.
args:
  - name: item_id
    description: "Optional item ID (e.g. BL-0003). If omitted, the highest-priority open unblocked BL item is selected."
    required: false
---

# Process Backlog Item

## Step 0: pre-flight baseline

Follow §1 in `.claude/prompts/backlog-gates.md` (full suite; STOP if red).

## Step 1: identify the item

- With an `item_id`: read `backlog/items/<item_id>.md`. If it does not exist, is not `open`, or is a BUG item, report and stop. If `blocked_by` is set, warn: "This item is blocked by: {blocked_by}. Proceed anyway?" and stop on no.
- Without one: read `backlog/BACKLOG.md`, skip blocked items, and pick the highest-priority open BL item (critical > high > medium > low; within a priority prefer tech-debt and test-gap over enhancement and feature). None open: say so and stop.

Display: "Processing BL-XXXX: [Title] ([Type] / [Area] / breaking: [yes/no])".

Apply §6 (public surface gate) in `.claude/prompts/backlog-gates.md`: an XL item without a reviewed proposal in its body stops here.

## Step 2: read the context

From the item collect type, Context, Scope, Decision points, Acceptance criteria and Locations. Read every file under Locations in full. This library is two source files plus tests, so investigate directly; do not spawn an explorer agent.

Also grep `README.md` and `CLAUDE.md` for every member or behaviour the item changes: both document the public surface and must change with it.

## Step 3: present the plan

```
## Implementation plan for BL-XXXX

Type: <type> | Breaking: <yes/no>

### Changes
1. <file>: <what changes>

### Tests
- <test class>: <what is added or changed>

### Docs
- README.md: <sections>  |  CLAUDE.md: <sections or "none">

### Decision points
- <each decision point from the item, with the option you propose and why>

### Risks
- <risks or trade-offs>
```

Then ask: "Plan ready. Proceed? (yes / no / adjust)". Incorporate feedback before continuing.

## Step 4: implement

By type:
- test-gap: tests only; production code changes only to make something testable.
- tech-debt and chore: the minimal change the acceptance criteria describe; preserve behaviour.
- enhancement and feature: implement to the acceptance criteria; add tests for every new path and error message. Keep the exact exception types and message formats the README documents unless the item changes them.
- investigation: the deliverable is findings, not code. Record them in the item under `## Findings` with evidence (file:line, commands, measurements) and a recommendation; file follow-up items via `/backlog-add-item` (§4 IDs) and reference them. Skip Step 5's test gate if only `backlog/` or docs changed.

Keep XML doc comments on every public member (the build has `GenerateDocumentationFile`).

## Step 5: verify

Run the new or changed tests first for fast feedback, then follow §2 in `.claude/prompts/backlog-gates.md` (full build + full suite, no new warnings). If it cannot be made green, STOP: do not mark done, do not commit.

## Step 6: public surface

Follow §6 in `.claude/prompts/backlog-gates.md`: README updated for any public change, Resolution notes the major version for breaking items, CLAUDE.md Architecture updated if the internals it describes changed.

## Step 7: update the backlog

Follow §3 in `.claude/prompts/backlog-gates.md`: tick the criteria, add `## Resolution (date)`, then `dotnet run tools/backlog-set-field.cs BL-XXXX status done`.

Then check other open items that cite the files you changed:

```bash
grep -rl "<FileName>" backlog/items | xargs grep -l "^status: \(open\|in-progress\)"
```

Read each hit; if it cites members, lines or behaviour this change removed or replaced, correct the item and say which item changed it.

## Step 8: commit

Follow §5 in `.claude/prompts/backlog-gates.md`: branch off `main` if needed, propose the message (`feat(<area>): ... (BL-XXXX)` or `chore(<area>): ... (BL-XXXX)`), and commit only after the user confirms.

## Step 9: report

- Item ID and title
- What was done (one sentence) and any decision point settled differently from the item
- Files changed
- Tests added or updated
- Build and test status
- Whether a major version bump is now pending

Close with: "BL-XXXX is done. Run `/backlog-status` for the next item."
