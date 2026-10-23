using System.Runtime.CompilerServices;

namespace Davish.JustBdd;

public sealed class ThenStage<T>
{
    private readonly ScenarioContext _ctx;
    private readonly StepInfo[] _steps;
    private readonly Func<Recorder, ValueTask<T>> _run;
    private readonly Func<Recorder, T, ValueTask>[] _thens;

    internal ThenStage(ScenarioContext ctx, StepInfo[] steps, Func<Recorder, ValueTask<T>> run, Func<Recorder, T, ValueTask>[] thens)
        => (_ctx, _steps, _run, _thens) = (ctx, steps, run, thens);

    // ---- Then

    /// <summary>Adds a description-only Then followed by assertions.</summary>
    public ThenStage<T> Then(string desc) => AddStep(StepKind.Then, desc, "", Steps.Noop<T>());

    public ThenStage<T> Then(string desc, Action<T> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => AddStep(StepKind.Then, desc, code, Steps.Check(fn));
    [OverloadResolutionPriority(1)]
    public ThenStage<T> Then(string desc, Func<T, Task> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => AddStep(StepKind.Then, desc, code, Steps.CheckTask(fn));
    public ThenStage<T> Then(string desc, Func<T, ValueTask> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => AddStep(StepKind.Then, desc, code, fn);
    public ThenStage<T> Then<TIgnored>(string desc, Func<T, ValueTask<TIgnored>> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => AddStep(StepKind.Then, desc, code, Steps.CheckValueTaskOf(fn));

    public ThenStage<T> Then(Action<T> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => AddStep(StepKind.Then, null, code, Steps.Check(fn));
    [OverloadResolutionPriority(1)]
    public ThenStage<T> Then(Func<T, Task> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => AddStep(StepKind.Then, null, code, Steps.CheckTask(fn));
    public ThenStage<T> Then(Func<T, ValueTask> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => AddStep(StepKind.Then, null, code, fn);
    public ThenStage<T> Then<TIgnored>(Func<T, ValueTask<TIgnored>> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => AddStep(StepKind.Then, null, code, Steps.CheckValueTaskOf(fn));

    // ---- Assert

    public ThenStage<T> Assert(Action<T> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => AddStep(StepKind.Assert, null, code, Steps.Check(fn));
    [OverloadResolutionPriority(1)]
    public ThenStage<T> Assert(Func<T, Task> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => AddStep(StepKind.Assert, null, code, Steps.CheckTask(fn));
    public ThenStage<T> Assert(Func<T, ValueTask> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => AddStep(StepKind.Assert, null, code, fn);
    public ThenStage<T> Assert<TIgnored>(Func<T, ValueTask<TIgnored>> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => AddStep(StepKind.Assert, null, code, Steps.CheckValueTaskOf(fn));

    /// <summary>
    /// Extension point for fluent assertions supplied by integration packages; records an Assert step.
    /// <code>
    /// public static ThenStage&lt;T&gt; True&lt;T&gt;(this ThenStage&lt;T&gt; s, Func&lt;T, bool&gt; actual,
    ///     [CallerArgumentExpression(nameof(actual))] string actualCode = "")
    ///     => s.AddAssertion(x => { Xunit.Assert.True(actual(x)); return default; }, null, $"True({actualCode})");
    /// </code>
    /// </summary>
    /// <param name="check">The assertion, which throws on failure.</param>
    /// <param name="desc">Optional description. When null, display <paramref name="code"/> instead.</param>
    /// <param name="code">The code fragment to display, such as <c>Equal("Davish", u => u.Name)</c>.</param>
    public ThenStage<T> AddAssertion(Func<T, ValueTask> check, string? desc, string code)
        => AddStep(StepKind.Assert, desc, code, check);

    // ---- When: start another segment. It receives the state seen by these Thens, including Thrown after ThenThrows.

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

    // The new segment's input runs every step so far, then passes the unchanged state on.
    private WhenStage<T, TOut> WhenCore<TOut>(string? desc, string code, Func<T, ValueTask<TOut>> fn)
    {
        var step = new StepInfo(StepKind.When, desc, code);
        var (run, thens) = (_run, _thens);

        async ValueTask<T> Prev(Recorder rec)
        {
            var state = await run(rec);
            foreach (var then in thens)
                await then(rec, state);
            return state;
        }

        return new(_ctx, [.. _steps, step], step, Prev, fn);
    }

    // Copy the array to keep branched chains independent and immutable.
    internal ThenStage<T> AddStep(StepKind kind, string? desc, string code, Func<T, ValueTask> fn)
    {
        var step = new StepInfo(kind, desc, code);
        return new(_ctx, [.. _steps, step], _run, [.. _thens, Steps.Then(_ctx, step, fn)]);
    }

    /// <summary>
    /// Runs the chain synchronously and returns its result; a failure throws the original exception.
    /// Use it in synchronous tests and await <see cref="RunAsync"/> in asynchronous tests.
    /// </summary>
    public ScenarioResult Run()
    {
        // Async steps must not resume on a caller's single-threaded context while this call blocks it.
        var context = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(null);
        try
        {
            var run = RunAsync();
            return run.IsCompletedSuccessfully ? run.Result : run.AsTask().GetAwaiter().GetResult();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(context);
        }
    }

    /// <summary>
    /// Every call executes the chain from Given again. Await each returned ValueTask only once.
    /// Failures preserve the exception and store results in <c>exception.Data[Scenario.ResultDataKey]</c>.
    /// Execution inside another scenario step adds the result to that step's <see cref="StepResult.Children"/>.
    /// Start execution (Run or RunAsync) inside the step; work started outside is not collected.
    /// </summary>
    public async ValueTask<ScenarioResult> RunAsync()
    {
        var parent = Nesting.Current;   // Capture the enclosing collector before executing any steps.
        var rec = new Recorder();
        ScenarioResult? result = null;
        var faulted = false;
        try
        {
            var state = await _run(rec);
            foreach (var then in _thens)
                await then(rec, state);
            return result = rec.ToResult(_ctx, _steps);
        }
        catch (Exception ex) when (Fault(ex)) { throw; }
        finally
        {
            // Publish success and failure: nested results go to the parent step; top-level results go to sinks.
            // A sink failure must not replace the original scenario exception.
            if (result is not null)
            {
                if (parent is not null) parent.Add(result);
                else ScenarioResults.Publish(result, throwOnSinkFailure: !faulted);
            }
        }

        bool Fault(Exception ex)
        {
            faulted = true;
            return Steps.AttachResult(ex, result = rec.ToResult(_ctx, _steps));
        }
    }
}
