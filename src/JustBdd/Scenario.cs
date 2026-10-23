using System.Runtime.CompilerServices;

namespace Davish.JustBdd;

/// <summary>
/// Lazy Given / When / Then execution. End the chain with <c>Run()</c>, or await <c>RunAsync()</c>, to execute it
/// and obtain a <see cref="ScenarioResult"/>.
/// <code>
/// new Scenario("Rename a user")
///     .Given("A user exists", () => new User("David"))
///     .When("Rename the user", user => user.Rename("Davish"))
///     .Then("The name is updated", user => Assert.Equal("Davish", user.Name))
///     .Run();
/// </code>
/// Descriptions are optional. A scenario defaults to the caller member name; steps default to their source code.
/// Every step captures its lambda source in <see cref="StepInfo.Code"/>.
/// Failures preserve the original exception. Its <c>Data</c> contains <see cref="StepDataKey"/> for the step path
/// and <see cref="ResultDataKey"/> for the <see cref="ScenarioResult"/>.
/// </summary>
public sealed class Scenario(
    [CallerMemberName] string description = "",
    [CallerFilePath] string filePath = "",
    [CallerLineNumber] int lineNumber = 0)
{
    public const string StepDataKey = Steps.StepKey;
    public const string ResultDataKey = Steps.ResultKey;

    private readonly ScenarioContext _ctx = new(description, filePath, lineNumber);

    private Scenario(ScenarioContext ctx) : this(ctx.Description, ctx.FilePath, ctx.LineNumber) => _ctx = ctx;

    public string Description => _ctx.Description;
    public string FilePath => _ctx.FilePath;
    public int LineNumber => _ctx.LineNumber;

    /// <summary>
    /// Returns a scenario that keeps running the consecutive Then, And, and Assert steps of a segment after one fails.
    /// Given, When, and state-producing steps such as ThenThrows still stop the scenario, as does the end of a segment.
    /// One failure throws the original exception; several throw <see cref="ScenarioFailuresException"/>.
    /// The option does not apply to nested scenarios.
    /// </summary>
    public Scenario ContinueOnFailure() => new(_ctx with { ContinueOnFailure = true });

    // ---- Given

    public GivenStage<T> Given<T>(string desc, Func<T> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => GivenCore(desc, code, Steps.Produce(fn));
    [OverloadResolutionPriority(1)]
    public GivenStage<T> Given<T>(string desc, Func<Task<T>> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => GivenCore(desc, code, Steps.ProduceTask(fn));
    public GivenStage<T> Given<T>(string desc, Func<ValueTask<T>> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => GivenCore(desc, code, fn);

    public GivenStage<T> Given<T>(Func<T> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => GivenCore(null, code, Steps.Produce(fn));
    [OverloadResolutionPriority(1)]
    public GivenStage<T> Given<T>(Func<Task<T>> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => GivenCore(null, code, Steps.ProduceTask(fn));
    public GivenStage<T> Given<T>(Func<ValueTask<T>> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => GivenCore(null, code, fn);

    // ---- Given setup without state: subsequent steps receive Unit

    public GivenStage<Unit> Given(string desc, Action fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => GivenCore(desc, code, Steps.ProduceUnit(fn));
    [OverloadResolutionPriority(1)]
    public GivenStage<Unit> Given(string desc, Func<Task> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => GivenCore(desc, code, Steps.ProduceUnitTask(fn));
    public GivenStage<Unit> Given(string desc, Func<ValueTask> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => GivenCore(desc, code, Steps.ProduceUnitValueTask(fn));

    public GivenStage<Unit> Given(Action fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => GivenCore(null, code, Steps.ProduceUnit(fn));
    [OverloadResolutionPriority(1)]
    public GivenStage<Unit> Given(Func<Task> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => GivenCore(null, code, Steps.ProduceUnitTask(fn));
    public GivenStage<Unit> Given(Func<ValueTask> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => GivenCore(null, code, Steps.ProduceUnitValueTask(fn));

    // ---- Given context setup: a new TContext is filled and flows on as the state

    public GivenStage<TContext> Given<TContext>(string desc, Action<TContext> setup, [CallerArgumentExpression(nameof(setup))] string code = "")
        where TContext : new()
        => GivenCore(desc, code, Steps.Setup(setup));
    [OverloadResolutionPriority(1)]
    public GivenStage<TContext> Given<TContext>(string desc, Func<TContext, Task> setup, [CallerArgumentExpression(nameof(setup))] string code = "")
        where TContext : new()
        => GivenCore(desc, code, Steps.SetupTask(setup));
    public GivenStage<TContext> Given<TContext>(string desc, Func<TContext, ValueTask> setup, [CallerArgumentExpression(nameof(setup))] string code = "")
        where TContext : new()
        => GivenCore(desc, code, Steps.SetupValueTask(setup));

    public GivenStage<TContext> Given<TContext>(Action<TContext> setup, [CallerArgumentExpression(nameof(setup))] string code = "")
        where TContext : new()
        => GivenCore(null, code, Steps.Setup(setup));
    [OverloadResolutionPriority(1)]
    public GivenStage<TContext> Given<TContext>(Func<TContext, Task> setup, [CallerArgumentExpression(nameof(setup))] string code = "")
        where TContext : new()
        => GivenCore(null, code, Steps.SetupTask(setup));
    public GivenStage<TContext> Given<TContext>(Func<TContext, ValueTask> setup, [CallerArgumentExpression(nameof(setup))] string code = "")
        where TContext : new()
        => GivenCore(null, code, Steps.SetupValueTask(setup));

    // ---- When

    public WhenStage<Unit, T> When<T>(string desc, Func<T> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => WhenCore(desc, code, Steps.Produce(fn));
    [OverloadResolutionPriority(1)]
    public WhenStage<Unit, T> When<T>(string desc, Func<Task<T>> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => WhenCore(desc, code, Steps.ProduceTask(fn));
    public WhenStage<Unit, T> When<T>(string desc, Func<ValueTask<T>> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => WhenCore(desc, code, fn);
    public WhenStage<Unit, Unit> When(string desc, Action fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => WhenCore(desc, code, Steps.ProduceUnit(fn));
    [OverloadResolutionPriority(1)]
    public WhenStage<Unit, Unit> When(string desc, Func<Task> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => WhenCore(desc, code, Steps.ProduceUnitTask(fn));
    public WhenStage<Unit, Unit> When(string desc, Func<ValueTask> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => WhenCore(desc, code, Steps.ProduceUnitValueTask(fn));

    public WhenStage<Unit, T> When<T>(Func<T> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => WhenCore(null, code, Steps.Produce(fn));
    [OverloadResolutionPriority(1)]
    public WhenStage<Unit, T> When<T>(Func<Task<T>> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => WhenCore(null, code, Steps.ProduceTask(fn));
    public WhenStage<Unit, T> When<T>(Func<ValueTask<T>> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => WhenCore(null, code, fn);
    public WhenStage<Unit, Unit> When(Action fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => WhenCore(null, code, Steps.ProduceUnit(fn));
    [OverloadResolutionPriority(1)]
    public WhenStage<Unit, Unit> When(Func<Task> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => WhenCore(null, code, Steps.ProduceUnitTask(fn));
    public WhenStage<Unit, Unit> When(Func<ValueTask> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => WhenCore(null, code, Steps.ProduceUnitValueTask(fn));

    // ---- Then

    /// <summary>Adds a description-only Then for subsequent assertions. The state is <see cref="Unit"/>.</summary>
    public ThenStage<Unit> Then(string desc) => ThenCore(desc, "", Steps.Noop<Unit>());

    public ThenStage<Unit> Then(string desc, Action fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => ThenCore(desc, code, Steps.CheckUnit(fn));
    [OverloadResolutionPriority(1)]
    public ThenStage<Unit> Then(string desc, Func<Task> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => ThenCore(desc, code, Steps.CheckUnitTask(fn));
    public ThenStage<Unit> Then(string desc, Func<ValueTask> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => ThenCore(desc, code, Steps.CheckUnitValueTask(fn));
    public ThenStage<Unit> Then<TIgnored>(string desc, Func<ValueTask<TIgnored>> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => ThenCore(desc, code, Steps.CheckUnitValueTaskOf(fn));

    public ThenStage<Unit> Then(Action fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => ThenCore(null, code, Steps.CheckUnit(fn));
    [OverloadResolutionPriority(1)]
    public ThenStage<Unit> Then(Func<Task> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => ThenCore(null, code, Steps.CheckUnitTask(fn));
    public ThenStage<Unit> Then(Func<ValueTask> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => ThenCore(null, code, Steps.CheckUnitValueTask(fn));
    public ThenStage<Unit> Then<TIgnored>(Func<ValueTask<TIgnored>> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => ThenCore(null, code, Steps.CheckUnitValueTaskOf(fn));

    private GivenStage<T> GivenCore<T>(string? desc, string code, Func<ValueTask<T>> fn)
    {
        var step = new StepInfo(StepKind.Given, desc, code);
        return new(_ctx, [step], rec => Steps.Run(rec, _ctx, step, fn));
    }

    private WhenStage<Unit, T> WhenCore<T>(string? desc, string code, Func<ValueTask<T>> fn)
    {
        var step = new StepInfo(StepKind.When, desc, code);
        return new(_ctx, [step], step, _ => new ValueTask<Unit>(default(Unit)), _ => fn());
    }

    private ThenStage<Unit> ThenCore(string? desc, string code, Func<Unit, ValueTask> fn)
    {
        var step = new StepInfo(StepKind.Then, desc, code);
        return new(_ctx, [step], _ => new ValueTask<Unit>(default(Unit)), [Steps.Then(_ctx, step, fn)]);
    }
}
