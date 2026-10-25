# Backlog Gates: shared rules

Canonical versions of the gates shared by `/backlog-fix-bug` and `/backlog-process-item`, and referenced by `/backlog-add-item`, `/backlog-add-bug` and `/backlog-status`. Skills point here with "Follow §N in `.claude/prompts/backlog-gates.md`" and add only their own delta.

---

## §1 Pre-flight test baseline

Before any code change, confirm the suite is green:

```bash
dotnet test tests/VnumEnumeration.Tests/VnumEnumeration.Tests.csproj
```

If the baseline is red, STOP and report the failures. Do not start work on a broken baseline.

---

## §2 Build + test gate

Run before marking an item done and before committing:

```bash
dotnet build VnumEnumeration.slnx
dotnet test tests/VnumEnumeration.Tests/VnumEnumeration.Tests.csproj
```

- Always the full suite. `--filter` runs are fine while iterating but do not satisfy this gate.
- The library builds with `GenerateDocumentationFile`; do not add new warnings (CS1591, CS1723 and the like). Fix them in the same change.
- If the build or any test fails, diagnose and fix. Do not skip or suppress.
- If it cannot be made green: STOP. Do not mark the item done, do not commit. Report what remains.

---

## §3 Mark the item done (or closed)

1. Tick every satisfied acceptance checkbox in the item body (`[ ]` to `[x]`). If one cannot be satisfied, annotate it with a short reason rather than leaving it silently unticked.
2. Add a `## Resolution (YYYY-MM-DD)` section at the end of the item: what was done, what was decided differently from the item and why, and the version the change ships in if known.
3. Set the status with the tool; it sets `completed` to today and rebuilds `backlog/BACKLOG.md`:

   ```bash
   dotnet run tools/backlog-set-field.cs <ID> status done
   ```

   Use `rejected` or `wont-fix` instead of `done` when closing without implementing. Several IDs can be passed at once.

Never hand-edit the `status` or `completed` fields, and never hand-edit `backlog/BACKLOG.md` below its size guide.

---

## §4 New item IDs

Search for an existing item first. Before creating any BL or BUG item, grep the open items for the key identifiers (type, member, file names) and a couple of title words:

```bash
grep -rliE "<Identifier>|<title word>" backlog/items | xargs grep -l "^status: \(open\|in-progress\)"
```

If an open item already covers the work, update it (refresh its scope, correct stale names, add a dated "Current state" note) instead of filing a new one.

Then take the next sequential ID from the tool; do not scan `backlog/items/` by hand:

```bash
dotnet run tools/backlog-next-id.cs BL    # non-bug items
dotnet run tools/backlog-next-id.cs BUG   # bugs
```

When creating several items in one pass, run it again after writing each file.

Batch filing (several items from one review or sweep):

1. Run the existing-item search for every proposed item first. Drop or merge any that an open item already covers.
2. Reserve the whole ID range before writing any file: take the next BL and BUG IDs, assign them in order, and write the mapping down. Items that reference each other must use the reserved IDs, never a guess.
3. Confirm every priority and size in one table (one row per item) instead of one interview per item. Never write an item with an unconfirmed priority or size when the user is available to confirm; when working unattended, set them from the evidence and list them in the final report so the user can adjust with `backlog-set-field.cs`.
4. Write the files, then run `dotnet run tools/backlog-sync.cs` once and `dotnet run tools/backlog-lint.cs -- --scope open`. Finally run `backlog-next-id.cs` for each prefix and check it returns the ID after the last one reserved; anything else means a file is missing or misnamed.

---

## §5 Branch guard + commit format

Branch guard. Before committing, check the branch:

```bash
git branch --show-current
```

If it is `main`, do not commit there. Create a branch first (`<type>/<ID>-<short-slug>`, for example `fix/BUG-0001-null-field-cache`) and commit on it.

Confirm before committing. `/backlog-process-item` and `/backlog-fix-bug` do not commit on their own: report the result, propose the commit message, and commit only after the user says so. Until then leave the work uncommitted.

Commit format. Conventional Commits, scoped by the item's `area`, with the item ID at the end of the subject:

```
<prefix>(<area>): <short imperative description> (<ID>)
```

| Item type | Prefix |
|-----------|--------|
| bug | `fix` |
| enhancement, feature | `feat` |
| tech-debt, test-gap, chore, investigation | `chore` |

Examples: `fix(core): cache the lookup table when a static field is null (BUG-0001)`, `feat(serialization): support Vnum dictionary keys (BUG-0002)`. Backlog-only edits use `chore(backlog): ...`. Never use `--no-verify`.

---

## §6 Public surface gate

This library is consumed through NuGet, so apply this before marking an item done:

- If the change touches a public member or documented behaviour, update `README.md` in the same change. The README is packed into the NuGet package, so a stale README ships to consumers.
- If the item has `breaking: true`, say in the Resolution section that it needs a major version bump. Breaking items are grouped under "Next major version" in `backlog/BACKLOG.md` and should ship together; do not tag a release that contains only some of them unless the user decides to.
- If the change alters how discovery, caching or lookup work internally, update the Architecture section of `CLAUDE.md` so later sessions read an accurate model.
- `size: XL` items (a public API redesign) need a written proposal in the item body, reviewed by the user, before implementation starts. Stop and ask for it if missing.
