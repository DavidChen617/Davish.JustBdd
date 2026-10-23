namespace Davish.JustBdd.Tests;

/// <summary>Given(setup) prepares without producing state; Given&lt;TContext&gt;(setup) fills a new context that flows on.</summary>
public class GivenSetupTests
{
    private sealed class CheckoutContext
    {
        public List<string> Log { get; } = [];
        public int Total { get; set; }
    }

    private static async Task Later()
    {
        await Task.Yield();
        await Task.Delay(10);
    }

    // ---- Given(Action): no state

    [Fact]
    public void Action_setup_runs_and_produces_unit_state()
    {
        var log = new List<string>();

        var result = new Scenario("S")
            .Given("Seed the log", () => { log.Add("given"); })
            .When(_ => log.Add("when"))
            .Then(_ => Assert.Equal(["given", "when"], log))
            .Run();

        Assert.Equal((StepKind.Given, "Seed the log", "() => { log.Add(\"given\"); }"),
            (result.Steps[0].Step.Kind, result.Steps[0].Step.Description, result.Steps[0].Step.Code));
        Assert.True(result.Passed);
    }

    [Fact]
    public async Task Async_action_setups_are_awaited()
    {
        var done = new List<string>();

        await new Scenario().Given(async () => { await Later(); done.Add("async"); }).Then(_ => { }).RunAsync();
        await new Scenario().Given(() => new ValueTask(Later().ContinueWith(_ => done.Add("valuetask")))).Then(_ => { }).RunAsync();
        await new Scenario().Given("desc", async () => { await Later(); done.Add("desc"); }).Then(_ => { }).RunAsync();

        Assert.Equal(["async", "valuetask", "desc"], done);
    }

    [Fact]
    public void Value_returning_lambda_still_produces_state()
    {
        var observed = 0;

        new Scenario().Given(() => 41 + 1).Then(x => observed = x).Run();

        Assert.Equal(42, observed);
    }

    // ---- Given<TContext>(Action<TContext>): a new context flows through the chain

    [Fact]
    public void Context_setup_fills_a_new_context_that_flows_on()
    {
        CheckoutContext? given = null, then = null;

        new Scenario()
            .Given<CheckoutContext>("A context", ctx => { given = ctx; ctx.Log.Add("given"); })
            .When(ctx => { ctx.Log.Add("when"); ctx.Total = 10; })
            .Then(ctx => { then = ctx; Assert.Equal(10, ctx.Total); })
            .Run();

        Assert.NotNull(given);
        Assert.Same(given, then);
        Assert.Equal(["given", "when"], given.Log);
    }

    [Fact]
    public void Each_run_creates_a_new_context()
    {
        var seen = new List<CheckoutContext>();
        var stage = new Scenario().Given<CheckoutContext>(ctx => seen.Add(ctx)).Then(_ => { });

        stage.Run();
        stage.Run();

        Assert.Equal(2, seen.Count);
        Assert.NotSame(seen[0], seen[1]);
    }

    [Fact]
    public async Task Async_context_setups_are_awaited()
    {
        var totals = new List<int>();

        await new Scenario().Given<CheckoutContext>(async ctx => { await Later(); ctx.Total = 1; }).Then(ctx => totals.Add(ctx.Total)).RunAsync();
        await new Scenario().Given<CheckoutContext>(ctx => new ValueTask(Later().ContinueWith(_ => ctx.Total = 2))).Then(ctx => totals.Add(ctx.Total)).RunAsync();
        await new Scenario().Given<CheckoutContext>("desc", async ctx => { await Later(); ctx.Total = 3; }).Then(ctx => totals.Add(ctx.Total)).RunAsync();

        Assert.Equal([1, 2, 3], totals);
    }

    private static void SeedTotal(CheckoutContext ctx) => ctx.Total = 7;

    [Fact]
    public void Context_setup_accepts_a_method_group()
    {
        var result = new Scenario().Given<CheckoutContext>(SeedTotal).Then(ctx => Assert.Equal(7, ctx.Total)).Run();

        Assert.Equal("SeedTotal", result.Steps[0].Step.Code);
    }

    [Fact]
    public void ThenThrows_after_a_context_keeps_the_context_as_state()
    {
        CheckoutContext? state = null;

        new Scenario()
            .Given<CheckoutContext>(ctx => ctx.Total = 5)
            .When(ctx => { if (ctx.Total == 5) throw new InvalidOperationException(); })
            .ThenThrows<InvalidOperationException>()
            .Then(t => state = t.State)
            .Run();

        Assert.Equal(5, state?.Total);
    }

    [Fact]
    public void Failing_context_setup_is_recorded_as_the_given_step()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            new Scenario("S").Given<CheckoutContext>("Broken setup", _ => throw new InvalidOperationException()).Then(_ => { }).Run());

        Assert.Equal("Scenario 'S' > Given 'Broken setup'", ex.Data[Scenario.StepDataKey]);
    }
}
