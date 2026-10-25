# Claude Code configuration

Commands live in `.claude/commands/` (single `.md` files), skills in `.claude/skills/<name>/SKILL.md`, and prompt files that commands and skills load by path in `.claude/prompts/`.

Naming is prefix-based, the same convention as the Mayflower platform repo: `backlog-*` for backlog management. Add a new prefix group when a new family appears rather than leaving names unprefixed.

| Name | Kind | Purpose |
|------|------|---------|
| `backlog-add-bug` | command | Interview the user and write a `BUG-XXXX` item |
| `backlog-add-item` | command | Interview the user and write a `BL-XXXX` item |
| `backlog-fix-bug` | skill | Fix a `BUG-XXXX` item end to end |
| `backlog-process-item` | skill | Implement a `BL-XXXX` item end to end |
| `backlog-status` | skill | Open-item counts and the top three next actions |

Shared gates (test baseline, build + test gate, marking done, IDs, branch and commit rules, public-surface rule) live in `.claude/prompts/backlog-gates.md`. Change a gate there, not in the individual skills.
