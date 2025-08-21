# Vnum

- Inspired by https://lostechies.com/jimmybogard/2008/08/12/enumeration-classes

## Overview

**VnumEnumeration** provides a base class (`Vnum`) for creating strongly-typed, enumeration-like constructs in C#. It enables you to define types that behave like enums but support additional metadata, such as display codes, and offer advanced lookup and parsing capabilities.

This library is designed for .NET 9 and leverages modern C# features for performance and type safety.

## Purpose

- **Strongly-Typed Enumerations**: Define custom types that encapsulate an integer value and a string code, similar to enums but with extensibility.
- **Reflection-Based Discovery**: Retrieve all instances of a Vnum type using reflection, with thread-safe caching for performance.
- **Flexible Lookup**: Find Vnum instances by value, code, or enum, with both strict and try-based methods.
- **Type Safety**: Generic support for enum-backed Vnum types via `Vnum<TEnum>`.

## Key Features

- **Value and Code**: Each Vnum instance has an integer value and a string code.
- **Static Lookup Methods**:
  - `GetAll<T>()`: Get all instances of a Vnum type.
  - `FromValue<T>(int value)`: Get instance by value.
  - `FromCode<T>(string code)`: Get instance by code.
  - `FromEnum<TVnum, TEnum>(TEnum value)`: Get instance by enum value.
  - `TryFromValue`, `TryFromCode`, `TryFromEnum`: Safe lookup variants.
- **Equality and Hashing**: Instances are compared by type and value.


