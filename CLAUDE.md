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
  - Values are always stored as `long`. `Vnum<TEnum>` converts enums of any underlying integral type to/from `long` via `EnumToLong`/`LongToEnum`; `ulong` values above `long.MaxValue` throw `OverflowException`.
  - Instance discovery is reflection-based and lives in the nested `Cache<TVnum>.Build`: it reads **public static, DeclaredOnly** fields whose type is assignable to the Vnum type, deduplicates aliased fields by reference, and throws `InvalidOperationException` (via `NullField`) when a field's value is null. The null check also rejects a lookup made from inside the type's own static initializer, where the remaining fields are still unassigned; that one surfaces as a `TypeInitializationException` (see `NullFieldVnum` and `ReentrantVnum` in test data). The two cases are deliberately not told apart: doing so needs a stack walk, which was tried and dropped. Private static fields and fields on base classes are intentionally not discovered (see `PrivateVnum` in test data). The result is an immutable `Lookup<TVnum>` holding the item list and three dictionaries (by value, by code ordinal, by code ignore-case), filled with `TryAdd`: when values or codes are duplicated, the instance declared first wins and nothing throws.
  - The built table is published once per type with `Volatile.Write` and never invalidated. Every successful build is cached; there is no partial table.
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

Both workflows publish through NuGet trusted publishing, not a stored API key: the `NuGet/login` step exchanges the job's GitHub OIDC token for an API key that lasts one hour. This depends on two things outside the repo: a trusted publishing policy on nuget.org for each workflow file (a policy names one repository and one workflow file name, so renaming a workflow file breaks publishing until its policy is updated), and the `NUGET_USER` repository secret holding the nuget.org profile name. A tag-triggered run uses the workflow file from the tagged commit.

`README.md` at the repo root is packed into the NuGet package as its readme, so keep its API examples in sync when the public surface changes.

`CHANGELOG.md` records every change a consumer can observe. Add an entry under "Unreleased" in the same change that makes it, with breaking changes under "Breaking". Read that section before tagging: if it lists breaking changes, the tag must be a new major version. When tagging, rename "Unreleased" to the version and date and start a new empty one.
