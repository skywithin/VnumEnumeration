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
    public void Lookup_During_Static_Initialization_Should_Throw()
    {
        // Act: the lookup runs inside the static initializer, so the failure surfaces as a type-load error
        var ex = Assert.Throws<TypeInitializationException>(() => ReentrantVnum.One);

        // Assert
        var inner = Assert.IsType<InvalidOperationException>(ex.InnerException);
        Assert.StartsWith("Public static field 'ReentrantVnum.", inner.Message);
    }

    [Fact]
    public void Null_Static_Field_Should_Throw_On_Every_Lookup()
    {
        // Act
        var ex = Assert.Throws<InvalidOperationException>(() => Vnum.GetAll<NullFieldVnum>());

        // Assert
        Assert.Equal(
            "Public static field 'NullFieldVnum.None' is null. " +
            "Every public static NullFieldVnum field must hold an instance when the first lookup runs; " +
            "a null field, or a lookup made from inside the type's own static initializer, is not supported",
            ex.Message);

        // A failed build is not cached, so the next lookup fails the same way
        Assert.Throws<InvalidOperationException>(() => Vnum.TryFromValue<NullFieldVnum>(1, out _));
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
