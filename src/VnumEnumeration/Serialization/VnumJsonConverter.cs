using System.Text.Json;
using System.Text.Json.Serialization;

namespace Skywithin.VnumEnumeration.Serialization;

/// <summary>
/// JSON converter for Vnum types that serializes to Code string and deserializes from Code or Value.
/// Deserialization is case-sensitive by default.
/// </summary>
public class VnumJsonConverter<TVnum> : JsonConverter<TVnum> where TVnum : Vnum
{
    /// <summary>
    /// Reads and converts a JSON value to an instance of the specified <see cref="TVnum"/> type.
    /// </summary>
    /// <remarks>This method supports deserialization of <see cref="TVnum"/> instances from either a string
    /// code or a numeric value. String codes are case-sensitive and must match a valid <see cref="TVnum"/> instance.
    /// Numeric values are supported for backward compatibility and must correspond to a valid <see cref="TVnum"/>
    /// instance.</remarks>
    public override TVnum? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Handle null
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }
        
        // Deserialize from string code (case-sensitive)
        if (reader.TokenType == JsonTokenType.String)
        {
            var code = reader.GetString();
            if (string.IsNullOrEmpty(code))
            {
                return null;
            }
            
            if (Vnum.TryFromCode<TVnum>(code, ignoreCase: false, out var vnum))
            {
                return vnum;
            }
            
            throw new JsonException(
                $"'{code}' is not a valid code for {typeof(TVnum).Name}. " +
                $"Valid codes: {string.Join(", ", Vnum.GetAll<TVnum>().Select(v => v.Code).Take(20))}"); //Take 20 codes max (sanity check)
        }
        
        // Deserialize from numeric value (backward compatibility)
        if (reader.TokenType == JsonTokenType.Number)
        {
            var value = reader.GetInt64();
            
            if (Vnum.TryFromValue<TVnum>(value, out var vnum))
            {
                return vnum;
            }
            
            throw new JsonException($"'{value}' is not a valid numeric value for {typeof(TVnum).Name}");
        }
        
        throw new JsonException($"Unexpected token type {reader.TokenType} when deserializing {typeof(TVnum).Name}");
    }

    /// <summary>
    /// Writes the specified <see cref="TVnum"/> code as a JSON string or null.
    /// </summary>
    public override void Write(Utf8JsonWriter writer, TVnum vnum, JsonSerializerOptions options)
    {
        if (vnum == null)
        {
            writer.WriteNullValue();
            return;
        }
        
        // Serialize as string code
        writer.WriteStringValue(vnum.Code);
    }
}

/// <summary>
/// Factory for creating VnumJsonConverter instances for any Vnum type.
/// Automatically handles all Vnum types without needing individual registration.
/// </summary>
public class VnumJsonConverterFactory : JsonConverterFactory
{
    /// <summary>
    /// Determines whether the specified type can be converted by this converter.
    /// </summary>
    /// <returns><see langword="true"/> if the specified type is assignable from <see cref="Vnum"/>  and is not exactly the <see
    /// cref="Vnum"/> type; otherwise, <see langword="false"/>.</returns>
    public override bool CanConvert(Type typeToConvert)
    {
        return typeof(Vnum).IsAssignableFrom(typeToConvert) && typeToConvert != typeof(Vnum);
    }

    /// <summary>
    /// Creates a custom <see cref="JsonConverter"/> for the specified type.
    /// </summary>
    /// <remarks>This method dynamically creates a generic converter of type <c>VnumJsonConverter&lt;T&gt;</c>
    /// for the specified type. Ensure that the type provided is compatible with the expected generic  constraints of
    /// the converter.</remarks>
    /// <returns>A <see cref="JsonConverter"/> instance capable of converting the specified type,  or <see langword="null"/> if
    /// the converter cannot be created.</returns>
    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var converterType = typeof(VnumJsonConverter<>).MakeGenericType(typeToConvert);
        return (JsonConverter?)Activator.CreateInstance(converterType);
    }
}
