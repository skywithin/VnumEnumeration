# Changelog

Changes to `Skywithin.VnumEnumeration` that a consumer of the package can observe. Breaking changes are listed first; a release that contains any is tagged as a new major version. Releases up to 10.1.0 predate this file; see the git tags.

## Unreleased

Contains breaking changes: the next release needs a new major version.

### Breaking

- **A `null` public static field on a Vnum type now throws.** The first lookup on the type throws `InvalidOperationException` naming the field. Before, such a type worked but rebuilt its lookup table by reflection on every call. Make the field non-public, or give it a real instance.
- **A lookup from inside a Vnum type's own static initializer now fails.** `public static readonly OrderStatus Default = Vnum.FromValue<OrderStatus>(1);` fails at type load with a `TypeInitializationException`, because `Default` is still unassigned when the lookup runs. Reference the field instead: `Default = Pending`. This and the previous entry are one rule: a null field and a not-yet-assigned field cannot be told apart reliably, and guessing wrong would silently lose instances, so both are rejected.
- **Duplicate values and codes now throw.** If two instances of a type share a value, share a code, or have codes that differ only by case, the first lookup on the type throws `InvalidOperationException` naming the type and the clashing members. Before, the instance declared first won silently. Calling `Vnum.GetAll<T>()` in a unit test is enough to check a type.
- **The `Vnum<TEnum>(long, string)` constructor rejects undeclared enum values.** A value that is not a declared member of `TEnum` throws `ArgumentOutOfRangeException`. `[Flags]` enums are exempt, and the `Vnum<TEnum>(TEnum, string)` constructor is unchanged.

## 10.2.0 (2026-11-12)

No breaking changes.

### Added

- A `net8.0` build alongside `net10.0`, so .NET 8 and .NET 9 applications can install the package.
- `Vnum` implements `IComparable<Vnum>` and `IComparable`, ordering by `Value` within one type. Comparing instances of two different Vnum types throws `ArgumentException`.
- Trimming support: the lookup methods and the JSON converter are annotated so the trimmer keeps the fields discovery reads, and the package is marked `IsAotCompatible`. Checked with a trimmed publish; a native AOT publish has not been tested yet.

### Fixed

- A Vnum can be used as a JSON dictionary key (`Dictionary<OrderStatus, int>`); keys are written and read as codes. Before, serializing or deserializing such a dictionary threw `NotSupportedException`.

### Planned for the next major version

These work today and will throw in the next major version. Nothing changes in this release; the list is here so they can be fixed ahead of time.

- A `null` public static field on a Vnum type. Today it silently disables caching for that type (see README "Limitations"). Make the field non-public, or give it a real instance.
- A lookup from inside a Vnum type's own static initializer, such as `public static readonly OrderStatus Default = Vnum.FromValue<OrderStatus>(1);`. Reference the field instead: `Default = Pending`.
- Two instances of a type that share a value, share a code, or have codes that differ only by case. Today the instance declared first wins silently.
- A `Vnum<TEnum>(long, string)` constructor value that is not a declared member of `TEnum` (`[Flags]` enums excepted).

Calling `Vnum.GetAll<T>()` for each type in a unit test will be enough to catch all four once they throw.
