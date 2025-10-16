using Skywithin.VnumEnumeration;
using Skywithin.VnumEnumeration.Serialization;
using System.Text.Json;

namespace VnumEnumeration.Tests;

public enum LongEnum : long
{
    Zero = 0L,
    Small = 100L,
    Medium = 1000000L,
    Large = 1000000000L,
    VeryLarge = 5000000000L,
    MaxValue = long.MaxValue
}

public sealed class TestLongVnum : Vnum<LongEnum>
{
    public TestLongVnum(LongEnum value, string code) : base(value, code) { }
    
    public static readonly TestLongVnum Zero = new(LongEnum.Zero, "zero");
    public static readonly TestLongVnum Small = new(LongEnum.Small, "small");
    public static readonly TestLongVnum Medium = new(LongEnum.Medium, "medium");
    public static readonly TestLongVnum Large = new(LongEnum.Large, "large");
    public static readonly TestLongVnum VeryLarge = new(LongEnum.VeryLarge, "verylarge");
    public static readonly TestLongVnum MaxValue = new(LongEnum.MaxValue, "maxvalue");
}

public class LongEnumVnumTests
{
    [Fact]
    public void FromEnum_WithLongEnum_ShouldWork()
    {
        var vnum = Vnum.FromEnum<TestLongVnum, LongEnum>(LongEnum.VeryLarge);
        Assert.Equal(LongEnum.VeryLarge, vnum.Id);
        Assert.Equal("verylarge", vnum.Code);
        Assert.Equal(5000000000L, vnum.Value);
    }

    [Fact]
    public void FromValue_WithLongValue_ShouldWork()
    {
        var vnum = Vnum.FromValue<TestLongVnum>(5000000000L);
        Assert.Equal(LongEnum.VeryLarge, vnum.Id);
        Assert.Equal("verylarge", vnum.Code);
        Assert.Equal(5000000000L, vnum.Value);
    }

    [Fact]
    public void TryFromEnum_WithLongEnum_ShouldWork()
    {
        var success = Vnum.TryFromEnum<TestLongVnum, LongEnum>(LongEnum.Large, out var vnum);
        Assert.True(success);
        Assert.Equal(LongEnum.Large, vnum.Id);
        Assert.Equal("large", vnum.Code);
        Assert.Equal(1000000000L, vnum.Value);
    }

    [Fact]
    public void TryFromValue_WithLongValue_ShouldWork()
    {
        var success = Vnum.TryFromValue<TestLongVnum>(1000000000L, out var vnum);
        Assert.True(success);
        Assert.Equal(LongEnum.Large, vnum.Id);
        Assert.Equal("large", vnum.Code);
        Assert.Equal(1000000000L, vnum.Value);
    }

    [Fact]
    public void FromValue_WithMaxLongValue_ShouldWork()
    {
        var vnum = Vnum.FromValue<TestLongVnum>(long.MaxValue);
        Assert.Equal(LongEnum.MaxValue, vnum.Id);
        Assert.Equal("maxvalue", vnum.Code);
        Assert.Equal(long.MaxValue, vnum.Value);
    }

    [Fact]
    public void FromValue_WithZero_ShouldWork()
    {
        var vnum = Vnum.FromValue<TestLongVnum>(0L);
        Assert.Equal(LongEnum.Zero, vnum.Id);
        Assert.Equal("zero", vnum.Code);
        Assert.Equal(0L, vnum.Value);
    }

    [Fact]
    public void FromValue_WithNegativeValue_ShouldThrow()
    {
        Assert.Throws<InvalidOperationException>(() => Vnum.FromValue<TestLongVnum>(-1L));
    }

    [Fact]
    public void FromValue_WithInvalidValue_ShouldThrow()
    {
        Assert.Throws<InvalidOperationException>(() => Vnum.FromValue<TestLongVnum>(999999L));
    }

    [Fact]
    public void TryFromValue_WithInvalidValue_ShouldReturnFalse()
    {
        var success = Vnum.TryFromValue<TestLongVnum>(999999L, out var vnum);
        Assert.False(success);
        Assert.Null(vnum);
    }

    [Fact]
    public void GetAll_ShouldReturnAllLongVnums()
    {
        var allVnums = Vnum.GetAll<TestLongVnum>();
        Assert.Equal(6, allVnums.Count());
        
        var values = allVnums.Select(v => v.Value).ToArray();
        Assert.Contains(0L, values);
        Assert.Contains(100L, values);
        Assert.Contains(1000000L, values);
        Assert.Contains(1000000000L, values);
        Assert.Contains(5000000000L, values);
        Assert.Contains(long.MaxValue, values);
    }

    [Fact]
    public void JsonSerialization_WithLongValue_ShouldWork()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new VnumJsonConverterFactory());
        
        var vnum = TestLongVnum.VeryLarge;
        var json = JsonSerializer.Serialize(vnum, options);
        Assert.Equal("\"verylarge\"", json);
    }

    [Fact]
    public void JsonDeserialization_WithLongValue_ShouldWork()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new VnumJsonConverterFactory());
        
        var json = "5000000000";
        var vnum = JsonSerializer.Deserialize<TestLongVnum>(json, options);
        
        Assert.Equal(LongEnum.VeryLarge, vnum.Id);
        Assert.Equal("verylarge", vnum.Code);
        Assert.Equal(5000000000L, vnum.Value);
    }

    [Fact]
    public void JsonDeserialization_WithMaxLongValue_ShouldWork()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new VnumJsonConverterFactory());
        
        var json = long.MaxValue.ToString();
        var vnum = JsonSerializer.Deserialize<TestLongVnum>(json, options);
        
        Assert.Equal(LongEnum.MaxValue, vnum.Id);
        Assert.Equal("maxvalue", vnum.Code);
        Assert.Equal(long.MaxValue, vnum.Value);
    }

    [Fact]
    public void JsonDeserialization_WithInvalidValue_ShouldThrow()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new VnumJsonConverterFactory());
        
        var json = "999999";
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<TestLongVnum>(json, options));
    }

    [Fact]
    public void JsonDeserialization_WithNegativeValue_ShouldThrow()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new VnumJsonConverterFactory());
        
        var json = "-1";
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<TestLongVnum>(json, options));
    }

    [Fact]
    public void ValueProperty_ShouldBeLong()
    {
        var vnum = TestLongVnum.VeryLarge;
        Assert.IsType<long>(vnum.Value);
        Assert.Equal(5000000000L, vnum.Value);
    }

    [Fact]
    public void IdProperty_ShouldReturnCorrectEnum()
    {
        var vnum = TestLongVnum.Large;
        Assert.Equal(LongEnum.Large, vnum.Id);
        Assert.IsType<LongEnum>(vnum.Id);
    }

    [Fact]
    public void CodeProperty_ShouldReturnCorrectCode()
    {
        var vnum = TestLongVnum.Medium;
        Assert.Equal("medium", vnum.Code);
    }

    [Fact]
    public void ToString_ShouldReturnCode()
    {
        var vnum = TestLongVnum.Small;
        Assert.Equal("small", vnum.ToString());
    }

    [Fact]
    public void Equals_WithSameValue_ShouldBeEqual()
    {
        var vnum1 = Vnum.FromValue<TestLongVnum>(100L);
        var vnum2 = Vnum.FromValue<TestLongVnum>(100L);
        Assert.Equal(vnum1, vnum2);
        Assert.True(vnum1.Equals(vnum2));
    }

    [Fact]
    public void Equals_WithDifferentValue_ShouldNotBeEqual()
    {
        var vnum1 = Vnum.FromValue<TestLongVnum>(100L);
        var vnum2 = Vnum.FromValue<TestLongVnum>(1000000L);
        Assert.NotEqual(vnum1, vnum2);
        Assert.False(vnum1.Equals(vnum2));
    }

    [Fact]
    public void GetHashCode_WithSameValue_ShouldBeEqual()
    {
        var vnum1 = Vnum.FromValue<TestLongVnum>(100L);
        var vnum2 = Vnum.FromValue<TestLongVnum>(100L);
        Assert.Equal(vnum1.GetHashCode(), vnum2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_WithDifferentValue_ShouldNotBeEqual()
    {
        var vnum1 = Vnum.FromValue<TestLongVnum>(100L);
        var vnum2 = Vnum.FromValue<TestLongVnum>(1000000L);
        Assert.NotEqual(vnum1.GetHashCode(), vnum2.GetHashCode());
    }

    [Fact]
    public void Comparison_WithLongValues_ShouldWork()
    {
        var small = Vnum.FromValue<TestLongVnum>(100L);
        var large = Vnum.FromValue<TestLongVnum>(1000000L);
        
        Assert.True(small.Value < large.Value);
        Assert.True(large.Value > small.Value);
        Assert.True(small.Value <= large.Value);
        Assert.True(large.Value >= small.Value);
    }

    [Fact]
    public void StaticInstances_ShouldBeAccessible()
    {
        Assert.Equal(0L, TestLongVnum.Zero.Value);
        Assert.Equal(100L, TestLongVnum.Small.Value);
        Assert.Equal(1000000L, TestLongVnum.Medium.Value);
        Assert.Equal(1000000000L, TestLongVnum.Large.Value);
        Assert.Equal(5000000000L, TestLongVnum.VeryLarge.Value);
        Assert.Equal(long.MaxValue, TestLongVnum.MaxValue.Value);
    }

    [Fact]
    public void EqualityOperator_SameInstance_ShouldBeEqual()
    {
        var vnum1 = TestLongVnum.Small;
        var vnum2 = TestLongVnum.Small;
        Assert.True(vnum1 == vnum2);
    }

    [Fact]
    public void EqualityOperator_SameValue_ShouldBeEqual()
    {
        var vnum1 = Vnum.FromValue<TestLongVnum>(100L);
        var vnum2 = Vnum.FromValue<TestLongVnum>(100L);
        Assert.True(vnum1 == vnum2);
    }

    [Fact]
    public void EqualityOperator_DifferentValues_ShouldNotBeEqual()
    {
        var vnum1 = Vnum.FromValue<TestLongVnum>(100L);
        var vnum2 = Vnum.FromValue<TestLongVnum>(1000000L);
        Assert.False(vnum1 == vnum2);
    }

    [Fact]
    public void EqualityOperator_NullComparison_ShouldWork()
    {
        TestLongVnum? vnum1 = null;
        TestLongVnum? vnum2 = null;
        TestLongVnum? vnum3 = TestLongVnum.Small;
        
        Assert.True(vnum1 == vnum2);
        Assert.False(vnum1 == vnum3);
        Assert.False(vnum3 == vnum1);
    }

    [Fact]
    public void EqualityOperator_OneNull_ShouldNotBeEqual()
    {
        TestLongVnum? vnum1 = null;
        var vnum2 = TestLongVnum.Small;
        
        Assert.False(vnum1 == vnum2);
        Assert.False(vnum2 == vnum1);
    }

    [Fact]
    public void InequalityOperator_ShouldBeOppositeOfEquality()
    {
        var vnum1 = Vnum.FromValue<TestLongVnum>(100L);
        var vnum2 = Vnum.FromValue<TestLongVnum>(1000000L);
        var vnum3 = Vnum.FromValue<TestLongVnum>(100L);
        
        Assert.True(vnum1 != vnum2);
        Assert.False(vnum1 != vnum3);
    }

    [Fact]
    public void InequalityOperator_NullComparison_ShouldWork()
    {
        TestLongVnum? vnum1 = null;
        TestLongVnum? vnum2 = null;
        TestLongVnum? vnum3 = TestLongVnum.Small;
        
        Assert.False(vnum1 != vnum2);
        Assert.True(vnum1 != vnum3);
        Assert.True(vnum3 != vnum1);
    }

    [Fact]
    public void EqualityOperator_ConsistencyWithEquals_ShouldMatch()
    {
        var vnum1 = Vnum.FromValue<TestLongVnum>(100L);
        var vnum2 = Vnum.FromValue<TestLongVnum>(100L);
        var vnum3 = Vnum.FromValue<TestLongVnum>(1000000L);
        
        // Equality operator should match Equals method
        Assert.Equal(vnum1 == vnum2, vnum1.Equals(vnum2));
        Assert.Equal(vnum1 == vnum3, vnum1.Equals(vnum3));
    }

    [Fact]
    public void EqualityOperator_StaticInstances_ShouldBeEqual()
    {
        var vnum1 = TestLongVnum.Small;
        var vnum2 = TestLongVnum.Small;
        
        Assert.True(vnum1 == vnum2);
        Assert.False(vnum1 != vnum2);
    }

    [Fact]
    public void EqualityOperator_DifferentStaticInstances_ShouldNotBeEqual()
    {
        var vnum1 = TestLongVnum.Small;
        var vnum2 = TestLongVnum.Medium;
        
        Assert.False(vnum1 == vnum2);
        Assert.True(vnum1 != vnum2);
    }
}
