using System.Text.Json;
using Skywithin.VnumEnumeration.Serialization;
using Skywithin.VnumEnumeration.Tests.Data;

namespace Skywithin.VnumEnumeration.Tests;

public class VnumJsonConverterTests
{
    private static readonly JsonSerializerOptions _options = new()
    {
        Converters = { new VnumJsonConverterFactory() }
    };
    [Fact]
    public void Serialize_Vnum_Should_Return_Code_String()
    {
        // Arrange
        var vnum = TestVnum1.OptionOne;
        var options = _options;

        // Act
        var json = JsonSerializer.Serialize(vnum, options);

        // Assert
        Assert.Equal("\"OptionOne\"", json);
    }

    [Fact]
    public void Serialize_VnumWithEnum_Should_Return_Code_String()
    {
        // Arrange
        var vnum = SampleVnum.One;
        var options = _options;

        // Act
        var json = JsonSerializer.Serialize(vnum, options);

        // Assert
        Assert.Equal("\"one\"", json);
    }

    [Fact]
    public void Serialize_Null_Vnum_Should_Return_Null()
    {
        // Arrange
        TestVnum1? vnum = null;
        var options = _options;

        // Act
        var json = JsonSerializer.Serialize(vnum, options);

        // Assert
        Assert.Equal("null", json);
    }

    [Fact]
    public void Deserialize_From_Code_String_Should_Return_Correct_Vnum()
    {
        // Arrange
        var json = "\"OptionOne\"";
        var options = _options;

        // Act
        var result = JsonSerializer.Deserialize<TestVnum1>(json, options);

        // Assert
        Assert.Equal(TestVnum1.OptionOne, result);
    }

    [Fact]
    public void Deserialize_From_Code_String_WithEnum_Should_Return_Correct_Vnum()
    {
        // Arrange
        var json = "\"one\"";
        var options = _options;

        // Act
        var result = JsonSerializer.Deserialize<SampleVnum>(json, options);

        // Assert
        Assert.Equal(SampleVnum.One, result);
    }

    [Fact]
    public void Deserialize_From_Numeric_Value_Should_Return_Correct_Vnum()
    {
        // Arrange
        var json = "1";
        var options = _options;

        // Act
        var result = JsonSerializer.Deserialize<TestVnum1>(json, options);

        // Assert
        Assert.Equal(TestVnum1.OptionOne, result);
    }

    [Fact]
    public void Deserialize_From_Numeric_Value_WithEnum_Should_Return_Correct_Vnum()
    {
        // Arrange
        var json = "1";
        var options = _options;

        // Act
        var result = JsonSerializer.Deserialize<SampleVnum>(json, options);

        // Assert
        Assert.Equal(SampleVnum.One, result);
    }

    [Fact]
    public void Deserialize_From_Null_Should_Return_Null()
    {
        // Arrange
        var json = "null";
        var options = _options;

        // Act
        var result = JsonSerializer.Deserialize<TestVnum1>(json, options);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Deserialize_From_Empty_String_Should_Return_Null()
    {
        // Arrange
        var json = "\"\"";
        var options = _options;

        // Act
        var result = JsonSerializer.Deserialize<TestVnum1>(json, options);

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData("\"InvalidCode\"")]
    [InlineData("\"optionone\"")] // Case sensitive
    [InlineData("\"OPTIONONE\"")] // Case sensitive
    public void Deserialize_From_Invalid_Code_Should_Throw_JsonException(string json)
    {
        // Arrange
        var options = _options;

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<TestVnum1>(json, options));
        Assert.Contains("is not a valid code for TestVnum1", ex.Message);
        Assert.Contains("Valid codes:", ex.Message);
    }

    [Fact]
    public void Deserialize_From_Invalid_Numeric_Value_Should_Throw_JsonException()
    {
        // Arrange
        var json = "9999";
        var options = _options;

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<TestVnum1>(json, options));
        Assert.Contains("is not a valid numeric value for TestVnum1", ex.Message);
    }

    [Fact]
    public void Deserialize_From_Invalid_Token_Type_Should_Throw_JsonException()
    {
        // Arrange
        var json = "true";
        var options = _options;

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<TestVnum1>(json, options));
        Assert.Contains("Unexpected token type", ex.Message);
    }

    [Fact]
    public void Serialize_Dictionary_With_Vnum_Keys_Should_Write_Codes()
    {
        // Arrange
        var dictionary = new Dictionary<TestVnum1, int>
        {
            [TestVnum1.OptionOne] = 1,
            [TestVnum1.OptionTwo] = 2,
        };

        // Act
        var json = JsonSerializer.Serialize(dictionary, _options);

        // Assert
        Assert.Equal("{\"OptionOne\":1,\"OptionTwo\":2}", json);
    }

    [Fact]
    public void Deserialize_Dictionary_With_Vnum_Keys_Should_Resolve_Codes()
    {
        // Arrange
        var json = "{\"OptionOne\":1,\"OptionTwo\":2}";

        // Act
        var result = JsonSerializer.Deserialize<Dictionary<TestVnum1, int>>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal(1, result[TestVnum1.OptionOne]);
        Assert.Equal(2, result[TestVnum1.OptionTwo]);
    }

    [Theory]
    [InlineData("{\"Unknown\":1}")]
    [InlineData("{\"optionone\":1}")] // Case sensitive
    [InlineData("{\"1\":1}")]         // Numeric values are not accepted as keys
    public void Deserialize_Dictionary_With_Unknown_Vnum_Key_Should_Throw_JsonException(string json)
    {
        // Act & Assert
        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Dictionary<TestVnum1, int>>(json, _options));
        Assert.Contains("is not a valid code for TestVnum1", ex.Message);
        Assert.Contains("Valid codes:", ex.Message);
    }

    [Fact]
    public void Complex_Object_With_Vnum_Keyed_Dictionary_Should_Round_Trip()
    {
        // Arrange
        var obj = new TestObjectWithDictionary
        {
            Name = "Test",
            Counts = { [TestVnum1.OptionTwo] = 5 },
        };

        // Act
        var json = JsonSerializer.Serialize(obj, _options);
        var result = JsonSerializer.Deserialize<TestObjectWithDictionary>(json, _options);

        // Assert
        Assert.Equal("{\"Name\":\"Test\",\"Counts\":{\"OptionTwo\":5}}", json);
        Assert.NotNull(result);
        var pair = Assert.Single(result.Counts);
        Assert.Equal(TestVnum1.OptionTwo, pair.Key);
        Assert.Equal(5, pair.Value);
    }

    [Fact]
    public void VnumJsonConverterFactory_Should_Handle_Vnum_Types()
    {
        // Arrange
        var factory = new VnumJsonConverterFactory();

        // Act & Assert
        Assert.True(factory.CanConvert(typeof(TestVnum1)));
        Assert.True(factory.CanConvert(typeof(SampleVnum)));
        Assert.False(factory.CanConvert(typeof(Vnum))); // Base type should not be handled
        Assert.False(factory.CanConvert(typeof(string)));
    }

    [Fact]
    public void VnumJsonConverterFactory_Should_Create_Converter()
    {
        // Arrange
        var factory = new VnumJsonConverterFactory();
        var options = _options;

        // Act
        var converter = factory.CreateConverter(typeof(TestVnum1), options);

        // Assert
        Assert.NotNull(converter);
        Assert.IsType<VnumJsonConverter<TestVnum1>>(converter);
    }

    [Fact]
    public void Serialize_Complex_Object_With_Vnum_Property_Should_Work()
    {
        // Arrange
        var obj = new { Name = "Test", Status = TestVnum1.OptionOne };
        var options = _options;

        // Act
        var json = JsonSerializer.Serialize(obj, options);

        // Assert
        Assert.Contains("\"Status\":\"OptionOne\"", json);
    }

    [Fact]
    public void Deserialize_Complex_Object_With_Vnum_Property_Should_Work()
    {
        // Arrange
        var json = "{\"Name\":\"Test\",\"Status\":\"OptionOne\"}";
        var options = _options;

        // Act
        var result = JsonSerializer.Deserialize<TestObject>(json, options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test", result.Name);
        Assert.Equal(TestVnum1.OptionOne, result.Status);
    }

    [Fact]
    public void Serialize_Array_Of_Vnums_Should_Work()
    {
        // Arrange
        var vnums = new[] { TestVnum1.OptionOne, TestVnum1.OptionTwo };
        var options = _options;

        // Act
        var json = JsonSerializer.Serialize(vnums, options);

        // Assert
        Assert.Equal("[\"OptionOne\",\"OptionTwo\"]", json);
    }

    [Fact]
    public void Deserialize_Array_Of_Vnums_Should_Work()
    {
        // Arrange
        var json = "[\"OptionOne\",\"OptionTwo\"]";
        var options = _options;

        // Act
        var result = JsonSerializer.Deserialize<TestVnum1[]>(json, options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Length);
        Assert.Equal(TestVnum1.OptionOne, result[0]);
        Assert.Equal(TestVnum1.OptionTwo, result[1]);
    }

    [Fact]
    public void Deserialize_Array_With_Mixed_String_And_Numeric_Values_Should_Work()
    {
        // Arrange
        var json = "[\"OptionOne\",1]";
        var options = _options;

        // Act
        var result = JsonSerializer.Deserialize<TestVnum1[]>(json, options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Length);
        Assert.Equal(TestVnum1.OptionOne, result[0]);
        Assert.Equal(TestVnum1.OptionOne, result[1]);
    }

    [Fact]
    public void Serialize_With_Custom_JsonSerializerOptions_Should_Work()
    {
        // Arrange
        var vnum = TestVnum1.OptionOne;
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        options.Converters.Add(new VnumJsonConverterFactory());

        // Act
        var json = JsonSerializer.Serialize(vnum, options);

        // Assert
        Assert.Equal("\"OptionOne\"", json); // Vnum serialization should not be affected by naming policy
    }

    [Fact]
    public void Deserialize_With_Custom_JsonSerializerOptions_Should_Work()
    {
        // Arrange
        var json = "\"OptionOne\"";
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        options.Converters.Add(new VnumJsonConverterFactory());

        // Act
        var result = JsonSerializer.Deserialize<TestVnum1>(json, options);

        // Assert
        Assert.Equal(TestVnum1.OptionOne, result);
    }

    private class TestObject
    {
        public string Name { get; set; } = null!;
        public TestVnum1 Status { get; set; } = null!;
    }

    private class TestObjectWithDictionary
    {
        public string Name { get; set; } = null!;
        public Dictionary<TestVnum1, int> Counts { get; set; } = new();
    }
}
