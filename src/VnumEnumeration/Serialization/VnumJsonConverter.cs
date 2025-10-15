using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace VnumEnumeration.Serialization;

/// <summary>
/// JSON converter for Vnum types that serializes to Code string and deserializes from Code or Value.
/// Deserialization is case-sensitive by default.
/// </summary>
public class VnumJsonConverter<T> : JsonConverter<T> where T : Vnum, new()
{
    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
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
            
            if (Vnum.TryFromCode<T>(code, ignoreCase: false, out var vnum))
            {
                return vnum;
            }
            
            throw new JsonException($"'{code}' is not a valid code for {typeof(T).Name}. Valid codes: {string.Join(", ", Vnum.GetAll<T>().Select(v => v.Code).Take(20))}"); //Take 20 codes max 
        }
        
        // Deserialize from numeric value (backward compatibility)
        if (reader.TokenType == JsonTokenType.Number)
        {
            var value = reader.GetInt32();
            
            if (Vnum.TryFromValue<T>(value, out var vnum))
            {
                return vnum;
            }
            
            throw new JsonException($"'{value}' is not a valid numeric value for {typeof(T).Name}");
        }
        
        throw new JsonException($"Unexpected token type {reader.TokenType} when deserializing {typeof(T).Name}");
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }
        
        // Serialize as string code
        writer.WriteStringValue(value.Code);
    }
}

/// <summary>
/// Factory for creating VnumJsonConverter instances for any Vnum type.
/// Automatically handles all Vnum types without needing individual registration.
/// </summary>
public class VnumJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeof(Vnum).IsAssignableFrom(typeToConvert) && typeToConvert != typeof(Vnum);
    }

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var converterType = typeof(VnumJsonConverter<>).MakeGenericType(typeToConvert);
        return (JsonConverter?)Activator.CreateInstance(converterType);
    }
}
