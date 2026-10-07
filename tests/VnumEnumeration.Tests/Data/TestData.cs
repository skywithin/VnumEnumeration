namespace Skywithin.VnumEnumeration.Tests.Data;

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
    private SampleVnum(SampleId value, string code) : base(value, code) { }

    public static readonly SampleVnum One = new (SampleId.One, "one");
    public static readonly SampleVnum Two = new (SampleId.Two, "two");
}

public sealed class BadSampleVnum : Vnum<SampleId>
{
    private BadSampleVnum(SampleId value, string code) : base(value, code) { }

    public static readonly BadSampleVnum One = new(SampleId.One, "one");

    // "Two" is intentionally missing, which should cause a test failure
}

public sealed class TestVnum1 : Vnum
{
    public string CustomDescription { get; } = null!;
    private string PrivateProp { get; } = "private";
    internal string InternalProp { get; } = "internal";

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

    private TestVnum3(int value, string code) : base(value, code)
    {
    }
}

public sealed class PrivateVnum : Vnum
{
    private static readonly PrivateVnum HiddenOption = new (1, "HiddenOption");

    private PrivateVnum(int value, string code) : base(value, code) { }
}

public sealed class AliasedVnum : Vnum
{
    private AliasedVnum(int value, string code) : base(value, code) { }

    public static readonly AliasedVnum One = new(1, "one");
    public static readonly AliasedVnum Two = new(2, "two");

    // Alias of an existing instance; must not be reported as a separate instance
    public static readonly AliasedVnum Default = One;
}

public sealed class ReentrantVnum : Vnum
{
    private ReentrantVnum(int value, string code) : base(value, code) { }

    public static readonly ReentrantVnum One = new(1, "one");

    // Looks up an instance while the type's static initializer is still running,
    // before Two has been assigned
    public static readonly ReentrantVnum Default = FromValue<ReentrantVnum>(1);

    public static readonly ReentrantVnum Two = new(2, "two");
}

public enum ByteId : byte
{
    One = 1
}

public sealed class ByteVnum : Vnum<ByteId>
{
    private ByteVnum(long value, string code) : base(value, code) { }

    public static readonly ByteVnum One = new(1, "one");

    public static ByteVnum Create(long value) => new(value, "created");
}
