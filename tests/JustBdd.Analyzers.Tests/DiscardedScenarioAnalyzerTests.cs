using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace Davish.JustBdd.Analyzers.Tests;

public class DiscardedScenarioAnalyzerTests
{
    // {|JBDD001:...|} marks where the diagnostic must be reported; code without markup must produce no diagnostic.
    private static Task Verify(string members)
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
}
