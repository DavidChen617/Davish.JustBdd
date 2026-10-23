using System.Runtime.CompilerServices;

namespace Davish.JustBdd;

/// <summary>
/// Stage following When. <typeparamref name="TIn"/> is its input and <typeparamref name="TOut"/> its result.
/// Then receives TOut; ThenThrows receives <see cref="Thrown{TState, TEx}"/> with the original TIn state.
/// </summary>
public sealed class WhenStage<TIn, TOut>
{
    private readonly ScenarioContext _ctx;
    private readonly StepInfo[] _steps;
    private readonly StepInfo _when;
    private readonly Func<Recorder, ValueTask<TIn>> _prev;
    private readonly Func<TIn, ValueTask<TOut>> _fn;

    internal WhenStage(ScenarioContext ctx, StepInfo[] steps, StepInfo when, Func<Recorder, ValueTask<TIn>> prev, Func<TIn, ValueTask<TOut>> fn)
        => (_ctx, _steps, _when, _prev, _fn) = (ctx, steps, when, prev, fn);

    // ---- Then

    /// <summary>Adds a description-only Then followed by assertions.</summary>
    public ThenStage<TOut> Then(string desc) => ThenCore(desc, "", Steps.Noop<TOut>());

    public ThenStage<TOut> Then(string desc, Action<TOut> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => ThenCore(desc, code, Steps.Check(fn));
    [OverloadResolutionPriority(1)]
    public ThenStage<TOut> Then(string desc, Func<TOut, Task> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => ThenCore(desc, code, Steps.CheckTask(fn));
    public ThenStage<TOut> Then(string desc, Func<TOut, ValueTask> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => ThenCore(desc, code, fn);
    public ThenStage<TOut> Then<TIgnored>(string desc, Func<TOut, ValueTask<TIgnored>> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => ThenCore(desc, code, Steps.CheckValueTaskOf(fn));

    public ThenStage<TOut> Then(Action<TOut> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => ThenCore(null, code, Steps.Check(fn));
    [OverloadResolutionPriority(1)]
    public ThenStage<TOut> Then(Func<TOut, Task> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => ThenCore(null, code, Steps.CheckTask(fn));
    public ThenStage<TOut> Then(Func<TOut, ValueTask> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => ThenCore(null, code, fn);
    public ThenStage<TOut> Then<TIgnored>(Func<TOut, ValueTask<TIgnored>> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => ThenCore(null, code, Steps.CheckValueTaskOf(fn));

    internal ThenStage<TOut> ThenCore(string? desc, string code, Func<TOut, ValueTask> fn)
    {
        var step = new StepInfo(StepKind.Then, desc, code);
        return new(_ctx, [.. _steps, step], Steps.Chain(_ctx, _when, _prev, _fn), [Steps.Then(_ctx, step, fn)]);
    }

    // ---- ThenThrows: expect TEx or a derived exception
    // No check-delegate overload: explicitly specifying TEx prevents inference of an additional TIgnored.
    // Otherwise Action could discard a ValueTask<T>. Put checks in a subsequent Then or Assert instead.

    public ThenStage<Thrown<TIn, TEx>> ThenThrows<TEx>(string desc) where TEx : Exception
        => ThrowsCore<TEx>(desc);
    public ThenStage<Thrown<TIn, TEx>> ThenThrows<TEx>() where TEx : Exception
        => ThrowsCore<TEx>(null);

    /// <summary>Equivalent to <c>ThenThrows&lt;Exception&gt;</c>.</summary>
    public ThenStage<Thrown<TIn, Exception>> ThenThrows(string desc) => ThenThrows<Exception>(desc);
    /// <summary>Equivalent to <c>ThenThrows&lt;Exception&gt;</c>.</summary>
    public ThenStage<Thrown<TIn, Exception>> ThenThrows() => ThenThrows<Exception>();

    // Execute ThenThrows within run; a missing exception fails this step without special handling in ThenStage.
    private ThenStage<Thrown<TIn, TEx>> ThrowsCore<TEx>(string? desc)
        where TEx : Exception
    {
        var (ctx, when, prev, whenFn) = (_ctx, _when, _prev, _fn);
        var step = new StepInfo(StepKind.Then, desc, $"ThenThrows<{typeof(TEx).Name}>()");

        async ValueTask<Thrown<TIn, TEx>> Run(Recorder rec)
        {
            var input = await prev(rec);
            var ex = await Steps.RunExpecting<TOut, TEx>(rec, ctx, when, () => whenFn(input));
            var thrown = ex is null ? null : new Thrown<TIn, TEx>(input, ex);

            await Steps.Run(rec, ctx, step, () => thrown is null
                ? throw new ScenarioExpectationException(
                    $"Expected When '{when.Description ?? when.Code}' to throw {typeof(TEx).Name}, but it completed without an exception.")
                : default(ValueTask));

            return thrown!;
        }

        return new(_ctx, [.. _steps, step], Run, []);
    }
}
