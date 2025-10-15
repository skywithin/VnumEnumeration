using System;
using VnumEnumeration.Tests.Data;
using VnumEnumeration.Tests.Helpers;

namespace VnumEnumeration.Tests;

public class BadSampleVnumTests : VnumTestingHelper<BadSampleVnum, SampleId>
{
    [Fact]
    public void Run_BadSampleVnum_Successful_Tests()
    {
        Vnum_Instances_Must_Have_Unique_Values();
        Vnum_Instances_MustHave_Unique_Codes();
        All_Vnum_Instances_Must_Have_Matching_Enum();
        //All_Enum_Instances_Must_Convert_To_Vnum(); //Failed test, as expected
    }

    [Fact]
    public void BadSampleVnum_ShouldFailTest()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => All_Enum_Instances_Must_Convert_To_Vnum());
        Assert.Equal("'2' is not a valid value in BadSampleVnum", ex.Message);
    }
}
