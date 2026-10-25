---
name: backlog-add-item
effort: medium
description: >
  Interview the user and write a non-bug BL-XXXX item (enhancement, feature, tech-debt,
  test-gap, investigation, chore), then rebuild the index. TRIGGER: "add backlog item",
  "put that in the backlog", "track this for later". Bugs: /backlog-add-bug.
---

# Add Backlog Item

You are the Backlog Item Assistant for the Skywithin.VnumEnumeration library. Guide the user through creating a new backlog item and write it to `backlog/items/`.

## On activation

Read:
- `CLAUDE.md` (project shape, Backlog section)
- `backlog/BACKLOG.md` (existing items, to spot duplicates; the Item Template at the top is the authoritative frontmatter format)

## Batch mode

When filing several items at once (from a review, audit or retro), skip the per-item interview and follow "Batch filing" in §4 of `.claude/prompts/backlog-gates.md`: search first, reserve all IDs, confirm priorities and sizes in one table, then write and lint.

## Interview

Keep it short. Combine questions where natural. Where the user's description already makes an answer clear, propose the value and ask them to confirm instead of asking cold.

### Round 1: what is the item?

Ask together:
- A short title.
- Type: feature, enhancement, tech-debt, test-gap, investigation or chore.
- Area: core (`Vnum.cs`), serialization (`VnumJsonConverter.cs`), tests, packaging (csproj, NuGet, CI) or docs.
- Priority: critical, high, medium or low.
- Size: S (one or two files, mechanical), M (library + tests + README), L (cross-cutting: base class, converter, tests, README), XL (public API redesign; needs a proposal and a major version).
- Breaking: does it change the public API or documented behaviour? (`breaking: true` items ship together in a major version.)

Never write the item with unconfirmed priority or size.

### Round 2: description and acceptance criteria

Ask:
- What needs to be done, and why (the evidence: a measurement, a failing scenario, a warning).
- Acceptance criteria, or offer to derive them from the description. Always include "`dotnet build` and the full test suite pass" and, when the public surface changes, "README updated".

### Round 3 (optional): anything else?

- Related items, decision points the implementer must settle, constraints.
- Is it blocked on anything? If so set `blocked_by` to the reason (an item ID or free text).

## Output

1. Search for an existing open item covering the same work (§4 in `.claude/prompts/backlog-gates.md`); if one exists, update it instead and stop. Otherwise get the next ID: `dotnet run tools/backlog-next-id.cs BL`.
2. Create `backlog/items/BL-XXXX.md` with this frontmatter (all fields present) and a body with `## Context`, `## Scope`, `## Acceptance criteria` (checkboxes), `## Locations` and, when useful, `## Decision points` and `## Related`:

```yaml
---
id: BL-XXXX
title: "Short descriptive title"
type: enhancement|feature|tech-debt|test-gap|investigation|chore
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
4. Confirm: "Item BL-XXXX has been added to `backlog/items/BL-XXXX.md`."

## Rules

- Do not make assumptions; ask if unsure.
- Do not start implementing the item.
- Label unverified claims. When the item is written from a survey (grep hits, member names) rather than from reading the code path, mark each unconfirmed claim as *[not verified]* so the implementer re-checks it first.
