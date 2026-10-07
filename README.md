# Vnum

[![NuGet](https://img.shields.io/nuget/v/Skywithin.VnumEnumeration.svg)](https://www.nuget.org/packages/Skywithin.VnumEnumeration/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://github.com/skywithin/VnumEnumeration/blob/main/LICENSE)

**VnumEnumeration** provides a base class (`Vnum`) for strongly-typed, enumeration-like types in C#. A Vnum behaves like an enum but each instance carries a numeric value *and* a string code, can hold extra metadata, and supports lookup, parsing and JSON serialization.

## Installation

### .NET CLI
```bash
dotnet add package Skywithin.VnumEnumeration
```

### Package Manager
```powershell
Install-Package Skywithin.VnumEnumeration
```

### PackageReference
```xml
<PackageReference Include="Skywithin.VnumEnumeration" Version="10.*" />
```

## Features

- **Value and code**: every instance has a `long` value and a non-empty string code.
- **Reflection-based discovery**: all instances of a type are found automatically and cached (thread-safe) after the first lookup.
- **Flexible lookup**: find instances by value, code (optionally case-insensitive) or enum, with throwing and `Try*` variants.
- **Enum-backed types**: `Vnum<TEnum>` ties a Vnum to an enum of any underlying integral type (`byte`, `sbyte`, `short`, `ushort`, `int`, `uint`, `long`, `ulong`).
- **JSON serialization**: a `System.Text.Json` converter factory that writes codes and reads codes or numeric values.

## Usage

### Defining a Vnum

```csharp
public sealed class OrderStatus : Vnum
{
    private OrderStatus(long value, string code) : base(value, code) { }

    public static readonly OrderStatus Pending = new(1, "PENDING");
    public static readonly OrderStatus Processing = new(2, "PROCESSING");
    public static readonly OrderStatus Shipped = new(3, "SHIPPED");
    public static readonly OrderStatus Delivered = new(4, "DELIVERED");
}
```

Instances can carry extra data by adding properties and constructor parameters:

```csharp
public sealed class Currency : Vnum
{
    public string Symbol { get; }

    private Currency(long value, string code, string symbol) : base(value, code)
    {
        Symbol = symbol;
    } 

    public static readonly Currency Aud = new(1, "AUD", "$");
    public static readonly Currency Eur = new(2, "EUR", "€");
}
```

### Enum-backed Vnum

```csharp
public enum StatusId
{
    Pending = 1,
    Processing = 2,
    Shipped = 3,
    Delivered = 4
}

public sealed class OrderStatus : Vnum<StatusId>
{
    private OrderStatus(StatusId value, string code) : base(value, code) { }

    public static readonly OrderStatus Pending = new(StatusId.Pending, "PENDING");
    public static readonly OrderStatus Processing = new(StatusId.Processing, "PROCESSING");
    public static readonly OrderStatus Shipped = new(StatusId.Shipped, "SHIPPED");
    public static readonly OrderStatus Delivered = new(StatusId.Delivered, "DELIVERED");
}

StatusId id = OrderStatus.Shipped.Id;   // StatusId.Shipped
long value = OrderStatus.Shipped.Value; // 3
```

### Lookup

```csharp
// All instances, optionally filtered
IEnumerable<OrderStatus> all = Vnum.GetAll<OrderStatus>();
IEnumerable<OrderStatus> open = Vnum.GetAll<OrderStatus>(s => s.Value < 3);

// Throwing lookups (InvalidOperationException if no match)
var byValue = Vnum.FromValue<OrderStatus>(1);
var byCode = Vnum.FromCode<OrderStatus>("PENDING");
var byCodeIgnoreCase = Vnum.FromCode<OrderStatus>("pending", ignoreCase: true);
var byEnum = Vnum.FromEnum<OrderStatus, StatusId>(StatusId.Pending);

// Safe lookups
if (Vnum.TryFromValue<OrderStatus>(1, out var fromValue)) { /* ... */ }
if (Vnum.TryFromCode<OrderStatus>("PENDING", out var fromCode)) { /* ... */ }
if (Vnum.TryFromCode<OrderStatus>("pending", ignoreCase: true, out var fromCodeIgnoreCase)) { /* ... */ }
if (Vnum.TryFromEnum<OrderStatus, StatusId>(StatusId.Pending, out var fromEnum)) { /* ... */ }

// ToString() returns the code
Console.WriteLine(OrderStatus.Pending); // PENDING
```

### JSON serialization

Register `VnumJsonConverterFactory` once and it handles every Vnum type:

```csharp
public record Order(int Id, OrderStatus Status);

var options = new JsonSerializerOptions();
options.Converters.Add(new VnumJsonConverterFactory());

// Serializes to the code
string json = JsonSerializer.Serialize(new Order(1, OrderStatus.Pending), options);
// {"Id":1,"Status":"PENDING"}

// Deserializes from the code...
Order? fromCode = JsonSerializer.Deserialize<Order>("""{"Id":1,"Status":"PENDING"}""", options);

// ...or from the numeric value (backward compatibility)
Order? fromValue = JsonSerializer.Deserialize<Order>("""{"Id":1,"Status":1}""", options);
```

In ASP.NET Core:

```csharp
// Minimal APIs
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new VnumJsonConverterFactory()));

// MVC controllers
builder.Services.AddControllers().AddJsonOptions(o =>
    o.JsonSerializerOptions.Converters.Add(new VnumJsonConverterFactory()));
```

Reading notes:
- Codes are matched **case-sensitively**.
- `null` and an empty string both deserialize to `null`.
- An unknown code or value throws `JsonException`; for unknown codes the message lists the valid ones.

## Behavior

### Instance discovery

Instances are discovered by reflection over **`public static` fields declared directly on the Vnum type** (typically `public static readonly`). The following are ignored:

- non-public static fields
- static properties (`public static OrderStatus X { get; } = ...`)
- fields declared on a base class

Discovery runs once per type; the results are cached for the lifetime of the process.

### Equality

Two Vnums are equal when they have the same runtime type and the same `Value`. `Code` is not part of equality or hashing. `==` and `!=` follow the same rules.

### Errors

| Situation | Exception |
|---|---|
| `FromValue` / `FromCode` / `FromEnum` finds no match | `InvalidOperationException` |
| `FromCode` called with `null` | `ArgumentNullException` |
| Constructor called with a null, empty or whitespace code | `ArgumentException` |
| JSON contains an unknown code or value | `JsonException` |

The `Try*` methods return `false` instead of throwing in these cases.

## Limitations

- **No duplicate validation**: the library does not check that values or codes are unique. If two instances share a value or code, lookups return whichever is found first.
- **`ulong` overflow**: `ulong` enum values greater than `long.MaxValue` throw `OverflowException`.

## Supported frameworks

- .NET 10.0+

## Building from source

```bash
dotnet build VnumEnumeration.slnx
dotnet test tests/VnumEnumeration.Tests/VnumEnumeration.Tests.csproj
```

Releases are published to NuGet by GitHub Actions when a tag is pushed: `v1.2.3` publishes `1.2.3`, and `rc-v1.2.3` publishes the pre-release `1.2.3-rc.<run number>`.

## Acknowledgements

Inspired by Jimmy Bogard's [Enumeration classes](https://lostechies.com/jimmybogard/2008/08/12/enumeration-classes).

## License

[MIT](https://github.com/skywithin/VnumEnumeration/blob/main/LICENSE)
