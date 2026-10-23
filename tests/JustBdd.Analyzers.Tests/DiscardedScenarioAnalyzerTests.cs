using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace Davish.JustBdd.Analyzers.Tests;

public class DiscardedScenarioAnalyzerTests
{
    // {|JBDD001:...|} marks where the diagnostic must be reported; code without markup must produce no diagnostic.
    private static Task Verify(string members, params DiagnosticResult[] expected)
    {
        var test = new CSharpAnalyzerTest<DiscardedScenarioAnalyzer, DefaultVerifier>
        {
            TestCode = $$"""
                using System;
                using System.Threading.Tasks;
                using Davish.JustBdd;

                class C
                {
                {{members}}
                }
                """,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        };
        test.TestState.AdditionalReferences.Add(typeof(Scenario).Assembly);
        test.ExpectedDiagnostics.AddRange(expected);
        return test.RunAsync(TestContext.Current.CancellationToken);
    }

    // ---- Reported

    [Fact]
    public Task Chain_discarded_in_a_sync_method()
        => Verify("""
            void M() { {|JBDD001:new Scenario().Given(() => 1).When(x => x + 1).Then(x => { })|}; }
            """);

    [Fact]
    public Task Chain_discarded_in_an_async_method()
        => Verify("""
            async Task M() { {|JBDD001:new Scenario().Then(() => { })|}; await Task.CompletedTask; }
            """);

    [Fact]
    public Task Every_stage_type_is_reported()
        => Verify("""
            void M()
            {
                {|JBDD001:new Scenario()|};
                {|JBDD001:new Scenario().Given(() => 1)|};
                {|JBDD001:new Scenario().Given(() => 1).When(x => x)|};
                {|JBDD001:new Scenario().Then(() => { })|};
            }
            """);

    [Fact]
    public Task Discarded_RunAsync_result_is_reported()
        => Verify("""
            void M() { {|JBDD001:new Scenario().Then(() => { }).RunAsync()|}; }
            """);

    [Fact]
    public Task Expression_bodied_void_members_are_reported()
        => Verify("""
            void M() => {|JBDD001:new Scenario().Then(() => { })|};
            Action A() => () => {|JBDD001:new Scenario().Then(() => { })|};
            """);

    // ---- Not reported

    [Fact]
    public Task Run_or_awaited_RunAsync()
        => Verify("""
            void M()
            {
                new Scenario().Then(() => { }).Run();   // The ScenarioResult may be discarded.
                var result = new Scenario().Then(() => { }).Run();
            }
            async Task N() => await new Scenario().Then(() => { }).RunAsync();
            """);

    [Fact]
    public Task Explicit_discard_and_assignment()
        => Verify("""
            ThenStage<Unit>? _field;
            void M()
            {
                _ = new Scenario().Then(() => { });
                _field = new Scenario().Then(() => { });
            }
            """);

    [Fact]
    public Task Stored_returned_or_passed_chain()
        => Verify("""
            ThenStage<Unit> Build() => new Scenario().Then(() => { });
            static void Take(ThenStage<Unit> stage) { }
            async Task M()
            {
                var stage = Build();
                Take(new Scenario().Then(() => { }));
                await stage.RunAsync();
            }
            """);

    [Fact]
    public Task Same_named_types_in_other_namespaces()
        => Verify("""
            void M() { new Other.ThenStage<int>(); new Other.Scenario(); }
            }

            namespace Other
            {
                class ThenStage<T> { }
                class Scenario { }
            """);

    // ---- A step lambda that returns a scenario turns the scenario into state instead of running it

    private static DiagnosticResult Expected(int location, string advice)
        => new DiagnosticResult(DiscardedScenarioAnalyzer.DiagnosticId, Microsoft.CodeAnalysis.DiagnosticSeverity.Warning)
            .WithLocation(location)
            .WithMessage("The scenario is never run. " + advice);

    private const string GivenAdvice = "Pass it directly: Given(\"...\", scenario).";
    private const string WhenAdvice = "Run it inside the step, or pass it to Given.";

    [Fact]
    public Task Given_lambda_returning_a_scenario_is_reported()
        => Verify("""
            void M()
            {
                new Scenario().Given("Prepared", {|#0:() => new Scenario().Given(() => 1).Then(x => { })|}).When(s => s).Then(s => { }).Run();   // reported once, not again on the When
                new Scenario().Given({|#1:() => Task.FromResult(new Scenario().Then(() => { }))|}).Then(s => { }).Run();   // Task-returning; async lambdas are ambiguous under C# 12
            }
            """, Expected(0, GivenAdvice), Expected(1, GivenAdvice));

    [Fact]
    public Task When_lambda_returning_a_scenario_is_reported()
        => Verify("""
            void M()
            {
                new Scenario().Given(() => 1).When({|#0:x => new Scenario().Then(() => { })|}).Then(s => { }).Run();
                new Scenario().Given(() => { }).When("unit", {|#1:() => new Scenario().Then(() => { })|}).Then(s => { }).Run();
            }
            """, Expected(0, WhenAdvice), Expected(1, WhenAdvice));

    [Fact]
    public Task Then_lambda_returning_a_scenario_is_a_discarded_chain()
        => Verify("""
            void M()
            {
                new Scenario().Given(() => 1)
                    .Then(x => {|#0:new Scenario().Then(() => { })|})
                    .And(x => {|#1:new Scenario().Then(() => { })|})
                    .Run();
            }
            """,
            Expected(0, "End the chain with Run(), or await RunAsync()."),
            Expected(1, "End the chain with Run(), or await RunAsync()."));

    [Fact]
    public Task Passing_a_scenario_or_returning_a_value_is_not_reported()
        => Verify("""
            void M()
            {
                var prepared = new Scenario().Given(() => 1).Then(x => { });
                new Scenario().Given("Prepared", prepared).When(x => x + 1).Then(x => { }).Run();
                new Scenario().Given(() => 1).When(x => new Scenario().Given(() => x).Then(y => { }).Run()).Then(r => { }).Run();
            }
            """);
}
