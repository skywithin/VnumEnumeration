# Tools

Single-file C# apps for repo-level automation. Run with `dotnet run tools/<name>.cs` (.NET 10 SDK). Pass tool flags after `--` so `dotnet run` does not try to parse them: `dotnet run tools/backlog-lint.cs -- --fix`. New tooling goes here as a C# single-file app, not a shell or Python script, to keep the toolchain uniform.

| Tool | Purpose |
|------|---------|
| `backlog-next-id.cs` | Print the next sequential ID: `dotnet run tools/backlog-next-id.cs BL` prints `BL-0013` |
| `backlog-sync.cs` | Regenerate `backlog/BACKLOG.md` from item frontmatter. `-- --dry-run` reports without writing |
| `backlog-set-field.cs` | Set one frontmatter field on one or more items and rebuild the index. Setting `status` to a terminal value sets `completed`; setting it back clears it. Accepts ID ranges (`BL-0004..BL-0007`) and `--dry-run`. Example: `dotnet run tools/backlog-set-field.cs BL-0003 status done` |
| `backlog-lint.cs` | Validate every item: required fields, known values, ID/filename match, completed-date consistency, stale `blocked_by`, suspicious closes. `-- --fix` auto-corrects what it can and rebuilds the index; `-- --scope open` limits the report to open items |

The conventions these tools enforce are described in `backlog/BACKLOG.md` (item template, allowed values, size guide) and `CLAUDE.md` (Backlog section).
