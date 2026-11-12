using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Skywithin.VnumEnumeration.Analyzers;

/// <summary>
/// Reports Vnum declarations that work today but will throw in the next major version.
/// It only sees what the compiler can prove: constant constructor arguments passed straight through to
/// the Vnum base constructor. Anything computed is left to the runtime checks.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class VnumDeclarationAnalyzer : DiagnosticAnalyzer
{
    private const string Category = "Usage";

    private static readonly DiagnosticDescriptor NullField = new(
        "VNUM001",
        "Vnum field is null",
        "Public static field '{0}.{1}' is null: it stops '{0}' lookups from being cached, and will throw in the next major version",
        Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor LookupInInitializer = new(
        "VNUM002",
        "Vnum lookup inside its own static initializer",
        "'{0}' is looked up before all of its public static fields are assigned: this will fail at type load in the next major version; reference the field directly instead",
        Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor Duplicate = new(
        "VNUM003",
        "Duplicate Vnum value or code",
        "'{0}.{1}' has the same {2} as '{0}.{3}': the first lookup on '{0}' will throw in the next major version",
        Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor UndeclaredEnumValue = new(
        "VNUM004",
        "Vnum value is not a declared enum member",
        "{0} is not a declared member of '{1}': constructing '{2}.{3}' will throw in the next major version",
        Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);

    private static readonly ImmutableHashSet<string> LookupMethods = ImmutableHashSet.Create(
        "GetAll", "FromValue", "TryFromValue", "FromEnum", "TryFromEnum", "FromCode", "TryFromCode");

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(NullField, LookupInInitializer, Duplicate, UndeclaredEnumValue);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(start =>
        {
            var vnum = start.Compilation.GetTypeByMetadataName("Skywithin.VnumEnumeration.Vnum");
            var vnumOfEnum = start.Compilation.GetTypeByMetadataName("Skywithin.VnumEnumeration.Vnum`1");
            if (vnum is null || vnumOfEnum is null)
            {
                return;
            }

            var flags = start.Compilation.GetTypeByMetadataName("System.FlagsAttribute");
            var known = new KnownTypes(vnum, vnumOfEnum, flags);

            start.RegisterSymbolAction(c => AnalyzeType(c, known), SymbolKind.NamedType);
            start.RegisterOperationAction(c => AnalyzeLookup(c, known), OperationKind.Invocation);
        });
    }

    private static void AnalyzeType(SymbolAnalysisContext context, KnownTypes known)
    {
        var type = (INamedTypeSymbol)context.Symbol;
        if (type.TypeKind != TypeKind.Class || !DerivesFrom(type, known.Vnum))
        {
            return;
        }

        var hasStaticConstructor = type.StaticConstructors.Any(c => !c.IsImplicitlyDeclared);
        var byValue = new Dictionary<long, IFieldSymbol>();
        var byCode = new Dictionary<string, IFieldSymbol>(StringComparer.Ordinal);
        var byCodeIgnoreCase = new Dictionary<string, IFieldSymbol>(StringComparer.OrdinalIgnoreCase);

        foreach (var field in DiscoveredFields(type))
        {
            var cancellation = context.CancellationToken;
            if (field.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(cancellation) is not VariableDeclaratorSyntax declarator)
            {
                continue;
            }

            var initializer = declarator.Initializer?.Value;
            if (initializer is null)
            {
                // Without a static constructor nothing can assign the field, so it stays null.
                if (!hasStaticConstructor)
                {
                    context.ReportDiagnostic(Diagnostic.Create(NullField, declarator.Identifier.GetLocation(), type.Name, field.Name));
                }

                continue;
            }

            var value = Unwrap(GetOperation(context.Compilation, initializer, cancellation));
            if (value is null)
            {
                continue;
            }

            if (value.ConstantValue is { HasValue: true, Value: null })
            {
                context.ReportDiagnostic(Diagnostic.Create(NullField, declarator.Identifier.GetLocation(), type.Name, field.Name));
                continue;
            }

            if (value is not IObjectCreationOperation { Constructor: { } constructor } creation
                || !TryResolve(context.Compilation, known, constructor, ConstantArguments(creation.Arguments, constructor), cancellation, out var resolved))
            {
                continue;
            }

            var location = initializer.GetLocation();
            var number = ToLong(resolved.Value);
            var code = resolved.Code is { HasValue: true, Value: string text } ? text : null;

            if (number is { } declaredValue)
            {
                ReportUndeclaredEnumValue(context, known, type, field, resolved.Terminal, declaredValue, location);

                if (byValue.TryGetValue(declaredValue, out var first))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Duplicate, location, type.Name, field.Name, $"value ({declaredValue})", first.Name));
                }
                else
                {
                    byValue.Add(declaredValue, field);
                }
            }

            if (code is not null)
            {
                if (byCode.TryGetValue(code, out var sameCode))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Duplicate, location, type.Name, field.Name, $"code (\"{code}\")", sameCode.Name));
                }
                else if (byCodeIgnoreCase.TryGetValue(code, out var sameIgnoringCase))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Duplicate, location, type.Name, field.Name, $"code ignoring case (\"{code}\")", sameIgnoringCase.Name));
                }
                else
                {
                    byCode.Add(code, field);
                    byCodeIgnoreCase.Add(code, field);
                }
            }
        }
    }

    private static void ReportUndeclaredEnumValue(
        SymbolAnalysisContext context, KnownTypes known, INamedTypeSymbol type, IFieldSymbol field,
        IMethodSymbol terminal, long value, Location location)
    {
        // Only the Vnum<TEnum>(long, string) constructor checks membership; the TEnum overload trusts its argument.
        if (!Same(terminal.ContainingType.OriginalDefinition, known.VnumOfEnum)
            || terminal.Parameters[0].Type.SpecialType != SpecialType.System_Int64
            || FindBase(type, known.VnumOfEnum)?.TypeArguments[0] is not INamedTypeSymbol { TypeKind: TypeKind.Enum } enumType
            || enumType.GetAttributes().Any(a => Same(a.AttributeClass, known.Flags)))
        {
            return;
        }

        var declared = enumType.GetMembers()
            .OfType<IFieldSymbol>()
            .Where(member => member.HasConstantValue)
            .Select(member => ToLong(new Optional<object?>(member.ConstantValue)));

        if (!declared.Contains(value))
        {
            context.ReportDiagnostic(Diagnostic.Create(UndeclaredEnumValue, location, value, enumType.Name, type.Name, field.Name));
        }
    }

    private static void AnalyzeLookup(OperationAnalysisContext context, KnownTypes known)
    {
        var invocation = (IInvocationOperation)context.Operation;
        var method = invocation.TargetMethod;

        if (!method.IsGenericMethod
            || !LookupMethods.Contains(method.Name)
            || !Same(method.ContainingType, known.Vnum)
            || method.TypeArguments[0] is not INamedTypeSymbol target)
        {
            return;
        }

        // Only static field and auto-property initializers of the looked-up type itself run during its
        // static initialization. A lambda in an initializer runs later, so it is not a lookup at this point.
        var member = context.ContainingSymbol;
        if (member is not (IFieldSymbol { IsStatic: true } or IPropertySymbol { IsStatic: true })
            || !Same(member.ContainingType, target)
            || IsInsideAnonymousFunction(invocation))
        {
            return;
        }

        // The lookup fails only while a discovered field is still unassigned: the field being
        // initialized, or one declared after this member.
        var position = member.Locations[0];
        var unassigned = DiscoveredFields(target).Any(field =>
            Same(field, member)
            || (field.Locations[0].SourceTree == position.SourceTree
                && field.Locations[0].SourceSpan.Start > position.SourceSpan.Start));

        if (unassigned)
        {
            context.ReportDiagnostic(Diagnostic.Create(LookupInInitializer, invocation.Syntax.GetLocation(), target.Name));
        }
    }

    /// <summary>
    /// Follows a constructor's <c>base(...)</c> / <c>this(...)</c> chain down to the Vnum base constructor and
    /// returns the constants that arrive as its <c>value</c> and <c>code</c> parameters.
    /// </summary>
    private static bool TryResolve(
        Compilation compilation, KnownTypes known, IMethodSymbol constructor, Optional<object?>[] arguments,
        CancellationToken cancellation, out Resolved resolved)
    {
        resolved = default;

        for (var depth = 0; depth < 8; depth++)
        {
            var owner = constructor.ContainingType.OriginalDefinition;
            if (Same(owner, known.Vnum) || Same(owner, known.VnumOfEnum))
            {
                if (arguments.Length != 2)
                {
                    return false;
                }

                resolved = new Resolved(constructor, arguments[0], arguments[1]);
                return true;
            }

            if (constructor.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(cancellation) is not ConstructorDeclarationSyntax { Initializer: { } initializer })
            {
                return false;
            }

            var operation = GetOperation(compilation, initializer, cancellation);
            if (operation is IExpressionStatementOperation statement)
            {
                operation = statement.Operation;
            }

            if (operation is not IInvocationOperation call)
            {
                return false;
            }

            arguments = ConstantArguments(call.Arguments, call.TargetMethod, arguments);
            constructor = call.TargetMethod;
        }

        return false;
    }

    /// <summary>
    /// Maps each parameter of <paramref name="target"/> to the constant passed for it, if any. A parameter of
    /// the calling constructor that is passed straight through takes the caller's constant.
    /// </summary>
    private static Optional<object?>[] ConstantArguments(
        ImmutableArray<IArgumentOperation> arguments, IMethodSymbol target, Optional<object?>[]? caller = null)
    {
        var constants = new Optional<object?>[target.Parameters.Length];

        foreach (var argument in arguments)
        {
            if (argument.Parameter is not { } parameter || parameter.Ordinal >= constants.Length)
            {
                continue;
            }

            if (argument.Value.ConstantValue.HasValue)
            {
                constants[parameter.Ordinal] = argument.Value.ConstantValue;
            }
            else if (caller is not null
                && Unwrap(argument.Value) is IParameterReferenceOperation reference
                && reference.Parameter.Ordinal < caller.Length)
            {
                constants[parameter.Ordinal] = caller[reference.Parameter.Ordinal];
            }
        }

        return constants;
    }

    private static IOperation? GetOperation(Compilation compilation, SyntaxNode node, CancellationToken cancellation)
    {
        // A constructor chain can cross files, so the model for the current tree is not enough.
        // The lookups are few: one per Vnum field initializer and one per constructor in its chain.
#pragma warning disable RS1030 // Do not invoke Compilation.GetSemanticModel() method within a diagnostic analyzer
        return compilation.GetSemanticModel(node.SyntaxTree).GetOperation(node, cancellation);
#pragma warning restore RS1030
    }

    private static IOperation? Unwrap(IOperation? operation)
    {
        while (operation is IConversionOperation or IParenthesizedOperation)
        {
            operation = operation is IConversionOperation conversion
                ? conversion.Operand
                : ((IParenthesizedOperation)operation).Operand;
        }

        return operation;
    }

    private static bool IsInsideAnonymousFunction(IOperation operation)
    {
        for (var parent = operation.Parent; parent is not null; parent = parent.Parent)
        {
            if (parent is IAnonymousFunctionOperation or ILocalFunctionOperation)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// The fields runtime discovery reads: public static fields declared on the type whose type is assignable to it.
    /// </summary>
    private static IEnumerable<IFieldSymbol> DiscoveredFields(INamedTypeSymbol type) =>
        type.GetMembers()
            .OfType<IFieldSymbol>()
            .Where(field => field is { IsStatic: true, IsConst: false, IsImplicitlyDeclared: false, DeclaredAccessibility: Accessibility.Public }
                && field.Type is INamedTypeSymbol fieldType
                && (Same(fieldType, type) || DerivesFrom(fieldType, type)));

    private static bool DerivesFrom(INamedTypeSymbol type, INamedTypeSymbol ancestor) =>
        FindBase(type, ancestor) is not null;

    private static INamedTypeSymbol? FindBase(INamedTypeSymbol type, INamedTypeSymbol ancestor)
    {
        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (Same(current.OriginalDefinition, ancestor.OriginalDefinition))
            {
                return current;
            }
        }

        return null;
    }

    private static bool Same(ISymbol? left, ISymbol? right) =>
        SymbolEqualityComparer.Default.Equals(left, right);

    private static long? ToLong(Optional<object?> constant) =>
        constant.HasValue
            ? constant.Value switch
            {
                sbyte v => v,
                byte v => v,
                short v => v,
                ushort v => v,
                int v => v,
                uint v => v,
                long v => v,
                ulong v when v <= long.MaxValue => (long)v,
                _ => null,
            }
            : null;

    private sealed class KnownTypes(INamedTypeSymbol vnum, INamedTypeSymbol vnumOfEnum, INamedTypeSymbol? flags)
    {
        public INamedTypeSymbol Vnum { get; } = vnum;
        public INamedTypeSymbol VnumOfEnum { get; } = vnumOfEnum;
        public INamedTypeSymbol? Flags { get; } = flags;
    }

    private readonly struct Resolved(IMethodSymbol terminal, Optional<object?> value, Optional<object?> code)
    {
        public IMethodSymbol Terminal { get; } = terminal;
        public Optional<object?> Value { get; } = value;
        public Optional<object?> Code { get; } = code;
    }
}
