using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace VnumEnumeration;

public abstract class Vnum<TEnum> : Vnum where TEnum : struct, Enum
{
    /// <summary>
    /// Gets the enumeration value of the Vnum item.
    /// </summary>
    public TEnum Id => (TEnum)Enum.ToObject(typeof(TEnum), Value);

    protected Vnum() : base() { }
    protected Vnum(int value, string code) : base(value, code) { }
    protected Vnum(TEnum value, string code) : base(Convert.ToInt32(value), code) { }
}

/// <summary>
/// Provides a base class for creating enumeration-like constructs 
/// with strongly-typed values and display codes.
/// </summary>
public abstract class Vnum : IEquatable<Vnum>
{
    /// <summary>
    /// A thread-safe cache that stores arrays of <see cref="Vnum"/> instances for each derived type.
    /// This cache is used to improve performance by avoiding repeated reflection operations when retrieving
    /// all instances of a specific type (e.g., via <see cref="GetAll(Type)"/>). 
    /// 
    /// Once populated, subsequent calls for the same type will retrieve the cached array instead of 
    /// invoking reflection, which can be computationally expensive. The cache uses a 
    /// <see cref="ConcurrentDictionary{TKey, TValue}"/> to ensure thread safety in multi-threaded environments.
    /// 
    /// Note: This cache is static, meaning it's shared across all instances of <see cref="Vnum"/>.
    /// </summary>
    private static readonly ConcurrentDictionary<Type, object[]> _cache = new();

    /// <summary>
    /// Gets the numeric value of the Vnum item.
    /// </summary>
    public int Value { get; }

    /// <summary>
    /// Gets the string code of the Vnum item.
    /// </summary>
    public string Code { get; } = null!;

    protected Vnum() { } // Required for reflection
    protected Vnum(int value, string code)
    {
        Value = value;
        Code = code ?? throw new ArgumentNullException(nameof(code));
    }

    /// <summary>
    /// Returns the code of the Vnum item.
    /// </summary>
    public override string ToString() => Code;

    /// <summary>
    /// Retrieves all Vnum instances of a given type <typeparamref name="T"/>.
    /// </summary>
    public static IEnumerable<T> GetAll<T>(Func<T, bool>? predicate = null) where T : Vnum, new()
    {
        var all = GetAll(typeof(T)).Cast<T>();
        return predicate is null ? all : all.Where(predicate);
    }

    private static object[] GetAll(Type type)
    {
        //1. Validate the type parameter.
        if (type is null)
        {
            throw new ArgumentNullException(
                paramName: nameof(type),
                message: "Type cannot be null");
        }

        if (!typeof(Vnum).IsAssignableFrom(type))
        {
            throw new ArgumentException(
                message: $"Type '{type.Name}' is not a valid {nameof(Vnum)} type",
                paramName: nameof(type));
        }

        //2. Check if the requested Vnum instances for type are already in _cache.
        //3. If not cached, use reflection to get all public, static fields on type that represent Vnum instances.
        //4. Store and return the results as an array.
        return _cache.GetOrAdd(
            key: type,
            t => t.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                  .Where(f => t.IsAssignableFrom(f.FieldType))
                  .Select(f => f.GetValue(null)!)
                  .ToArray());
    }

    /// <summary>
    /// Retrieves a Vnum instance by its numeric value.
    /// </summary>
    public static T FromValue<T>(int value) where T : Vnum, new() =>
        Parse<T, int>(
            value: value,
            description: nameof(value),
            predicate: item => item.Value == value);

    /// <summary>
    /// Attempts to retrieve a Vnum instance by its numeric value.
    /// </summary>
    public static bool TryFromValue<T>(int value, out T vnum) where T : Vnum, new() =>
        TryParse(() => FromValue<T>(value), out vnum);

    /// <summary>
    /// Retrieves a Vnum instance by its enum value.
    /// </summary>
    public static TVnum FromEnum<TVnum, TEnum>(TEnum value)
        where TVnum : Vnum<TEnum>, new()
        where TEnum : struct, Enum
        => FromValue<TVnum>(Convert.ToInt32(value));

    /// <summary>
    /// Attempts to retrieve a Vnum instance by its enum value.
    /// </summary>
    public static bool TryFromEnum<TVnum, TEnum>(TEnum value, out TVnum vnum)
        where TVnum : Vnum<TEnum>, new()
        where TEnum : struct, Enum
        => TryParse(() => FromValue<TVnum>(Convert.ToInt32(value)), out vnum);

    /// <summary>
    /// Retrieves a Vnum instance by its code.
    /// </summary>
    public static T FromCode<T>(string code, bool ignoreCase = false) where T : Vnum, new()
    {
        Func<T, bool> predicate =
            (item) =>
                ignoreCase
                    ? item.Code.Equals(code, StringComparison.OrdinalIgnoreCase)
                    : item.Code == code;

        return Parse(
            value: code,
            description: nameof(code),
            predicate: predicate);
    }

    /// <summary>
    /// Retrieves a Vnum instance by its code (case sensitive).
    /// </summary>
    public static T FromCode<T>(string code) where T : Vnum, new() =>
        FromCode<T>(code, ignoreCase: false);

    /// <summary>
    /// Attempts to retrieve a Vnum instance by its code.
    /// </summary>
    public static bool TryFromCode<T>(string code, bool ignoreCase, out T vnum) where T : Vnum, new() =>
        TryParse(() => FromCode<T>(code, ignoreCase), out vnum);

    /// <summary>
    /// Attempts to retrieve a Vnum instance by its code (case sensitive).
    /// </summary>
    public static bool TryFromCode<T>(string code, out T vnum) where T : Vnum, new() =>
        TryFromCode(code, ignoreCase: false, out vnum);

    /// <summary>
    /// Parses a Vnum instance based on a specified predicate.
    /// This method is used internally to find a matching Vnum item.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if no matching Vnum instance is found.</exception>
    private static T Parse<T, K>(
        K value,
        string description,
        Func<T, bool> predicate) where T : Vnum, new()
    {
        var matchingItem = GetAll<T>().FirstOrDefault(predicate);

        if (matchingItem is null)
        {
            throw new InvalidOperationException($"'{value}' is not a valid {description} in {typeof(T).Name}");
        }

        return matchingItem;
    }

    private static bool TryParse<T>(Func<T> parseFunc, out T result) where T : Vnum, new()
    {
        try
        {
            result = parseFunc();
            return true;
        }
        catch (ArgumentNullException) // Narrow catch. Avoid masking catastrophic exceptions
        {
            result = null!;
            return false;
        }
        catch (InvalidOperationException)
        {
            result = null!;
            return false;
        }
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current Vnum instance.
    /// </summary>
    public override bool Equals(object? obj) =>
        obj is Vnum other &&
        Equals(other);

    /// <summary>
    /// Determines whether the specified Vnum is equal to the current Vnum instance.
    /// </summary>
    public bool Equals(Vnum? other) =>
        other != null &&
        GetType().Equals(other.GetType()) &&
        Value == other.Value;

    /// <summary>
    /// Returns the hash code for the Vnum item, based on its value.
    /// </summary>
    public override int GetHashCode() => HashCode.Combine(GetType(), Value);
}