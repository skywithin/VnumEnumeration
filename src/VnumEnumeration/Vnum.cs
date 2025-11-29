using System.Collections.Concurrent;
using System.Reflection;

namespace Skywithin.VnumEnumeration;

/// <summary>
/// Represents a strongly-typed value-number (Vnum) associated with a specific enumeration type.
/// </summary>
/// <remarks>This class provides a type-safe way to associate an enum value with a Vnum, ensuring that only
/// valid enumeration values are used. The <see cref="Id"/> property allows access to the enumeration value
/// corresponding to the Vnum.</remarks>
/// <typeparam name="TEnum">The enumeration type that defines the valid values for this Vnum. Must be a struct and an enumeration.</typeparam>
public abstract class Vnum<TEnum> : Vnum where TEnum : struct, Enum
{
    /// <summary>
    /// Gets the enumeration value of the Vnum item.
    /// </summary>
    public TEnum Id => LongToEnum(Value);

    /// <summary>
    /// Initializes a new instance of the <see cref="Vnum{TEnum}"/> class with the specified value and code.
    /// </summary>
    protected Vnum(long value, string code) : base(value, code) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Vnum{TEnum}"/> class with the specified enum value and code.
    /// </summary>
    protected Vnum(TEnum value, string code) : base(EnumToLong(value), code) { }

    /// <summary>
    /// Converts an enum value to its long representation based on the enum's underlying type.
    /// </summary>
    internal static long EnumToLong(TEnum value)
    {
        var underlyingType = Enum.GetUnderlyingType(typeof(TEnum));
        var typeCode = Type.GetTypeCode(underlyingType);

        return typeCode switch
        {
            TypeCode.SByte => (long)(sbyte)(object)value,
            TypeCode.Byte => (long)(byte)(object)value,
            TypeCode.Int16 => (long)(short)(object)value,
            TypeCode.UInt16 => (long)(ushort)(object)value,
            TypeCode.Int32 => (long)(int)(object)value,
            TypeCode.UInt32 => (long)(uint)(object)value,
            TypeCode.Int64 => (long)(object)value,
            TypeCode.UInt64 => ConvertUInt64((ulong)(object)value),
            _ => throw new NotSupportedException($"Unsupported enum underlying type: {underlyingType}")
        };

        static long ConvertUInt64(ulong v)
        {
            if (v > long.MaxValue)
                throw new OverflowException($"Enum value exceeds Int64.MaxValue.");
            return (long)v;
        }
    }

    /// <summary>
    /// Converts a long value back to the enum type based on the enum's underlying type.
    /// </summary>
    private static TEnum LongToEnum(long value)
    {
        var underlyingType = Enum.GetUnderlyingType(typeof(TEnum));
        var typeCode = Type.GetTypeCode(underlyingType);

        object convertedValue = typeCode switch
        {
            TypeCode.SByte => Convert.ToSByte(value),
            TypeCode.Byte => Convert.ToByte(value),
            TypeCode.Int16 => Convert.ToInt16(value),
            TypeCode.UInt16 => Convert.ToUInt16(value),
            TypeCode.Int32 => Convert.ToInt32(value),
            TypeCode.UInt32 => Convert.ToUInt32(value),
            TypeCode.Int64 => Convert.ToInt64(value),
            TypeCode.UInt64 => Convert.ToUInt64(value),
            _ => throw new NotSupportedException($"Unsupported enum underlying type: {underlyingType}")
        };

        return (TEnum)Enum.ToObject(typeof(TEnum), convertedValue);
    }
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
    public long Value { get; }

    /// <summary>
    /// Gets the string code of the Vnum item.
    /// </summary>
    public string Code { get; } = null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="Vnum"/> class with the specified value and code.
    /// </summary>
    protected Vnum(long value, string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Code cannot be null or whitespace", nameof(code));
        }

        Value = value;
        Code = code;
    }

    /// <summary>
    /// Returns the code of the Vnum item.
    /// </summary>
    public override string ToString() => Code;

    /// <summary>
    /// Retrieves all Vnum instances of a given type <typeparamref name="TVnum"/>.
    /// </summary>
    public static IEnumerable<TVnum> GetAll<TVnum>() where TVnum : Vnum
    {
        return GetAll(typeof(TVnum)).Cast<TVnum>();
    }

    /// <summary>
    /// Retrieves all Vnum instances of a given type <typeparamref name="TVnum"/> that satisfy the specified predicate.
    /// </summary>
    public static IEnumerable<TVnum> GetAll<TVnum>(Func<TVnum, bool> predicate) where TVnum : Vnum
    {
        return GetAll(typeof(TVnum)).Cast<TVnum>().Where(predicate);
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
    public static TVnum FromValue<TVnum>(long value) where TVnum : Vnum =>
        Parse<TVnum, long>(
            value: value,
            description: nameof(value),
            predicate: item => item.Value == value);

    /// <summary>
    /// Attempts to retrieve a Vnum instance by its numeric value.
    /// </summary>
    public static bool TryFromValue<T>(long value, out T vnum) where T : Vnum =>
        TryParse(() => FromValue<T>(value), out vnum);

    /// <summary>
    /// Retrieves a Vnum instance by its enum value.
    /// </summary>
    public static TVnum FromEnum<TVnum, TEnum>(TEnum value)
        where TVnum : Vnum<TEnum>
        where TEnum : struct, Enum
        => FromValue<TVnum>(Vnum<TEnum>.EnumToLong(value));

    /// <summary>
    /// Attempts to retrieve a Vnum instance by its enum value.
    /// </summary>
    public static bool TryFromEnum<TVnum, TEnum>(TEnum value, out TVnum vnum)
        where TVnum : Vnum<TEnum>
        where TEnum : struct, Enum
        => TryParse(() => FromValue<TVnum>(Vnum<TEnum>.EnumToLong(value)), out vnum);

    /// <summary>
    /// Retrieves a Vnum instance by its code.
    /// </summary>
    public static TVnum FromCode<TVnum>(string code, bool ignoreCase = false) where TVnum : Vnum
    {
        if (code is null)
        {
            throw new ArgumentNullException(nameof(code));
        }

        Func<TVnum, bool> predicate =
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
    /// Attempts to retrieve a Vnum instance by its code.
    /// </summary>
    public static bool TryFromCode<TVnum>(string code, bool ignoreCase, out TVnum vnum) where TVnum : Vnum =>
        TryParse(() => FromCode<TVnum>(code, ignoreCase), out vnum);

    /// <summary>
    /// Attempts to retrieve a Vnum instance by its code (case sensitive).
    /// </summary>
    public static bool TryFromCode<TVnum>(string code, out TVnum vnum) where TVnum : Vnum =>
        TryFromCode(code, ignoreCase: false, out vnum);

    // Parses a Vnum instance based on a specified predicate. This method is used internally to find a matching Vnum item.
    private static TVnum Parse<TVnum, K>(
        K value,
        string description,
        Func<TVnum, bool> predicate) where TVnum : Vnum
    {
        var matchingItem = GetAll<TVnum>().FirstOrDefault(predicate);

        if (matchingItem is null)
        {
            throw new InvalidOperationException($"'{value}' is not a valid {description} in {typeof(TVnum).Name}");
        }

        return matchingItem;
    }

    // Attempts to parse a value of type <typeparamref name="T"/> using the specified parsing function.
    private static bool TryParse<TVnum>(Func<TVnum> parseFunc, out TVnum vnum) where TVnum : Vnum
    {
        try
        {
            vnum = parseFunc();
            return true;
        }
        catch (ArgumentNullException) // Narrow catch. Avoid masking catastrophic exceptions
        {
            vnum = null!;
            return false;
        }
        catch (ArgumentException)
        {
            vnum = null!;
            return false;
        }
        catch (InvalidOperationException)
        {
            vnum = null!;
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

    /// <summary>
    /// Determines whether two Vnum instances are equal.
    /// </summary>
    public static bool operator ==(Vnum? left, Vnum? right) =>
    left?.Equals(right) ?? right is null;

    /// <summary>
    /// Determines whether two Vnum instances are not equal.
    /// </summary>
    public static bool operator !=(Vnum? left, Vnum? right) =>
        !(left == right);
}