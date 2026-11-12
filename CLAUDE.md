# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

`Skywithin.VnumEnumeration` is a NuGet library (one runtime assembly targeting `net8.0` and `net10.0`, plus a `netstandard2.0` Roslyn analyzer packed into the same package; the test project targets both frameworks) that provides `Vnum`, a base class for "smart enum" / enumeration-class types (value + code pairs), plus a `System.Text.Json` converter. Root namespace: `Skywithin.VnumEnumeration`.

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

All runtime library code lives in two files:

- `src/VnumEnumeration/Vnum.cs` — `Vnum` (non-generic base) and `Vnum<TEnum>` (enum-backed subclass).
  - Values are always stored as `long`. `Vnum<TEnum>` converts enums of any underlying integral type to/from `long` via `EnumToLong`/`LongToEnum`; `ulong` values above `long.MaxValue` throw `OverflowException`.
  - Instance discovery is reflection-based and lives in the nested `Cache<TVnum>.Build`: it reads **public static, DeclaredOnly** fields whose type is assignable to the Vnum type, deduplicates aliased fields by reference, and skips fields whose value is null. Private static fields and fields on base classes are intentionally not discovered (see `PrivateVnum` in test data). The result is an immutable `Lookup<TVnum>` holding the item list and three dictionaries (by value, by code ordinal, by code ignore-case), filled with `TryAdd`: when values or codes are duplicated, the instance declared first wins and nothing throws.
  - The built table is published with `Volatile.Write` and never invalidated, but only when no discovered field was null. A null field is taken to mean the lookup was made from inside the type's own static initializer (see `ReentrantVnum` in test data): the partial table is returned for that call and not cached, so the next call after initialization sees the full set. Known limitation: a field that is genuinely null looks the same, so such a type is never cached and every lookup re-runs reflection.
  - Lookups: the `TryFrom*` methods do the dictionary lookup and never throw (`TryFromCode(null, ...)` returns false). The throwing `FromValue`/`FromCode`/`FromEnum` wrap them and throw `InvalidOperationException` via `NotFound` with the message format `"'{value}' is not a valid {value|code} in {TypeName}"` — tests assert on this exact text. `FromCode(null)` throws `ArgumentNullException`. `FromEnum`/`TryFromEnum` convert the enum to `long` and defer to the value lookup.
  - Trimming/AOT: `TVnum` carries `[DynamicallyAccessedMembers(PublicFields)]` on every public lookup method, on `Cache<TVnum>` and on the JSON converter, so the trimmer keeps the fields discovery reads. Keep the annotation on any new entry point that reaches the cache.
  - Equality and ordering are by runtime type + `Value` (not `Code`). `CompareTo` orders by `Value` within one runtime type, puts `null` first, and throws `ArgumentException` across types; there are no `<`/`>` operators.
- `src/VnumEnumeration/Serialization/VnumJsonConverter.cs` — `VnumJsonConverter<TVnum>` writes the `Code` string; reads either a string code (case-sensitive) or a number (backward compatibility). Dictionary keys go through `WriteAsPropertyName`/`ReadAsPropertyName` and are codes only. Unknown codes on both paths throw through the private `InvalidCode` helper. `VnumJsonConverterFactory` handles every `Vnum` subtype so consumers register a single converter.

The analyzer is one file, `src/VnumEnumeration.Analyzers/VnumDeclarationAnalyzer.cs`. It reports `VNUM001` to `VNUM004` as warnings for declarations the next major version will reject at runtime (null field, lookup in the type's own static initializer, duplicate value or code, undeclared enum value). It only uses what the compiler can prove: `TryResolve` follows a field initializer's constructor through its `base(...)`/`this(...)` chain to the Vnum base constructor and carries constant arguments along; anything computed is skipped, so the analyzer must never be the only check for a rule. It is built against `Microsoft.CodeAnalysis.CSharp` 4.8.0 (the .NET 8 SDK compiler) so every SDK that can consume the package can load it; do not raise that version without a reason. The library csproj packs the DLL under `analyzers/dotnet/cs` through a `ProjectReference` with `ReferenceOutputAssembly="false"`, so it is not a package dependency. `VnumDeclarationAnalyzerTests` compiles sample sources in memory and asserts on the exact diagnostic messages.

Tests (`tests/VnumEnumeration.Tests`, xUnit):
- `Data/TestData.cs` holds all sample Vnum types used across tests, including deliberately broken ones (e.g. `BadSampleVnum` is missing an enum member).
- `Helpers/VnumTestingHelper<TVnum, TEnum>` is a reusable set of consistency checks (unique values, unique codes, enum↔Vnum coverage). Test classes inherit from it and call the checks inside `[Fact]` methods.

## Release process

Versioning is driven by git tags, not the `<Version>` in the csproj (CI overrides it with `-p:Version=`):
- `v1.2.3` tag → `.github/workflows/build-and-publish.yml` builds, tests, and publishes `1.2.3` to NuGet.org.
- `rc-v1.2.3` tag → `.github/workflows/pre-release-publish.yml` publishes `1.2.3-rc.<run_number>`.

`README.md` at the repo root is packed into the NuGet package as its readme, so keep its API examples in sync when the public surface changes.

`CHANGELOG.md` records every change a consumer can observe. Add an entry under "Unreleased" in the same change that makes it, with breaking changes under "Breaking". Read that section before tagging: if it lists breaking changes, the tag must be a new major version. When tagging, rename "Unreleased" to the version and date and start a new empty one.
