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

        // Assert
        Assert.Equal("value", ex.ParamName);
    }

    [Fact]
    public void Constructor_With_Long_Value_Should_Set_Id()
    {
        Assert.Equal(ByteId.One, ByteVnum.One.Id);
    }
}
