using System.Runtime.CompilerServices;

namespace Davish.JustBdd;

/// <summary>
/// Lazy Given / When / Then execution. Await the chain to obtain a <see cref="ScenarioResult"/>.
/// <code>
/// await new Scenario("Rename a user")
///     .Given("A user exists", () => new User("David"))
///     .When("Rename the user", user => user.Rename("Davish"))
///     .Then("The name is updated", user => Assert.Equal("Davish", user.Name));
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

    public string Description => _ctx.Description;
    public string FilePath => _ctx.FilePath;
    public int LineNumber => _ctx.LineNumber;

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
