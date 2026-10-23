using System.Runtime.CompilerServices;

namespace Davish.JustBdd;

public sealed class GivenStage<T>
{
    private readonly ScenarioContext _ctx;
    private readonly StepInfo[] _steps;
    private readonly Func<Recorder, ValueTask<T>> _run;

    internal GivenStage(ScenarioContext ctx, StepInfo[] steps, Func<Recorder, ValueTask<T>> run)
        => (_ctx, _steps, _run) = (ctx, steps, run);

    // ---- When

    public WhenStage<T, TOut> When<TOut>(string desc, Func<T, TOut> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => WhenCore(desc, code, Steps.Map(fn));
    [OverloadResolutionPriority(1)]
    public WhenStage<T, TOut> When<TOut>(string desc, Func<T, Task<TOut>> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => WhenCore(desc, code, Steps.MapTask(fn));
    public WhenStage<T, TOut> When<TOut>(string desc, Func<T, ValueTask<TOut>> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => WhenCore(desc, code, fn);
    public WhenStage<T, T> When(string desc, Action<T> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => WhenCore(desc, code, Steps.Pass(fn));
    [OverloadResolutionPriority(1)]
    public WhenStage<T, T> When(string desc, Func<T, Task> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => WhenCore(desc, code, Steps.PassTask(fn));
    public WhenStage<T, T> When(string desc, Func<T, ValueTask> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => WhenCore(desc, code, Steps.PassValueTask(fn));

    public WhenStage<T, TOut> When<TOut>(Func<T, TOut> fn, [CallerArgumentExpression(nameof(fn))] string code = "")

        => WhenCore(null, code, Steps.Map(fn));
    [OverloadResolutionPriority(1)]
    public WhenStage<T, TOut> When<TOut>(Func<T, Task<TOut>> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => WhenCore(null, code, Steps.MapTask(fn));
    public WhenStage<T, TOut> When<TOut>(Func<T, ValueTask<TOut>> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => WhenCore(null, code, fn);
    public WhenStage<T, T> When(Action<T> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => WhenCore(null, code, Steps.Pass(fn));
    [OverloadResolutionPriority(1)]
    public WhenStage<T, T> When(Func<T, Task> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => WhenCore(null, code, Steps.PassTask(fn));
    public WhenStage<T, T> When(Func<T, ValueTask> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => WhenCore(null, code, Steps.PassValueTask(fn));

    // ---- Then

    /// <summary>Adds a description-only Then followed by assertions.</summary>
    public ThenStage<T> Then(string desc) => ThenCore(desc, "", Steps.Noop<T>());

    public ThenStage<T> Then(string desc, Action<T> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => ThenCore(desc, code, Steps.Check(fn));
    [OverloadResolutionPriority(1)]
    public ThenStage<T> Then(string desc, Func<T, Task> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => ThenCore(desc, code, Steps.CheckTask(fn));
    public ThenStage<T> Then(string desc, Func<T, ValueTask> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => ThenCore(desc, code, fn);
    public ThenStage<T> Then<TIgnored>(string desc, Func<T, ValueTask<TIgnored>> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => ThenCore(desc, code, Steps.CheckValueTaskOf(fn));

    public ThenStage<T> Then(Action<T> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => ThenCore(null, code, Steps.Check(fn));
    [OverloadResolutionPriority(1)]
    public ThenStage<T> Then(Func<T, Task> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => ThenCore(null, code, Steps.CheckTask(fn));
    public ThenStage<T> Then(Func<T, ValueTask> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => ThenCore(null, code, fn);
    public ThenStage<T> Then<TIgnored>(Func<T, ValueTask<TIgnored>> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => ThenCore(null, code, Steps.CheckValueTaskOf(fn));

    internal WhenStage<T, TOut> WhenCore<TOut>(string? desc, string code, Func<T, ValueTask<TOut>> fn)
    {
        var step = new StepInfo(StepKind.When, desc, code);
        return new(_ctx, [.. _steps, step], step, _run, fn);
    }

    internal ThenStage<T> ThenCore(string? desc, string code, Func<T, ValueTask> fn)
    {
        var step = new StepInfo(StepKind.Then, desc, code);
        return new(_ctx, [.. _steps, step], _run, [Steps.Then(_ctx, step, fn)]);
    }
}
