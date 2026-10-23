using System.Diagnostics;

namespace Davish.JustBdd;

internal static class Steps
{
    public const string StepKey = "Davish.JustBdd.Step";
    public const string ResultKey = "Davish.JustBdd.Result";

    public static async ValueTask<T> Run<T>(Recorder rec, ScenarioContext ctx, StepInfo step, Func<ValueTask<T>> fn)
    {
        var sw = Stopwatch.StartNew();
        var children = Nesting.Begin();
        try
        {
            var value = await fn();
            rec.Add(step, StepStatus.Passed, sw.Elapsed, null, children.Snapshot());
            return value;
        }
        catch (Exception ex) when (Fail(rec, ctx, step, sw.Elapsed, ex, children)) { throw; }
        finally { Nesting.End(children); }
    }

    public static async ValueTask Run(Recorder rec, ScenarioContext ctx, StepInfo step, Func<ValueTask> fn)
    {
        var sw = Stopwatch.StartNew();
        var children = Nesting.Begin();
        try
        {
            await fn();
            rec.Add(step, StepStatus.Passed, sw.Elapsed, null, children.Snapshot());
        }
        catch (Exception ex) when (Fail(rec, ctx, step, sw.Elapsed, ex, children)) { throw; }
        finally { Nesting.End(children); }
    }

    /// <summary>
    /// Records an expected <typeparamref name="TEx"/> as ThrewAsExpected and returns it, retaining it in Error.
    /// Normal completion records Passed and returns null; other exceptions record Failed and propagate.
    /// </summary>
    public static async ValueTask<TEx?> RunExpecting<TOut, TEx>(Recorder rec, ScenarioContext ctx, StepInfo step, Func<ValueTask<TOut>> fn)
        where TEx : Exception
    {
        var sw = Stopwatch.StartNew();
        var children = Nesting.Begin();
        try
        {
            await fn();
            rec.Add(step, StepStatus.Passed, sw.Elapsed, null, children.Snapshot());
            return null;
        }
        catch (TEx ex)
        {
            rec.Add(step, StepStatus.ThrewAsExpected, sw.Elapsed, ex, children.Snapshot());
            return ex;
        }
        catch (Exception ex) when (Fail(rec, ctx, step, sw.Elapsed, ex, children)) { throw; }
        finally { Nesting.End(children); }
    }

    // Always return false: preserve the original exception and stack trace.
    // Filters run before finally; nested scenarios have already added their results in their own finally blocks.
    private static bool Fail(Recorder rec, ScenarioContext ctx, StepInfo step, TimeSpan elapsed, Exception ex, ChildCollector children)
    {
        rec.Add(step, StepStatus.Failed, elapsed, ex, children.Snapshot());
        if (!ex.Data.IsReadOnly && !ex.Data.Contains(StepKey))
            ex.Data[StepKey] = $"Scenario '{ctx.Description}' > {step.Kind} '{step.Description ?? step.Code}'";
        return false;
    }

    // Each enclosing RunAsync replaces the result, leaving the outermost scenario result on the exception.
    // Keep the innermost step path, which identifies the actual failure.
    public static bool AttachResult(Exception ex, ScenarioResult result)
    {
        if (!ex.Data.IsReadOnly)
            ex.Data[ResultKey] = result;
        return false;
    }

    // Wrap only this step; the preceding step has already recorded its own result.
    public static Func<Recorder, ValueTask<TOut>> Chain<TIn, TOut>(
        ScenarioContext ctx, StepInfo step, Func<Recorder, ValueTask<TIn>> prev, Func<TIn, ValueTask<TOut>> fn)
        => async rec =>
        {
            var input = await prev(rec);
            return await Run(rec, ctx, step, () => fn(input));
        };

    public static Func<Recorder, T, ValueTask> Then<T>(ScenarioContext ctx, StepInfo step, Func<T, ValueTask> fn)
        => (rec, state) => Run(rec, ctx, step, () => fn(state));

    // Normalize sync and Task delegates to ValueTask. Distinct names avoid delegate covariance ambiguity.

    public static Func<ValueTask<T>> Produce<T>(Func<T> fn) => () => new ValueTask<T>(fn());
    public static Func<ValueTask<T>> ProduceTask<T>(Func<Task<T>> fn) => () => new ValueTask<T>(fn());
    public static Func<ValueTask<Unit>> ProduceUnit(Action fn) => () => { fn(); return default; };
    public static Func<ValueTask<Unit>> ProduceUnitTask(Func<Task> fn) => async () => { await fn(); return default; };
    public static Func<ValueTask<Unit>> ProduceUnitValueTask(Func<ValueTask> fn) => async () => { await fn(); return default; };

    // A context setup fills a new instance, which becomes the state.
    public static Func<ValueTask<T>> Setup<T>(Action<T> fn) where T : new() => () => { var x = new T(); fn(x); return new ValueTask<T>(x); };
    public static Func<ValueTask<T>> SetupTask<T>(Func<T, Task> fn) where T : new() => async () => { var x = new T(); await fn(x); return x; };
    public static Func<ValueTask<T>> SetupValueTask<T>(Func<T, ValueTask> fn) where T : new() => async () => { var x = new T(); await fn(x); return x; };

    public static Func<TIn, ValueTask<TOut>> Map<TIn, TOut>(Func<TIn, TOut> fn) => x => new ValueTask<TOut>(fn(x));
    public static Func<TIn, ValueTask<TOut>> MapTask<TIn, TOut>(Func<TIn, Task<TOut>> fn) => x => new ValueTask<TOut>(fn(x));

    // A void When passes its input state through unchanged.
    public static Func<T, ValueTask<T>> Pass<T>(Action<T> fn) => x => { fn(x); return new ValueTask<T>(x); };
    public static Func<T, ValueTask<T>> PassTask<T>(Func<T, Task> fn) => async x => { await fn(x); return x; };
    public static Func<T, ValueTask<T>> PassValueTask<T>(Func<T, ValueTask> fn) => async x => { await fn(x); return x; };

    public static Func<T, ValueTask> Check<T>(Action<T> fn) => x => { fn(x); return default; };
    public static Func<T, ValueTask> CheckTask<T>(Func<T, Task> fn) => x => new ValueTask(fn(x));
    public static Func<Unit, ValueTask> CheckUnit(Action fn) => _ => { fn(); return default; };
    public static Func<Unit, ValueTask> CheckUnitTask(Func<Task> fn) => _ => new ValueTask(fn());
    public static Func<Unit, ValueTask> CheckUnitValueTask(Func<ValueTask> fn) => _ => fn();

    // ValueTask<T> cannot convert to ValueTask; await it explicitly instead of discarding the result.
    public static Func<T, ValueTask> CheckValueTaskOf<T, TIgnored>(Func<T, ValueTask<TIgnored>> fn) => async x => { await fn(x); };
    public static Func<Unit, ValueTask> CheckUnitValueTaskOf<TIgnored>(Func<ValueTask<TIgnored>> fn) => async _ => { await fn(); };

    // A description-only Then performs no work and always passes.
    public static Func<T, ValueTask> Noop<T>() => _ => default;
}
