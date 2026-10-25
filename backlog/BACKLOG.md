# Backlog

Backlog for the Skywithin.VnumEnumeration library. Each item is a standalone file in `backlog/items/`. Everything below the size guide is generated from item frontmatter by `dotnet run tools/backlog-sync.cs`; do not hand-edit it.

## Quick Commands

| Action | Command | Example |
|--------|---------|---------|
| File a bug | `/backlog-add-bug` | `/backlog-add-bug` |
| Add enhancement / tech debt | `/backlog-add-item` | `/backlog-add-item` |
| Fix a bug | `/backlog-fix-bug BUG-XXXX` | `/backlog-fix-bug BUG-0001` |
| Process a non-bug item | `/backlog-process-item BL-XXXX` | `/backlog-process-item BL-0003` |
| View backlog status | `/backlog-status` | `/backlog-status` |

## Item Template

```yaml
---
id: BL-XXXX          # or BUG-XXXX; get it from: dotnet run tools/backlog-next-id.cs BL|BUG
title: "Short title" # REQUIRED; used by backlog-sync for the index
type: enhancement    # enhancement|feature|tech-debt|test-gap|bug|investigation|chore
area: core           # core|serialization|tests|packaging|docs
priority: medium     # critical|high|medium|low
size: M              # S|M|L|XL
status: open         # open|in-progress|done|rejected|wont-fix
completed: null      # YYYY-MM-DD; set by backlog-set-field.cs on a terminal status
breaking: false      # true if the change alters the public API or documented behaviour
blocked_by: null     # free text: what blocks this item (an item ID or an external dependency)
---
```

Change `status` with `dotnet run tools/backlog-set-field.cs <ID> status <value>`; it sets `completed` and rebuilds this index. Do not hand-edit `status` or `completed`. Validate everything with `dotnet run tools/backlog-lint.cs`.

**Areas:** `core` is `src/VnumEnumeration/Vnum.cs`; `serialization` is `src/VnumEnumeration/Serialization/`; `tests` is the test project; `packaging` is the csproj, NuGet metadata and GitHub workflows; `docs` is README.md and CLAUDE.md.

**Size guide:**

| Size | Scope | Typical effort |
|------|-------|----------------|
| `S` | One or two files, mechanical change | < 30 min |
| `M` | Library change plus tests plus README | 30 min - 2 hours |
| `L` | Cross-cutting: base class, converter, tests and README together | Half day |
| `XL` | Public API redesign; needs a written proposal in the item and a major version | Day+ |

- `BL-` prefix for all non-bug items, `BUG-` for bugs, each numbered sequentially.
- `breaking: true` items are collected under "Next major version" below and should ship together in one major version. Releases are cut by git tag (`v1.2.3`, `rc-v1.2.3`); see README.

---

## Next major version

Open items with `breaking: true`: they change the public API or documented behaviour, so ship them together in one major version. They are listed again under Open.

| ID | Title | Type | Area | Priority | Size |
|----|-------|------|------|----------|------|
| BL-0004 | Return IReadOnlyList<TVnum> from GetAll<TVnum>() | enhancement | core | low | S |
| BL-0006 | JSON: treat an empty string as an invalid code instead of null, and remove the dead null branches | enhancement | serialization | low | S |
| BL-0012 | Evaluate a self-referencing generic API shape (OrderStatus.FromCode) against the current Vnum.FromCode<OrderStatus> entry points | investigation | core | low | M |

## Open

### Features & Enhancements

| ID | Title | Type | Area | Priority | Size |
|----|-------|------|------|----------|------|
| BL-0004 | Return IReadOnlyList<TVnum> from GetAll<TVnum>() | enhancement | core | low | S |
| BL-0006 | JSON: treat an empty string as an invalid code instead of null, and remove the dead null branches | enhancement | serialization | low | S |
| BL-0007 | JSON: explain the failure when the target type declares no instances (abstract intermediate types) | enhancement | serialization | low | S |

### Tech Debt

| ID | Title | Type | Area | Priority | Size |
|----|-------|------|------|----------|------|
| BL-0009 | NuGet packaging hygiene: SourceLink, deterministic CI builds, symbols package, release notes | chore | packaging | low | S |

### Investigations

| ID | Title | Type | Area | Priority | Size |
|----|-------|------|------|----------|------|
| BL-0012 | Evaluate a self-referencing generic API shape (OrderStatus.FromCode) against the current Vnum.FromCode<OrderStatus> entry points | investigation | core | low | M |

## Done (recent)

Older completed items stay in `backlog/items/`; this table shows the most recent completions.

| ID | Title | Type | Area | Priority | Size |
|----|-------|------|------|----------|------|
| BUG-0002 | Vnum cannot be used as a JSON dictionary key: the converter lacks the property-name overrides | bug | serialization | medium | S |
| BUG-0001 | A null public static field permanently disables the lookup cache, so every lookup runs reflection | bug | core | high | M |
| BL-0011 | Update the CLAUDE.md architecture notes to match the current discovery and lookup design | chore | docs | medium | S |
| BL-0010 | Fix the CS1723 XML doc warnings in the JSON converter and the CS8602 warnings in the tests | chore | serialization | low | S |
| BL-0008 | Multi-target net8.0 and net10.0 so LTS consumers can install the package | enhancement | packaging | medium | S |
| BL-0005 | Implement IComparable<Vnum> so instances sort by Value | enhancement | core | low | S |
| BL-0003 | Annotate reflection for trimming and AOT and declare IsAotCompatible | tech-debt | core | medium | M |
| BL-0002 | Reject values that are not declared enum members in the Vnum<TEnum> long constructor | enhancement | core | medium | S |
| BL-0001 | Fail fast on duplicate values and codes when the lookup table is built | enhancement | core | high | M |

