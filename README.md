# Vnum

[![NuGet](https://img.shields.io/nuget/v/Skywithin.VnumEnumeration.svg)](https://www.nuget.org/packages/Skywithin.VnumEnumeration/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

- Inspired by https://lostechies.com/jimmybogard/2008/08/12/enumeration-classes

## Installation

### Package Manager
```powershell
Install-Package Skywithin.VnumEnumeration
```

### .NET CLI
```bash
dotnet add package Skywithin.VnumEnumeration
```

### PackageReference
```xml
<PackageReference Include="Skywithin.VnumEnumeration" Version="1.0.0" />
```

## Overview

**VnumEnumeration** provides a base class (`Vnum`) for creating strongly-typed, enumeration-like constructs in C#. It enables you to define types that behave like enums but support additional metadata, such as display codes, and offer advanced lookup and parsing capabilities.

This library supports .NET 9.0+ and leverages modern C# features for performance and type safety.

## Purpose

- **Strongly-Typed Enumerations**: Define custom types that encapsulate a long integer value and a string code, similar to enums but with extensibility.
- **Reflection-Based Discovery**: Retrieve all instances of a Vnum type using reflection, with thread-safe caching for performance.
- **Flexible Lookup**: Find Vnum instances by value, code, or enum, with both strict and try-based methods.
- **Type Safety**: Generic support for enum-backed Vnum types via `Vnum<TEnum>`.
- **Universal Enum Support**: Supports all enum underlying types (byte, sbyte, short, ushort, int, uint, long, ulong).
- **JSON Serialization**: Built-in support for JSON serialization with `System.Text.Json`.

## Key Features

- **Value and Code**: Each Vnum instance has an integer value and a string code.
- **Static Lookup Methods**:
  - `GetAll<T>()`: Get all instances of a Vnum type.
  - `FromValue<T>(long value)`: Get instance by value.
  - `FromCode<T>(string code)`: Get instance by code.
  - `FromEnum<TVnum, TEnum>(TEnum value)`: Get instance by enum value.
  - `TryFromValue`, `TryFromCode`, `TryFromEnum`: Safe lookup variants.
- **Equality and Hashing**: Instances are compared by type and value.
- **JSON Serialization**: Automatic serialization to string codes and deserialization from codes or numeric values.

## Usage Examples

### Basic Vnum Definition

```csharp
public sealed class OrderStatus : Vnum
{
    public OrderStatus() { }
    private OrderStatus(int value, string code) : base(value, code) { }

    public static readonly OrderStatus Pending = new(1, "PENDING");
    public static readonly OrderStatus Processing = new(2, "PROCESSING");
    public static readonly OrderStatus Shipped = new(3, "SHIPPED");
    public static readonly OrderStatus Delivered = new(4, "DELIVERED");
}
```

### Enum-Backed Vnum

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
    public OrderStatus() { }
    private OrderStatus(StatusId value, string code) : base(value, code) { }

    public static readonly OrderStatus Pending = new(StatusId.Pending, "PENDING");
    public static readonly OrderStatus Processing = new(StatusId.Processing, "PROCESSING");
    public static readonly OrderStatus Shipped = new(StatusId.Shipped, "SHIPPED");
    public static readonly OrderStatus Delivered = new(StatusId.Delivered, "DELIVERED");
}
```

### Lookup Operations

```csharp
// Get all instances
var allStatuses = Vnum.GetAll<OrderStatus>();

// Find by value
var status = Vnum.FromValue<OrderStatus>(1);

// Find by code
var status = Vnum.FromCode<OrderStatus>("PENDING");

// Safe lookup
if (Vnum.TryFromValue<OrderStatus>(1, out var status))
{
    // Use status
}

// Enum conversion
var status = Vnum.FromEnum<OrderStatus, StatusId>(StatusId.Pending);
```

### JSON Serialization

```csharp
// Configure JSON serialization
var options = new JsonSerializerOptions();
options.Converters.Add(new VnumJsonConverterFactory());

// Serialization
var order = new { Id = 1, Status = OrderStatus.Pending };
var json = JsonSerializer.Serialize(order, options);
// Result: {"Id":1,"Status":"PENDING"}

// Deserialization (supports both string codes and numeric values)
var json = "{\"Id\":1,\"Status\":\"PENDING\"}";
var order = JsonSerializer.Deserialize<Order>(json, options);

// Also works with numeric values for backward compatibility
var json = "{\"Id\":1,\"Status\":1}";
var order = JsonSerializer.Deserialize<Order>(json, options);
```

## Limitations

- **ULong Overflow**: `ulong` enum values exceeding `long.MaxValue` will throw `OverflowException`

## Supported Frameworks

- .NET 9.0+
