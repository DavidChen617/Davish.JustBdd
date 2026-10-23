using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Davish.JustBdd.Analyzers;

/// <summary>
/// JBDD001: a scenario chain used as a statement is never run. Scenarios are lazy and run only through Run() or
/// RunAsync(); a discarded chain lets the test pass without running any step.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DiscardedScenarioAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "JBDD001";

    internal static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        title: "Scenario is never run",
        messageFormat: "The scenario is never run. End the chain with Run(), or await RunAsync().",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Scenarios run only through Run() or RunAsync(). A discarded chain lets the test pass without running any step.");

    // Matched by metadata name: this netstandard2.0 analyzer cannot reference the core assembly.
    private static readonly string[] StageTypeNames =
    [
        "Davish.JustBdd.Scenario",
        "Davish.JustBdd.GivenStage`1",
        "Davish.JustBdd.WhenStage`2",
        "Davish.JustBdd.ThenStage`1",
    ];

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            var stages = StageTypeNames
                .Select(start.Compilation.GetTypeByMetadataName)
                .OfType<INamedTypeSymbol>()
                .ToImmutableHashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
            if (stages.IsEmpty) return;   // The compilation does not reference Davish.JustBdd.

            var valueTask = start.Compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask`1");
            var scenarioResult = start.Compilation.GetTypeByMetadataName("Davish.JustBdd.ScenarioResult");
            start.RegisterOperationAction(c => Analyze(c, stages, valueTask, scenarioResult), OperationKind.ExpressionStatement);
        });
    }

    private static void Analyze(OperationAnalysisContext context, ImmutableHashSet<INamedTypeSymbol> stages,
        INamedTypeSymbol? valueTask, INamedTypeSymbol? scenarioResult)
    {
        var expression = ((IExpressionStatementOperation)context.Operation).Operation;
        // Assignments, including `_ = chain`, keep or deliberately discard the chain.
        if (expression is IAssignmentOperation || expression.Type is not INamedTypeSymbol type) return;

        if (stages.Contains(type.OriginalDefinition) || IsRunAsyncResult(type, valueTask, scenarioResult))
            context.ReportDiagnostic(Diagnostic.Create(Rule, expression.Syntax.GetLocation()));
    }

    // RunAsync() returns ValueTask<ScenarioResult>; discarding it leaves the run unobserved.
    private static bool IsRunAsyncResult(INamedTypeSymbol type, INamedTypeSymbol? valueTask, INamedTypeSymbol? scenarioResult)
        => valueTask is not null && scenarioResult is not null
            && SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, valueTask)
            && SymbolEqualityComparer.Default.Equals(type.TypeArguments[0], scenarioResult);
}
