namespace VnumEnumeration.Tests.Data;

//┌────────────────────────────────────────────────────────────────────┐
//│                             TEST DATA                              │
//└────────────────────────────────────────────────────────────────────┘

public enum SampleId
{
    One = 1,
    Two = 2
}

public sealed class SampleVnum : Vnum<SampleId>
{
    public SampleVnum() { }
    private SampleVnum(SampleId value, string code) : base(value, code) { }

    public static readonly SampleVnum One = new (SampleId.One, "one");
    public static readonly SampleVnum Two = new (SampleId.Two, "two");
}

public sealed class BadSampleVnum : Vnum<SampleId>
{
    public BadSampleVnum() { }
    private BadSampleVnum(SampleId value, string code) : base(value, code) { }

    public static readonly BadSampleVnum One = new(SampleId.One, "one");

    // "Two" is intentionally missing, which should cause a test failure
}

public sealed class TestVnum1 : Vnum
{
    public string CustomDescription { get; } = null!;
    private string PrivateProp { get; } = "private";
    internal string InternalProp { get; } = "internal";

    public TestVnum1() { }
    private TestVnum1(int value, string code, string customDescription) : base(value, code)
    {
        CustomDescription = customDescription;
    }

    public static readonly TestVnum1 OptionOne = new(1, "OptionOne", "OptionOne_Description");
    public static readonly TestVnum1 OptionTwo = new(2, "OptionTwo", "OptionTwo_Description");

    public static TestVnum1 Stub(int value, string code) => new (value, code, customDescription: string.Empty);
}

public sealed class TestVnum3 : Vnum
{
    public static readonly TestVnum3 OptionOne = new (1, "OptionOne");

    public TestVnum3() { }
    private TestVnum3(int value, string code) : base(value, code)
    {
    }
}

public sealed class PrivateVnum : Vnum
{
    private static readonly PrivateVnum HiddenOption = new (1, "HiddenOption");

    public PrivateVnum() { }
    private PrivateVnum(int value, string code) : base(value, code) { }
}
