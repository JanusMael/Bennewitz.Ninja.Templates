using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Bennewitz.Ninja.PkgStem.Rules;

/// <summary>
/// BNXX1001: a public type of a library is named for being a helper.
/// </summary>
/// <remarks>
/// <para>
/// ⭐ <b>A sample.</b> It is here so the package has a rule to compile, test, pack and see fire in a
/// consumer's build from the first run. Replace it with the package's first real rule, as a library
/// replaces <c>Greeting</c>, and keep the shape: a gate in <see cref="EnterScope"/>, actions in
/// <see cref="Register"/>, and the three kinds of test in <c>HelperNameTests</c>.
/// </para>
/// <para>
/// In scope only where the compilation is a library: an application's public types are nobody's
/// surface, so for every other output kind the gate returns null and the rule registers nothing.
/// </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class HelperNameAnalyzer : ScopedAnalyzer<Compilation>
{
    /// <summary>The rule's ID.</summary>
    public const string Id = Catalog.Prefix + "1001";

    private const string Suffix = "Helper";

    /// <summary>The rule.</summary>
    internal static readonly DiagnosticDescriptor Rule = new(
        Id,
        title: "A library's public type is named for being a helper",
        messageFormat: "'{0}' is public and named for being a helper; name it for what it does",
        category: Catalog.Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A name ending in Helper says what is left over, not what the type does. In a library's public surface every consumer comes to depend on that name.",
        helpLinkUri: Catalog.HelpLink(Id));

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    /// <inheritdoc />
    protected override Compilation? EnterScope(Compilation compilation, AnalyzerOptions options, CancellationToken cancellationToken) =>
        compilation.Options.OutputKind == OutputKind.DynamicallyLinkedLibrary ? compilation : null;

    /// <inheritdoc />
    protected override void Register(CompilationStartAnalysisContext context, Compilation scope) =>
        context.RegisterSymbolAction(Analyze, SymbolKind.NamedType);

    private static void Analyze(SymbolAnalysisContext context)
    {
        INamedTypeSymbol type = (INamedTypeSymbol)context.Symbol;

        if (type.Name.EndsWith(Suffix, StringComparison.Ordinal) && IsPublicSurface(type))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, type.Locations[0], type.Name));
        }
    }

    /// <summary>Public, and nested only in public types, so a consumer can name it.</summary>
    private static bool IsPublicSurface(INamedTypeSymbol type)
    {
        for (INamedTypeSymbol? current = type; current is not null; current = current.ContainingType)
        {
            if (current.DeclaredAccessibility != Accessibility.Public)
            {
                return false;
            }
        }

        return true;
    }
}
