# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

`Skywithin.VnumEnumeration` is a single-assembly NuGet library (target `net10.0`) that provides `Vnum`, a base class for "smart enum" / enumeration-class types (value + code pairs), plus a `System.Text.Json` converter. Root namespace: `Skywithin.VnumEnumeration`.

## Commands

```bash
dotnet build VnumEnumeration.slnx
dotnet test tests/VnumEnumeration.Tests/VnumEnumeration.Tests.csproj

# Single test class / single test (xUnit)
dotnet test tests/VnumEnumeration.Tests --filter "FullyQualifiedName~VnumJsonConverterTests"
dotnet test tests/VnumEnumeration.Tests --filter "FullyQualifiedName~SampleVnumTests.SomeTestName"

dotnet pack src/VnumEnumeration/VnumEnumeration.csproj -c Release -p:Version=x.y.z
```

The solution file is the new XML `.slnx` format (requires a recent .NET SDK). There is no separate lint step; the library builds with `Nullable` enabled and `GenerateDocumentationFile`, so public members need XML doc comments to avoid CS1591 warnings.

## Architecture

All library code lives in two files:

- `src/VnumEnumeration/Vnum.cs` — `Vnum` (non-generic base) and `Vnum<TEnum>` (enum-backed subclass).
  - Values are always stored as `long`. `Vnum<TEnum>` converts enums of any underlying integral type to/from `long` via `EnumToLong`/`LongToEnum`; `ulong` values above `long.MaxValue` throw `OverflowException`.
  - Instance discovery is reflection-based: `GetAllVnums` reads **public static, DeclaredOnly** fields whose type is assignable to the Vnum type. Private static fields and fields on base classes are intentionally not discovered (see `PrivateVnum` in test data). Results are cached per type in a static `ConcurrentDictionary` that is never invalidated.
  - All lookups (`FromValue`, `FromCode`, `FromEnum`) go through the private `Parse` helper, which throws `InvalidOperationException` with the message format `"'{value}' is not a valid {value|code} in {TypeName}"` — tests assert on this exact text. The `TryFrom*` variants wrap the throwing versions and catch only `ArgumentException`/`ArgumentNullException`/`InvalidOperationException`.
  - Equality is by runtime type + `Value` (not `Code`).
- `src/VnumEnumeration/Serialization/VnumJsonConverter.cs` — `VnumJsonConverter<TVnum>` writes the `Code` string; reads either a string code (case-sensitive) or a number (backward compatibility). `VnumJsonConverterFactory` handles every `Vnum` subtype so consumers register a single converter.

Tests (`tests/VnumEnumeration.Tests`, xUnit):
- `Data/TestData.cs` holds all sample Vnum types used across tests, including deliberately broken ones (e.g. `BadSampleVnum` is missing an enum member).
- `Helpers/VnumTestingHelper<TVnum, TEnum>` is a reusable set of consistency checks (unique values, unique codes, enum↔Vnum coverage). Test classes inherit from it and call the checks inside `[Fact]` methods.

## Release process

Versioning is driven by git tags, not the `<Version>` in the csproj (CI overrides it with `-p:Version=`):
- `v1.2.3` tag → `.github/workflows/build-and-publish.yml` builds, tests, and publishes `1.2.3` to NuGet.org.
- `rc-v1.2.3` tag → `.github/workflows/pre-release-publish.yml` publishes `1.2.3-rc.<run_number>`.

`README.md` at the repo root is packed into the NuGet package as its readme, so keep its API examples in sync when the public surface changes.
