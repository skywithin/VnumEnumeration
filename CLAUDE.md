# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

`Skywithin.VnumEnumeration` is a single-assembly NuGet library (targets `net8.0` and `net10.0`; the test project targets both too) that provides `Vnum`, a base class for "smart enum" / enumeration-class types (value + code pairs), plus a `System.Text.Json` converter. Root namespace: `Skywithin.VnumEnumeration`.

## Commands

```bash
dotnet build VnumEnumeration.slnx
dotnet test tests/VnumEnumeration.Tests/VnumEnumeration.Tests.csproj

# Single test class / single test (xUnit)
dotnet test tests/VnumEnumeration.Tests --filter "FullyQualifiedName~VnumJsonConverterTests"
dotnet test tests/VnumEnumeration.Tests --filter "FullyQualifiedName~SampleVnumTests.SomeTestName"

# One target framework only (the test project runs on net8.0 and net10.0 by default)
dotnet test tests/VnumEnumeration.Tests -f net8.0

dotnet pack src/VnumEnumeration/VnumEnumeration.csproj -c Release -p:Version=x.y.z
```

The solution file is the new XML `.slnx` format (requires a recent .NET SDK). There is no separate lint step; the library builds with `Nullable` enabled and `GenerateDocumentationFile`, so public members need XML doc comments (CS1591). The library csproj sets `IsAotCompatible` and `TreatWarningsAsErrors`, so any warning in the library fails the build, including the IL2xxx/IL3xxx trim/AOT analyzer warnings for unannotated reflection. The test project does not treat warnings as errors, but keep it warning-free too.

## Architecture

All library code lives in two files:

- `src/VnumEnumeration/Vnum.cs` — `Vnum` (non-generic base) and `Vnum<TEnum>` (enum-backed subclass).
  - Values are always stored as `long`. `Vnum<TEnum>` converts enums of any underlying integral type to/from `long` via `EnumToLong`/`LongToEnum`; `ulong` values above `long.MaxValue` throw `OverflowException`. The `long` constructor also rejects values that are not declared members (`Enum.IsDefined`) with `ArgumentOutOfRangeException`, except for `[Flags]` enums; the `TEnum` constructor trusts its argument.
  - Instance discovery is reflection-based and lives in the nested `Cache<TVnum>.Build`: it reads **public static, DeclaredOnly** fields whose type is assignable to the Vnum type, deduplicates aliased fields by reference, and skips fields whose value is null. Private static fields and fields on base classes are intentionally not discovered (see `PrivateVnum` in test data). The result is an immutable `Lookup<TVnum>` holding the item list and three dictionaries (by value, by code ordinal, by code ignore-case). A duplicate value, a duplicate code, or two codes differing only by case throws `InvalidOperationException` from the `Lookup` constructor, i.e. from the first lookup on that type; a failed build is never cached, so every later lookup throws too.
  - The built table is published once per type with `Volatile.Write` and never invalidated. One exception: a lookup made from inside the type's own static initializer (a `.cctor` frame of `TVnum` on the stack, checked with `DiagnosticMethodInfo` only when a null field was seen) returns a partial table that is not cached, so the next call after initialization sees the full set (see `ReentrantVnum` in test data).
  - Lookups: the `TryFrom*` methods do the dictionary lookup and never throw (`TryFromCode(null, ...)` returns false). The throwing `FromValue`/`FromCode`/`FromEnum` wrap them and throw `InvalidOperationException` via `NotFound` with the message format `"'{value}' is not a valid {value|code} in {TypeName}"` — tests assert on this exact text. `FromCode(null)` throws `ArgumentNullException`. `FromEnum`/`TryFromEnum` convert the enum to `long` and defer to the value lookup.
  - Trimming/AOT: `TVnum` carries `[DynamicallyAccessedMembers(PublicFields)]` on every public lookup method, on `Cache<TVnum>` and on the JSON converter, so the trimmer keeps the fields discovery reads. Keep the annotation on any new entry point that reaches the cache.
  - Equality and ordering are by runtime type + `Value` (not `Code`). `CompareTo` orders by `Value` within one runtime type, puts `null` first, and throws `ArgumentException` across types; there are no `<`/`>` operators.
- `src/VnumEnumeration/Serialization/VnumJsonConverter.cs` — `VnumJsonConverter<TVnum>` writes the `Code` string; reads either a string code (case-sensitive) or a number (backward compatibility). Dictionary keys go through `WriteAsPropertyName`/`ReadAsPropertyName` and are codes only. Unknown codes on both paths throw through the private `InvalidCode` helper. `VnumJsonConverterFactory` handles every `Vnum` subtype so consumers register a single converter.

Tests (`tests/VnumEnumeration.Tests`, xUnit):
- `Data/TestData.cs` holds all sample Vnum types used across tests, including deliberately broken ones (e.g. `BadSampleVnum` is missing an enum member).
- `Helpers/VnumTestingHelper<TVnum, TEnum>` is a reusable set of consistency checks (unique values, unique codes, enum↔Vnum coverage). Test classes inherit from it and call the checks inside `[Fact]` methods.

## Release process

Versioning is driven by git tags, not the `<Version>` in the csproj (CI overrides it with `-p:Version=`):
- `v1.2.3` tag → `.github/workflows/build-and-publish.yml` builds, tests, and publishes `1.2.3` to NuGet.org.
- `rc-v1.2.3` tag → `.github/workflows/pre-release-publish.yml` publishes `1.2.3-rc.<run_number>`.

`README.md` at the repo root is packed into the NuGet package as its readme, so keep its API examples in sync when the public surface changes.

## Backlog

Issues and planned work live in `backlog/`: one markdown file per item in `backlog/items/` with YAML frontmatter, and `backlog/BACKLOG.md` as the generated index (`dotnet run tools/backlog-sync.cs`). IDs are `BL-XXXX` for non-bug items and `BUG-XXXX` for bugs. The item template with the allowed values is at the top of `backlog/BACKLOG.md`; `tools/backlog-lint.cs` enforces `id`, `title`, `type`, `area`, `priority`, `size` and `status`.

- Change `status` with `dotnet run tools/backlog-set-field.cs <ID> status <value>`; it sets `completed` and rebuilds the index. Never hand-edit `status`, `completed` or the generated part of `BACKLOG.md`.
- `breaking: true` marks an item that changes the public API or documented behaviour. The index groups them under "Next major version"; they should ship together in one major version.
- `/backlog-add-bug` and `/backlog-add-item` file items; `/backlog-fix-bug <ID>` and `/backlog-process-item <ID>` work them; `/backlog-status` gives an overview. The shared gates (test baseline, build + test, marking done, IDs, branch and commit rules, public-surface rule) are in `.claude/prompts/backlog-gates.md`.
- Commits that resolve an item use Conventional Commits scoped by area with the ID at the end: `fix(core): ... (BUG-0001)`, `feat(serialization): ... (BL-0007)`, `chore(packaging): ... (BL-0009)`.
- Do not cite backlog IDs in code comments, XML docs or test names; the reference rots once the item closes. IDs belong in `backlog/`, commit messages and `TODO` markers.

## Tools

Single-file C# apps in `tools/`, run with `dotnet run tools/<name>.cs` (pass tool flags after `--`). See `tools/README.md` for the list.
