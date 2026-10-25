using Skywithin.VnumEnumeration.Tests.Data;

namespace Skywithin.VnumEnumeration.Tests;

public class VnumDiscoveryTests
{
    [Fact]
    public void GetAll_Should_Not_Duplicate_Aliased_Instances()
    {
        // Act
        var actual = Vnum.GetAll<AliasedVnum>().ToList();

        // Assert
        Assert.Equal([AliasedVnum.One, AliasedVnum.Two], actual);
    }

    [Fact]
    public void Lookup_During_Static_Initialization_Should_Not_Corrupt_Cache()
    {
        // Act
        var defaultValue = ReentrantVnum.Default;
        var two = Vnum.FromValue<ReentrantVnum>(2);
        var all = Vnum.GetAll<ReentrantVnum>().ToList();

        // Assert
        Assert.Same(ReentrantVnum.One, defaultValue);
        Assert.Same(ReentrantVnum.Two, two);
        Assert.Equal([ReentrantVnum.One, ReentrantVnum.Two], all);
    }

    [Fact]
    public void Null_Static_Field_Should_Be_Skipped_And_Not_Prevent_Caching()
    {
        // Act
        var first = Vnum.GetAll<NullFieldVnum>();
        var second = Vnum.GetAll<NullFieldVnum>();

        // Assert
        Assert.Equal([NullFieldVnum.One, NullFieldVnum.Two], first);
        Assert.Same(first, second); // the cached table is returned, not a rebuilt one
        Assert.True(Vnum.TryFromValue<NullFieldVnum>(2, out var two));
        Assert.Same(NullFieldVnum.Two, two);
    }

    [Fact]
    public void Duplicate_Value_Should_Throw_On_First_Lookup()
    {
        // Act
        var ex = Assert.Throws<InvalidOperationException>(() => Vnum.GetAll<DuplicateValueVnum>());

        // Assert
        Assert.Equal("Duplicate value 1 in DuplicateValueVnum: 'a' and 'b'", ex.Message);
    }

    [Fact]
    public void Duplicate_Code_Should_Throw_On_First_Lookup()
    {
        // Act
        var ex = Assert.Throws<InvalidOperationException>(() => Vnum.TryFromValue<DuplicateCodeVnum>(1, out _));

        // Assert
        Assert.Equal("Duplicate code 'same' in DuplicateCodeVnum", ex.Message);
    }

    [Fact]
    public void Codes_Differing_Only_By_Case_Should_Throw_On_First_Lookup()
    {
        // Act
        var ex = Assert.Throws<InvalidOperationException>(() => Vnum.FromCode<CaseClashCodeVnum>("abc"));

        // Assert
        Assert.Equal(
            "Codes 'abc' and 'ABC' in CaseClashCodeVnum differ only by case; codes must be unique ignoring case",
            ex.Message);
    }

    [Fact]
    public void Broken_Type_Should_Throw_On_Every_Lookup_Not_Only_The_First()
    {
        // Act & Assert: a failed build is not cached, so the second call fails the same way
        Assert.Throws<InvalidOperationException>(() => Vnum.GetAll<DuplicateValueVnum>());
        Assert.Throws<InvalidOperationException>(() => Vnum.FromValue<DuplicateValueVnum>(1));
    }

    [Fact]
    public void GetAll_Result_Should_Be_Read_Only()
    {
        // Act
        var all = (IList<TestVnum1>)Vnum.GetAll<TestVnum1>();

        // Assert
        Assert.Throws<NotSupportedException>(() => all[0] = TestVnum1.OptionTwo);
    }

    [Theory]
    [InlineData(256)]
    [InlineData(-1)]
    public void Constructor_With_Value_Out_Of_Enum_Range_Should_Throw(long value)
    {
        // Act
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => ByteVnum.Create(value));

        // Assert: the range check runs before the declared-member check
        Assert.Equal("value", ex.ParamName);
        Assert.StartsWith("Value is out of range for the underlying type of enum ByteId.", ex.Message);
    }

    [Fact]
    public void Constructor_With_Undefined_Enum_Member_Should_Throw()
    {
        // Act: 7 fits in a byte but is not a member of ByteId
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => ByteVnum.Create(7));

        // Assert
        Assert.Equal("value", ex.ParamName);
        Assert.Equal(7L, ex.ActualValue);
        Assert.StartsWith("Value 7 is not a defined member of enum ByteId.", ex.Message);
    }

    [Fact]
    public void Constructor_With_Combined_Flags_Value_Should_Not_Throw()
    {
        // Act: Read | Write is not a declared member, but [Flags] enums skip the check
        var vnum = FlagsVnum.Create(3);

        // Assert
        Assert.Equal(PermissionId.Read | PermissionId.Write, vnum.Id);
    }

    [Fact]
    public void Constructor_With_Long_Value_Should_Set_Id()
    {
        Assert.Equal(ByteId.One, ByteVnum.One.Id);
    }
}
