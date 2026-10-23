using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Davish.JustBdd.Analyzers;

/// <summary>
/// JBDD001: a scenario is never run. Scenarios are lazy and run only through Run() or RunAsync(), so the test passes
/// without running any step when a chain is discarded as a statement, or when a Given or When lambda returns a
/// scenario, which then becomes the state instead of running.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DiscardedScenarioAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "JBDD001";

    internal static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        title: "Scenario is never run",
        messageFormat: "The scenario is never run. {0}.",
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

    private const string EndAdvice = "End the chain with Run(), or await RunAsync()";
    private const string GivenAdvice = "Pass it directly: Given(\"...\", scenario)";
    private const string WhenAdvice = "Run it inside the step, or pass it to Given";

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

            var givenStage = start.Compilation.GetTypeByMetadataName("Davish.JustBdd.GivenStage`1");
            var whenStage = start.Compilation.GetTypeByMetadataName("Davish.JustBdd.WhenStage`2");
            start.RegisterOperationAction(c => AnalyzeStep(c, stages, givenStage, whenStage), OperationKind.Invocation);
        });
    }

    private static void Analyze(OperationAnalysisContext context, ImmutableHashSet<INamedTypeSymbol> stages,
        INamedTypeSymbol? valueTask, INamedTypeSymbol? scenarioResult)
    {
        var expression = ((IExpressionStatementOperation)context.Operation).Operation;
        // Assignments, including `_ = chain`, keep or deliberately discard the chain.
        if (expression is IAssignmentOperation || expression.Type is not INamedTypeSymbol type) return;

        if (stages.Contains(type.OriginalDefinition) || IsRunAsyncResult(type, valueTask, scenarioResult))
            context.ReportDiagnostic(Diagnostic.Create(Rule, expression.Syntax.GetLocation(), EndAdvice));
    }

    // A Given or When whose resulting state is itself a scenario: the lambda returned the scenario instead of running it.
    private static void AnalyzeStep(OperationAnalysisContext context, ImmutableHashSet<INamedTypeSymbol> stages,
        INamedTypeSymbol? givenStage, INamedTypeSymbol? whenStage)
    {
        var invocation = (IInvocationOperation)context.Operation;
        if (invocation.Type is not INamedTypeSymbol { IsGenericType: true } returned) return;

        var definition = returned.OriginalDefinition;
        var args = returned.TypeArguments;
        string advice;
        if (SymbolEqualityComparer.Default.Equals(definition, givenStage)) advice = GivenAdvice;
        // Report only where the state becomes a scenario, not on later Whens that pass it along.
        else if (SymbolEqualityComparer.Default.Equals(definition, whenStage) && !IsStage(args[0], stages)) advice = WhenAdvice;
        else return;
        if (!IsStage(args[args.Length - 1], stages)) return;

        var lambda = invocation.Arguments.Select(a => a.Value)
            .FirstOrDefault(v => v is IDelegateCreationOperation or IAnonymousFunctionOperation);
        context.ReportDiagnostic(Diagnostic.Create(Rule, (lambda?.Syntax ?? invocation.Syntax).GetLocation(), advice));
    }

    private static bool IsStage(ITypeSymbol type, ImmutableHashSet<INamedTypeSymbol> stages)
        => type is INamedTypeSymbol named && stages.Contains(named.OriginalDefinition);

    // RunAsync() returns ValueTask<ScenarioResult>; discarding it leaves the run unobserved.
    private static bool IsRunAsyncResult(INamedTypeSymbol type, INamedTypeSymbol? valueTask, INamedTypeSymbol? scenarioResult)
        => valueTask is not null && scenarioResult is not null
            && SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, valueTask)
            && SymbolEqualityComparer.Default.Equals(type.TypeArguments[0], scenarioResult);
}
