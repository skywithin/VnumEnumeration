---
name: backlog-status
effort: low
description: >
  Show open backlog counts by priority and type, the breaking-change set, and recommend the
  top three items. TRIGGER: "backlog status", "what's in the backlog", "what should I work
  on next".
args: []
---

# Backlog Status

## Step 1: read the index

Read `backlog/BACKLOG.md` in full.

## Step 2: summarise open items

Count open items by the `## Open` subsections, which `tools/backlog-sync.cs` derives from `type`: Bugs (`bug`), Features & Enhancements (`feature`, `enhancement`), Test Coverage (`test-gap`), Tech Debt (`tech-debt`, `chore`), Investigations (`investigation`). Present:

```
## Backlog Status: <today's date>

| Category                | Critical | High | Medium | Low | Total |
|-------------------------|----------|------|--------|-----|-------|
| Bugs                    |          |      |        |     |       |
| Features & Enhancements |          |      |        |     |       |
| Test Coverage           |          |      |        |     |       |
| Tech Debt               |          |      |        |     |       |
| Investigations          |          |      |        |     |       |
| Total                   |          |      |        |     |       |
```

Omit all-zero rows. Below the table give the total open count and, if the `## Next major version` section is present, how many items are waiting for the next major version (count them once; they also appear under Open).

## Step 3: recommend the top three

Skip items with `blocked_by` set. Priority order:

1. Critical bugs
2. High bugs
3. High non-bug items, tech-debt and packaging first (broad blast radius for consumers)
4. Medium items, by type: bug, then test-gap, then tech-debt and chore, then enhancement and feature
5. Everything else by priority then ID

Read each candidate's file in `backlog/items/` to confirm it is still open and unblocked, and take a one-line summary from its Summary or Context section.

```
## Top 3: next actions

1. <ID>: <Title>
   <Type> | <Area> | <Priority> | <Size> | breaking: <yes/no>
   <One-line summary>
   Run `/backlog-fix-bug <ID>` or `/backlog-process-item <ID>`

---
(repeat)
```

Fewer than three open items: show what there is. None: say so and suggest `/backlog-add-item`.

## Step 4: breaking-change set

If any open items have `breaking: true`, list them together with a one-line note: these should ship in one major version, and the Resolution of each should name that version. Suggest which are small enough to bundle with the next one being worked.

## Step 5: quick win

Name one unblocked, size S (then M) item not in the top three and not an investigation, preferring bugs and tech-debt. Skip the section if none qualifies.

## Step 6: blocked items

If any open items have `blocked_by` set, list them in a table with the reason. Skip if none.
