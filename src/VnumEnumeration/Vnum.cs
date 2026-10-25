using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

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
    private static readonly TypeCode UnderlyingTypeCode = Type.GetTypeCode(Enum.GetUnderlyingType(typeof(TEnum)));

    // Combined [Flags] values are legitimate but are not "defined" members, so the defined-member check skips them.
    private static readonly bool IsFlagsEnum = typeof(TEnum).IsDefined(typeof(FlagsAttribute), inherit: false);

    /// <summary>
    /// Gets the enumeration value of the Vnum item.
    /// </summary>
    public TEnum Id { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Vnum{TEnum}"/> class with the specified value and code.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="value"/> does not fit in the underlying type of <typeparamref name="TEnum"/>, or is not a
    /// declared member of <typeparamref name="TEnum"/>. The declared-member check is skipped for <c>[Flags]</c> enums,
    /// whose combined values are not declared members.
    /// </exception>
    protected Vnum(long value, string code) : base(value, code)
    {
        Id = LongToEnum(value);

        if (!IsFlagsEnum && !Enum.IsDefined(Id))
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                $"Value {value} is not a defined member of enum {typeof(TEnum).Name}.");
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Vnum{TEnum}"/> class with the specified enum value and code.
    /// </summary>
    protected Vnum(TEnum value, string code) : base(EnumToLong(value), code)
    {
        Id = value;
    }

    /// <summary>
    /// Converts an enum value to its long representation based on the enum's underlying type.
    /// </summary>
    internal static long EnumToLong(TEnum value)
    {
        // Enums have the same size and layout as their underlying type, so reinterpreting avoids boxing.
        return UnderlyingTypeCode switch
        {
            TypeCode.SByte => Unsafe.As<TEnum, sbyte>(ref value),
            TypeCode.Byte => Unsafe.As<TEnum, byte>(ref value),
            TypeCode.Int16 => Unsafe.As<TEnum, short>(ref value),
            TypeCode.UInt16 => Unsafe.As<TEnum, ushort>(ref value),
            TypeCode.Int32 => Unsafe.As<TEnum, int>(ref value),
            TypeCode.UInt32 => Unsafe.As<TEnum, uint>(ref value),
            TypeCode.Int64 => Unsafe.As<TEnum, long>(ref value),
            TypeCode.UInt64 => ConvertUInt64(Unsafe.As<TEnum, ulong>(ref value)),
            _ => throw new NotSupportedException($"Unsupported enum underlying type: {typeof(TEnum).GetEnumUnderlyingType()}")
        };

        static long ConvertUInt64(ulong v)
        {
            if (v > long.MaxValue)
                throw new OverflowException($"Enum value exceeds Int64.MaxValue.");
            return (long)v;
        }
    }

    /// <summary>
    /// Converts a long value back to the enum type, throwing if it does not fit in the enum's underlying type.
    /// </summary>
    private static TEnum LongToEnum(long value)
    {
        try
        {
            return UnderlyingTypeCode switch
            {
                TypeCode.SByte => As(checked((sbyte)value)),
                TypeCode.Byte => As(checked((byte)value)),
                TypeCode.Int16 => As(checked((short)value)),
                TypeCode.UInt16 => As(checked((ushort)value)),
                TypeCode.Int32 => As(checked((int)value)),
                TypeCode.UInt32 => As(checked((uint)value)),
                TypeCode.Int64 => As(value),
                TypeCode.UInt64 => As(checked((ulong)value)),
                _ => throw new NotSupportedException($"Unsupported enum underlying type: {typeof(TEnum).GetEnumUnderlyingType()}")
            };
        }
        catch (OverflowException ex)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                $"Value is out of range for the underlying type of enum {typeof(TEnum).Name}. {ex.Message}");
        }

        static TEnum As<TUnderlying>(TUnderlying v) where TUnderlying : struct => Unsafe.As<TUnderlying, TEnum>(ref v);
    }
}

/// <summary>
/// Provides a base class for creating enumeration-like constructs
/// with strongly-typed values and display codes.
/// </summary>
public abstract class Vnum : IEquatable<Vnum>, IComparable<Vnum>, IComparable
{
    /// <summary>
    /// Gets the numeric value of the Vnum item.
    /// </summary>
    public long Value { get; }

    /// <summary>
    /// Gets the string code of the Vnum item.
    /// </summary>
    public string Code { get; }

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
    public static IEnumerable<TVnum> GetAll<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] TVnum>() where TVnum : Vnum =>
        Cache<TVnum>.Get().Items;

    /// <summary>
    /// Retrieves all Vnum instances of a given type <typeparamref name="TVnum"/> that satisfy the specified predicate.
    /// </summary>
    public static IEnumerable<TVnum> GetAll<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] TVnum>(Func<TVnum, bool> predicate) where TVnum : Vnum =>
        Cache<TVnum>.Get().Items.Where(predicate);

    /// <summary>
    /// Retrieves a Vnum instance by its numeric value.
    /// </summary>
    public static TVnum FromValue<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] TVnum>(long value) where TVnum : Vnum =>
        TryFromValue<TVnum>(value, out var vnum)
            ? vnum
            : throw NotFound<TVnum>(value, nameof(value));

    /// <summary>
    /// Attempts to retrieve a Vnum instance by its numeric value.
    /// </summary>
    public static bool TryFromValue<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] TVnum>(long value, out TVnum vnum) where TVnum : Vnum =>
        TryGet(Cache<TVnum>.Get().ByValue, value, out vnum);

    /// <summary>
    /// Retrieves a Vnum instance by its enum value.
    /// </summary>
    public static TVnum FromEnum<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] TVnum, TEnum>(TEnum value)
        where TVnum : Vnum<TEnum>
        where TEnum : struct, Enum
        => FromValue<TVnum>(Vnum<TEnum>.EnumToLong(value));

    /// <summary>
    /// Attempts to retrieve a Vnum instance by its enum value.
    /// </summary>
    public static bool TryFromEnum<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] TVnum, TEnum>(TEnum value, out TVnum vnum)
        where TVnum : Vnum<TEnum>
        where TEnum : struct, Enum
        => TryFromValue(Vnum<TEnum>.EnumToLong(value), out vnum);

    /// <summary>
    /// Retrieves a Vnum instance by its code.
    /// </summary>
    public static TVnum FromCode<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] TVnum>(string code, bool ignoreCase = false) where TVnum : Vnum
    {
        ArgumentNullException.ThrowIfNull(code);

        return TryFromCode<TVnum>(code, ignoreCase, out var vnum)
            ? vnum
            : throw NotFound<TVnum>(code, nameof(code));
    }

    /// <summary>
    /// Attempts to retrieve a Vnum instance by its code.
    /// </summary>
    public static bool TryFromCode<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] TVnum>(string code, bool ignoreCase, out TVnum vnum) where TVnum : Vnum
    {
        if (code is null)
        {
            vnum = null!;
            return false;
        }

        var lookup = Cache<TVnum>.Get();
        return TryGet(ignoreCase ? lookup.ByCodeIgnoreCase : lookup.ByCode, code, out vnum);
    }

    /// <summary>
    /// Attempts to retrieve a Vnum instance by its code (case sensitive).
    /// </summary>
    public static bool TryFromCode<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] TVnum>(string code, out TVnum vnum) where TVnum : Vnum =>
        TryFromCode(code, ignoreCase: false, out vnum);

    private static bool TryGet<TKey, TVnum>(Dictionary<TKey, TVnum> map, TKey key, out TVnum vnum)
        where TKey : notnull
        where TVnum : Vnum
    {
        if (map.TryGetValue(key, out var found))
        {
            vnum = found;
            return true;
        }

        vnum = null!;
        return false;
    }

    private static InvalidOperationException NotFound<TVnum>(object value, string description) =>
        new($"'{value}' is not a valid {description} in {typeof(TVnum).Name}");

    /// <summary>
    /// Per-type cache of discovered instances and lookup tables, built with reflection on first use.
    /// The PublicFields annotation tells the trimmer to keep TVnum's public static fields; every public
    /// entry point repeats it so the requirement reaches consumers' call sites.
    /// </summary>
    private static class Cache<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] TVnum> where TVnum : Vnum
    {
        private static Lookup<TVnum>? _lookup;

        public static Lookup<TVnum> Get() => Volatile.Read(ref _lookup) ?? Build();

        private static Lookup<TVnum> Build()
        {
            var values = typeof(TVnum)
                .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(f => typeof(TVnum).IsAssignableFrom(f.FieldType))
                .Select(f => (TVnum?)f.GetValue(null))
                .ToArray();

            var lookup = new Lookup<TVnum>(values.OfType<TVnum>().Distinct<TVnum>(ReferenceEqualityComparer.Instance));

            // A null field is either a genuine null (e.g. a "None" sentinel), which is simply skipped,
            // or a field whose initializer has not run yet because we were called from TVnum's own
            // static initializer (e.g. from one of its field initializers). Only the second case must
            // not be cached: return the partial result for this call so later calls see the full set.
            if (values.All(v => v is not null) || !IsStaticInitializerRunning())
            {
                // Concurrent builders produce equivalent lookups, so whichever write wins is fine.
                Volatile.Write(ref _lookup, lookup);
            }

            return lookup;
        }

        /// <summary>
        /// Checks whether TVnum's static initializer is on the current call stack. Only consulted when a
        /// null field was seen, so the normal path never walks the stack. When a frame cannot be
        /// resolved (possible under native AOT) the answer is "unknown" and we err on the side of not caching.
        /// </summary>
#if !NET9_0_OR_GREATER
        [UnconditionalSuppressMessage("Trimming", "IL2026",
            Justification = "Only the method name and declaring type are read; if metadata was trimmed GetMethod returns null and the table is simply not cached.")]
#endif
        private static bool IsStaticInitializerRunning()
        {
            foreach (var frame in new StackTrace(fNeedFileInfo: false).GetFrames())
            {
#if NET9_0_OR_GREATER
                // Trim-safe: exposes only the names, which is all we need.
                var method = DiagnosticMethodInfo.Create(frame);
                if (method is null)
                    return true;

                if (method.Name == ".cctor" && method.DeclaringTypeName == typeof(TVnum).FullName)
                    return true;
#else
                var method = frame.GetMethod();
                if (method is null)
                    return true;

                if (method.Name == ".cctor" && method.DeclaringType == typeof(TVnum))
                    return true;
#endif
            }

            return false;
        }
    }

    private sealed class Lookup<TVnum> where TVnum : Vnum
    {
        public ReadOnlyCollection<TVnum> Items { get; }
        public Dictionary<long, TVnum> ByValue { get; } = new();
        public Dictionary<string, TVnum> ByCode { get; } = new(StringComparer.Ordinal);
        public Dictionary<string, TVnum> ByCodeIgnoreCase { get; } = new(StringComparer.OrdinalIgnoreCase);

        public Lookup(IEnumerable<TVnum> items)
        {
            Items = items.ToList().AsReadOnly();

            // A duplicate value or code is an authoring mistake; fail on the first lookup rather than
            // silently answering with whichever instance was declared first.
            foreach (var item in Items)
            {
                if (!ByValue.TryAdd(item.Value, item))
                {
                    throw new InvalidOperationException(
                        $"Duplicate value {item.Value} in {typeof(TVnum).Name}: '{ByValue[item.Value].Code}' and '{item.Code}'");
                }

                if (!ByCode.TryAdd(item.Code, item))
                {
                    throw new InvalidOperationException(
                        $"Duplicate code '{item.Code}' in {typeof(TVnum).Name}");
                }

                if (!ByCodeIgnoreCase.TryAdd(item.Code, item))
                {
                    throw new InvalidOperationException(
                        $"Codes '{ByCodeIgnoreCase[item.Code].Code}' and '{item.Code}' in {typeof(TVnum).Name} differ only by case; " +
                        "codes must be unique ignoring case");
                }
            }
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
        other is not null &&
        (ReferenceEquals(this, other) ||
         (GetType() == other.GetType() && Value == other.Value));

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

    /// <summary>
    /// Compares this instance with another Vnum of the same runtime type by <see cref="Value"/>.
    /// </summary>
    /// <returns>
    /// A negative number, zero or a positive number when this instance's <see cref="Value"/> is less than, equal to
    /// or greater than that of <paramref name="other"/>. Every instance is greater than <see langword="null"/>.
    /// </returns>
    /// <exception cref="ArgumentException"><paramref name="other"/> has a different runtime type.</exception>
    public int CompareTo(Vnum? other)
    {
        if (other is null)
        {
            return 1;
        }

        if (ReferenceEquals(this, other))
        {
            return 0;
        }

        // Ordering is scoped to one runtime type, like equality.
        if (GetType() != other.GetType())
        {
            throw new ArgumentException(
                $"Cannot compare {GetType().Name} with {other.GetType().Name}: Vnums are only ordered within one type.",
                nameof(other));
        }

        return Value.CompareTo(other.Value);
    }

    /// <inheritdoc/>
    int IComparable.CompareTo(object? obj) => obj switch
    {
        null => 1,
        Vnum other => CompareTo(other),
        _ => throw new ArgumentException($"Object must be a Vnum, not {obj.GetType().Name}.", nameof(obj)),
    };
}
