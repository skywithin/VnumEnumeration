# Changelog

Changes to `Skywithin.VnumEnumeration` that a consumer of the package can observe. Breaking changes are listed first; a release that contains any is tagged as a new major version. Releases up to 10.1.0 predate this file; see the git tags.

## Unreleased

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
