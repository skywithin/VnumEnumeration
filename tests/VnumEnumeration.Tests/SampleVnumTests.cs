using System;
using Skywithin.VnumEnumeration.Tests.Data;
using Skywithin.VnumEnumeration.Tests.Helpers;

namespace Skywithin.VnumEnumeration.Tests;

public class SampleVnumTests : VnumTestingHelper<SampleVnum, SampleId>
{
    [Fact]
    public void Run_SampleVnum_Successful_Tests()
    {
        Vnum_Instances_Must_Have_Unique_Values();
        Vnum_Instances_MustHave_Unique_Codes();
        All_Vnum_Instances_Must_Have_Matching_Enum();
        All_Enum_Instances_Must_Convert_To_Vnum();
    }
}
