using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Skywithin.VnumEnumeration.Analyzers;

namespace Skywithin.VnumEnumeration.Tests;

public class VnumDeclarationAnalyzerTests
{
    [Fact]
    public async Task Well_Formed_Types_Should_Report_Nothing()
    {
        var diagnostics = await Analyze("""
            public enum StatusId { Pending = 1, Shipped = 2 }

            public sealed class Status : Vnum<StatusId>
            {
                private Status(StatusId id, string code, string description) : base(id, code) { }

                public static readonly Status Pending = new(StatusId.Pending, "PENDING", "Waiting");
                public static readonly Status Shipped = new(StatusId.Shipped, "SHIPPED", "On its way");

                // An alias is the same instance, not a duplicate
                public static readonly Status Default = Pending;
            }

            public sealed class Plain : Vnum
            {
                private Plain(long value, string code) : base(value, code) { }

                public static readonly Plain One = new(1, "one");
                public static readonly Plain Two = new(2, "two");
            }
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Null_Public_Static_Field_Should_Report_VNUM001()
    {
        var diagnostics = await Analyze("""
            public sealed class Status : Vnum
            {
                private Status(long value, string code) : base(value, code) { }

                public static readonly Status One = new(1, "one");
                public static readonly Status? None = null;
                public static Status? Unassigned;

                // Not discovered at runtime, so not reported
                private static readonly Status? Hidden = null;
            }
            """);

        Assert.Equal(
            [
                "VNUM001: Public static field 'Status.None' is null: it stops 'Status' lookups from being cached, and will throw in the next major version",
                "VNUM001: Public static field 'Status.Unassigned' is null: it stops 'Status' lookups from being cached, and will throw in the next major version",
            ],
            diagnostics);
    }

    [Fact]
    public async Task Field_Without_Initializer_Should_Not_Report_When_A_Static_Constructor_Could_Assign_It()
    {
        var diagnostics = await Analyze("""
            public sealed class Status : Vnum
            {
                private Status(long value, string code) : base(value, code) { }

                public static readonly Status One;

                static Status() { One = new(1, "one"); }
            }
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Lookup_Inside_Own_Static_Initializer_Should_Report_VNUM002()
    {
        var diagnostics = await Analyze("""
            public sealed class Status : Vnum
            {
                private Status(long value, string code) : base(value, code) { }

                public static readonly Status One = new(1, "one");
                public static readonly Status Default = FromValue<Status>(1);
                private static readonly int Count = Vnum.GetAll<Status>().Count();
                public static readonly Status Two = new(2, "two");
            }
            """);

        const string message =
            "VNUM002: 'Status' is looked up before all of its public static fields are assigned: " +
            "this will fail at type load in the next major version; reference the field directly instead";

        // Default is itself unassigned during its lookup; Count runs before Two is assigned
        Assert.Equal([message, message], diagnostics);
    }

    [Fact]
    public async Task Lookup_That_Runs_After_Initialization_Should_Report_Nothing()
    {
        var diagnostics = await Analyze("""
            public sealed class Status : Vnum
            {
                private Status(long value, string code) : base(value, code) { }

                public static readonly Status One = new(1, "one");
                public static readonly Status Two = new(2, "two");

                // Every public static field is assigned by the time these run
                private static readonly Dictionary<string, Status> ByCode = GetAll<Status>().ToDictionary(s => s.Code);
                private static readonly Func<Status> Lazy = () => FromValue<Status>(1);

                // A property getter and a method run on demand, not during initialization
                public static Status First => FromValue<Status>(1);
                public static Status Find(long value) => FromValue<Status>(value);
            }

            public static class Elsewhere
            {
                // A lookup of another type is not a lookup during that type's own initialization
                private static readonly Status First = Vnum.GetAll<Status>().First();
            }
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Duplicate_Value_Or_Code_Should_Report_VNUM003()
    {
        var diagnostics = await Analyze("""
            public sealed class Status : Vnum
            {
                private Status(long value, string code) : base(value, code) { }

                public static readonly Status A = new(1, "a");
                public static readonly Status B = new(1, "b");
                public static readonly Status C = new(3, "a");
                public static readonly Status D = new(4, "B");
            }
            """);

        Assert.Equal(
            [
                "VNUM003: 'Status.B' has the same value (1) as 'Status.A': the first lookup on 'Status' will throw in the next major version",
                "VNUM003: 'Status.C' has the same code (\"a\") as 'Status.A': the first lookup on 'Status' will throw in the next major version",
                "VNUM003: 'Status.D' has the same code ignoring case (\"B\") as 'Status.B': the first lookup on 'Status' will throw in the next major version",
            ],
            diagnostics);
    }

    [Fact]
    public async Task Duplicate_Should_Be_Found_Through_Enum_Arguments_And_An_Intermediate_Base_Class()
    {
        var diagnostics = await Analyze("""
            public enum StatusId { Pending = 1, Shipped = 2 }

            public abstract class AppVnum<TEnum> : Vnum<TEnum> where TEnum : struct, Enum
            {
                protected AppVnum(TEnum id, string code, string description) : base(id, code) { }
            }

            public sealed class Status : AppVnum<StatusId>
            {
                private Status(StatusId id, string description, string code = "DEFAULT") : base(id, code, description) { }

                public static readonly Status Pending = new(StatusId.Pending, "Waiting", "PENDING");
                public static readonly Status Shipped = new(StatusId.Pending, "On its way", code: "SHIPPED");
                public static readonly Status First = new(StatusId.Shipped, "No code given");
                public static readonly Status Second = new(code: "default", id: (StatusId)7, description: "Named, reordered");
            }
            """);

        Assert.Equal(
            [
                "VNUM003: 'Status.Shipped' has the same value (1) as 'Status.Pending': the first lookup on 'Status' will throw in the next major version",
                "VNUM003: 'Status.Second' has the same code ignoring case (\"default\") as 'Status.First': the first lookup on 'Status' will throw in the next major version",
            ],
            diagnostics);
    }

    [Fact]
    public async Task Undeclared_Enum_Value_In_Long_Constructor_Should_Report_VNUM004()
    {
        var diagnostics = await Analyze("""
            public enum ByteId : byte { One = 1 }

            [Flags]
            public enum PermissionId : byte { Read = 1, Write = 2 }

            public sealed class ByteVnum : Vnum<ByteId>
            {
                private ByteVnum(long value, string code) : base(value, code) { }
                private ByteVnum(ByteId id, string code) : base(id, code) { }

                public static readonly ByteVnum One = new(1, "one");
                public static readonly ByteVnum Seven = new(7, "seven");

                // The TEnum constructor trusts its argument
                public static readonly ByteVnum Cast = new((ByteId)9, "nine");
            }

            public sealed class Permission : Vnum<PermissionId>
            {
                private Permission(long value, string code) : base(value, code) { }

                // Combined [Flags] values are not declared members and are allowed
                public static readonly Permission ReadWrite = new(3, "rw");
            }
            """);

        Assert.Equal(
            ["VNUM004: 7 is not a declared member of 'ByteId': constructing 'ByteVnum.Seven' will throw in the next major version"],
            diagnostics);
    }

    [Fact]
    public async Task Values_The_Compiler_Cannot_See_Should_Report_Nothing()
    {
        var diagnostics = await Analyze("""
            public sealed class Status : Vnum
            {
                private static long Next() => 1;

                // The value is transformed on the way to the base constructor
                private Status(long value, string code) : base(value * 10, code.ToUpperInvariant()) { }
                private Status(string code) : base(Next(), code) { }

                public static readonly Status A = new(1, "a");
                public static readonly Status B = new(1, "a");
                // Both get the value 1 from Next(), which only the runtime check can see
                public static readonly Status C = new("c");
                public static readonly Status D = new("d");
                public static readonly Status E = Create();

                private static Status Create() => new(1, "a");
            }
            """);

        Assert.Empty(diagnostics);
    }

    private static async Task<string[]> Analyze(string source)
    {
        const string usings = """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using Skywithin.VnumEnumeration;

            """;

        // Reference everything the test process itself runs against, which includes the Vnum library.
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path));

        var compilation = CSharpCompilation.Create(
            "Sample",
            [CSharpSyntaxTree.ParseText(usings + source)],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        // A sample that does not compile would make an empty result meaningless
        Assert.Empty(compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));

        var diagnostics = await compilation
            .WithAnalyzers([new VnumDeclarationAnalyzer()])
            .GetAnalyzerDiagnosticsAsync();

        return diagnostics
            .OrderBy(d => d.Location.SourceSpan.Start)
            .Select(d => $"{d.Id}: {d.GetMessage()}")
            .ToArray();
    }
}
