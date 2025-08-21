using System.Text.RegularExpressions;

namespace VnumEnumeration.Tests.Helpers;

public class VnumTestingHelper<TVnum, TEnum>
    where TVnum : Vnum<TEnum>, new()
    where TEnum : struct, Enum
{
    /// <summary>
    /// Validates that all Vnum instances of type <typeparamref name="TVnum"/> 
    /// have unique integer values.
    /// </summary>
    public void Vnum_Instances_Must_Have_Unique_Values()
    {
        var hashSet = new HashSet<int>();

        foreach (TVnum item in Vnum.GetAll<TVnum>())
        {
            if (!hashSet.Add(item.Value))
            {
                Assert.Fail($"Duplicate value detected in Vnum '{typeof(TVnum).Name}' for value '{item.Value}'");
            }
        }
    }

    /// <summary>
    /// Validates that all Vnum instances of type <typeparamref name="TVnum"/> 
    /// have unique codes, with optional check for pattern matching.
    /// </summary>
    public void Vnum_Instances_MustHave_Unique_Codes(string? codePattern = null)
    {
        var hashSet = new HashSet<string>();

        foreach (TVnum item in Vnum.GetAll<TVnum>())
        {
            var code =
                codePattern is null
                    ? item.Code.ToUpper()
                    : item.Code;

            if (string.IsNullOrEmpty(code))
            {
                Assert.Fail($"Vnum instance '{typeof(TVnum).Name}' has an empty code, but a non-empty code is required");
            }

            if (!string.IsNullOrEmpty(codePattern) && !Regex.IsMatch(input: code, pattern: codePattern))
            {
                Assert.Fail($"Vnum instance '{typeof(TVnum).Name}' has code '{code}' that does not match required pattern '{codePattern}'");
            }

            if (!hashSet.Add(code))
            {
                Assert.Fail($"Duplicate code detected in Vnum '{typeof(TVnum).Name}' for code '{code}'");
            }
        }
    }

    ///<summary>
    /// Validates that every Vnum instance of type <typeparamref name="TVnum"/> has an associated enum value of type <typeparamref name="TEnum"/>.
    /// Throws an assertion if any Vnum instance does not match an enum value.
    /// </summary>
    public void All_Vnum_Instances_Must_Have_Matching_Enum()
    {
        // Arrange
        var enumValues = Enum.GetValues<TEnum>();

        // Assert
        foreach (var vnumInstance in Vnum.GetAll<TVnum>())
        {
            Assert.True(
                Array.Exists(enumValues, e => e.Equals(vnumInstance.Id)),
                $"Vnum instance '{vnumInstance.Code}' with value '{vnumInstance.Value}' does not match any enum value of type '{typeof(TEnum).Name}'"
            );
        }
    }

    /// <summary>
    /// Validates that every enum value of type <typeparamref name="TEnum"/> can be converted to a Vnum instance of type <typeparamref name="TVnum"/>.
    /// Throws an assertion if any enum value does not have a corresponding Vnum instance.
    /// </summary>
    public void All_Enum_Instances_Must_Convert_To_Vnum()
    {
        foreach (var enumInstance in Enum.GetValues<TEnum>())
        {
            var vnum = Vnum.FromEnum<TVnum, TEnum>(enumInstance);

            Assert.True(vnum is not null);
        }
    }
}
