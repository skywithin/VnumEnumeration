#nullable disable
using VnumEnumeration.Tests.Data;

namespace VnumEnumeration.Tests;

public class BaseVnumTests
{
    [Fact]
    public void Constructor_Should_Initialize_Properties_Correctly()
    {
        Assert.Equal(1, TestVnum1.OptionOne.Value);
        Assert.Equal("OptionOne", TestVnum1.OptionOne.Code);

        Assert.Equal(2, TestVnum1.OptionTwo.Value);
        Assert.Equal("OptionTwo", TestVnum1.OptionTwo.Code);
    }

    [Fact]
    public void ToString_Should_Return_Expected_Value()
    {
        Assert.Equal("OptionOne", TestVnum1.OptionOne.ToString());
    }

    [Fact]
    public void Equals_Same_Instance_Should_Return_True()
    {
        // Arrange
        var instance = TestVnum1.OptionOne;

        // Act
        Assert.True(instance.Equals(instance));
    }

    [Fact]
    public void Equals_Different_Instances_With_Same_Value_And_Code_Should_Return_True()
    {
        // Arrange
        var instance = TestVnum1.Stub(1, "OptionOne");

        // Act
        Assert.True(instance.Equals(TestVnum1.OptionOne));
    }

    [Fact]
    public void Equals_Different_Instances_With_Same_Value_Different_Code_Should_Return_True()
    {
        // Arrange
        var instance = TestVnum1.Stub(1, "BBBB");

        // Act
        Assert.True(instance.Equals(TestVnum1.OptionOne));
    }

    [Fact]
    public void Equals_Different_Values_Should_Return_False()
    {
        Assert.False(TestVnum1.OptionOne.Equals(TestVnum1.OptionTwo));
    }

    [Fact]
    public void Equals_Different_Instances_With_Same_Code_Different_Value_Should_Return_False()
    {
        // Arrange
        var instance = TestVnum1.Stub(9999, "OptionOne");

        // Act
        Assert.False(instance.Equals(TestVnum1.OptionOne));
    }

    [Fact]
    public void Equality_Operator_Should_Return_True_When_Values_Are_Equal()
    {
        // Act
        var obj1 = TestVnum1.OptionOne;
        var obj2 = TestVnum1.OptionOne;


        var actual = obj1 == obj2;
        // Assert
        Assert.True(actual);
    }

    [Fact]
    public void Equality_Operator_Should_Return_False_When_Values_Are_Not_Equal()
    {
        // Act
        var actual = TestVnum1.OptionOne == TestVnum1.OptionTwo;
        // Assert
        Assert.False(actual);
    }

    [Fact]
    public void Inequality_Operator_Should_Return_True_When_Values_Are_Not_Equal()
    {
        // Act
        var actual = TestVnum1.OptionOne != TestVnum1.OptionTwo;
        // Assert
        Assert.True(actual);
    }

    [Fact]
    public void Inequality_Operator_Should_Return_False_When_Values_Are_Equal()
    {
        // Arrange
        var obj1 = TestVnum1.OptionOne;
        var obj2 = TestVnum1.OptionOne;

        // Act
        var actual = obj1 != obj2;
        // Assert
        Assert.False(actual);
    }

    [Fact]
    public void GetHashCode_Same_Value_Should_Be_Equal()
    {
        // Arrange
        var instance = TestVnum1.Stub(1, "OptionOne");

        // Act
        Assert.Equal(instance.GetHashCode(), TestVnum1.OptionOne.GetHashCode());
    }

    [Fact]
    public void FromValue_Given_Valid_Value_Should_Return_Correct_Vnum()
    {
        // Act
        var actual = Vnum.FromValue<TestVnum1>(1);

        // Assert 
        Assert.Equal(TestVnum1.OptionOne, actual);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(9999)]
    public void FromValue_Given_Invalid_Value_Should_Throw_Exception(int value)
    {
        // Act
        var ex = Assert.Throws<InvalidOperationException>(() => Vnum.FromValue<TestVnum1>(value));

        // Assert
        Assert.Equal($"'{value}' is not a valid value in TestVnum1", ex.Message);
    }

    [Fact]
    public void TryFromValue_Given_Valid_Value_Should_Return_True_With_Expected_Vnum()
    {
        // Act
        var actual = Vnum.TryFromValue<TestVnum1>(2, out var vnum);

        // Assert
        Assert.True(actual);
        Assert.Equal(TestVnum1.OptionTwo, vnum);
    }

    [Fact]
    public void TryFromValue_Given_Invalid_Value_Should_Return_False_With_Null_Vnum()
    {
        // Act
        var actual = Vnum.TryFromValue<TestVnum1>(3, out var vnum);

        // Assert
        Assert.False(actual);
        Assert.Null(vnum);
    }

    [Fact]
    public void FromEnum_GivenValidEnumValue_ShouldReturnExpectedResult()
    {
        // Act
        var actual = Vnum.FromEnum<SampleVnum, SampleId>(SampleId.One);

        // Assert 
        Assert.Equal(SampleVnum.One, actual);
    }

    [Fact]
    public void FromEnum_GivenInValidEnumValue_ShouldReturnExpectedResult()
    {
        // Act
        var value = SampleId.Two;

        // Assert
        var ex = Assert.Throws<InvalidOperationException>(() => Vnum.FromEnum<BadSampleVnum, SampleId>(value));
        Assert.Equal($"'{(int)value}' is not a valid value in {nameof(BadSampleVnum)}", ex.Message);
    }

    [Fact]
    public void TryFromEnum_GivenValidEnumValue_ShouldReturnTrue()
    {
        // Act
        var actual = Vnum.TryFromEnum<SampleVnum, SampleId>(SampleId.One, out var vnum);

        // Assert
        Assert.True(actual);
        Assert.Equal(SampleVnum.One, vnum);
    }

    [Fact]
    public void TryFromEnum_GivenInvalidEnumValue_ShouldReturnFalse()
    {
        // Act
        var actual = Vnum.TryFromEnum<BadSampleVnum, SampleId>(SampleId.Two, out var vnum);

        // Assert
        Assert.False(actual);
        Assert.Null(vnum);
    }

    [Fact]
    public void FromCode_Given_Valid_Code_Should_Return_Correct_Vnum()
    {
        // Act
        var actual = Vnum.FromCode<TestVnum1>("OptionOne");

        // Assert
        Assert.Equal(TestVnum1.OptionOne, actual);
    }

    [Theory]
    [InlineData("")]
    [InlineData("optionone")]
    [InlineData("OPTIONONE")]
    [InlineData("INVALID_CODE")]
    public void FromCode_Given_Invalid_Code_Should_Throw_Exception(string code)
    {
        // Act
        var ex = Assert.Throws<InvalidOperationException>(() => Vnum.FromCode<TestVnum1>(code));

        // Assert
        Assert.Equal($"'{code}' is not a valid code in TestVnum1", ex.Message);
    }

    [Theory]
    [InlineData("optionone")]
    [InlineData("OPTIONONE")]
    public void FromCode_With_IgnoreCase_Should_Return_Correct_Vnum(string code)
    {
        // Act
        var actual = Vnum.FromCode<TestVnum1>(code, ignoreCase: true);

        // Assert
        Assert.Equal(TestVnum1.OptionOne, actual);
    }

    [Fact]
    public void TryFromCode_Given_Valid_Code_Should_Return_True_With_Expected_Vnum()
    {
        // Act
        var actual = Vnum.TryFromCode<TestVnum1>("OptionOne", out var vnum);

        // Assert
        Assert.True(actual);
        Assert.Equal(TestVnum1.OptionOne, vnum);
    }

    [Theory]
    [InlineData("")]
    [InlineData("optionone")]
    [InlineData("OPTIONONE")]
    [InlineData("INVALID_CODE")]
    public void TryFromCode_Given_Invalid_Code_Should_Return_False_With_Null_Vnum(string code)
    {
        // Act
        var actual = Vnum.TryFromCode<TestVnum1>(code, out var vnum);

        // Assert
        Assert.False(actual);
        Assert.Null(vnum);
    }

    [Fact]
    public void TryFromCode_Given_Null_Should_Return_False_With_Null_Vnum()
    {
        // Act
        var actual = Vnum.TryFromCode<TestVnum1>(code: null, out var vnum);

        // Assert
        Assert.False(actual);
        Assert.Null(vnum);
    }

    [Theory]
    [InlineData("optionone")]
    [InlineData("OPTIONONE")]
    public void TryFromCode_With_IgnoreCase_Should_Return_Correct_Vnum(string code)
    {
        // Act
        var result = Vnum.TryFromCode<TestVnum1>(code, ignoreCase: true, out var vnum);

        // Assert
        Assert.True(result);
        Assert.Equal(TestVnum1.OptionOne, vnum);
    }

    [Fact]
    public void GetAll_With_No_Predicate_Should_Return_All_Vnums()
    {
        // Act
        var actual = Vnum.GetAll<TestVnum1>().ToList();

        // Assert
        var expected = new List<TestVnum1> { TestVnum1.OptionOne, TestVnum1.OptionTwo };
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GetAll_With_Predicate_Filter_By_Value_Should_Return_Expected_Result()
    {
        // Act
        var actual = Vnum.GetAll<TestVnum1>(x => x.Value == 1).ToList();

        // Assert
        Assert.Single(actual);
        Assert.Equal(TestVnum1.OptionOne, actual[0]);
    }

    [Fact]
    public void GetAll_With_Predicate_Filter_By_Code_Should_Return_Expected_Result()
    {
        // Act
        var actual = Vnum.GetAll<TestVnum1>(x => x.Code == "OptionTwo").ToList();

        // Assert
        Assert.Single(actual);
        Assert.Equal(TestVnum1.OptionTwo, actual[0]);
    }

    [Fact]
    public void GetAll_With_No_Matching_Predicate_Should_Return_Empty_List()
    {
        // Act
        var actual = Vnum.GetAll<TestVnum1>(x => x.Value == 9999).ToList();

        // Assert
        Assert.Empty(actual);
    }

    [Fact]
    public void GetAll_Should_Exclude_Private_Vnums()
    {
        // Act
        var actual = Vnum.GetAll<PrivateVnum>();

        // Assert
        Assert.Empty(actual);
    }

}
