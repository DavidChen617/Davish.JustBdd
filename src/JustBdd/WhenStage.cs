using System.ComponentModel;
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

    // ---- ThenRaises: subscribe to the When input while When runs
    // attach/detach receive the When input; the object must exist before When. A When exception fails as usual.

    public ThenStage<Raised<TIn, TArgs>> ThenRaises<TArgs>(string desc, Action<TIn, EventHandler<TArgs>> attach, Action<TIn, EventHandler<TArgs>> detach, [CallerArgumentExpression(nameof(attach))] string code = "")
        => RaisesCore(desc, $"ThenRaises<{typeof(TArgs).Name}>({code})", Handler(attach, detach), typeof(TArgs).Name);
    public ThenStage<Raised<TIn, TArgs>> ThenRaises<TArgs>(Action<TIn, EventHandler<TArgs>> attach, Action<TIn, EventHandler<TArgs>> detach, [CallerArgumentExpression(nameof(attach))] string code = "")
        => RaisesCore(null, $"ThenRaises<{typeof(TArgs).Name}>({code})", Handler(attach, detach), typeof(TArgs).Name);
    public ThenStage<Raised<TIn, EventArgs>> ThenRaises(string desc, Action<TIn, EventHandler> attach, Action<TIn, EventHandler> detach, [CallerArgumentExpression(nameof(attach))] string code = "")
        => RaisesCore(desc, $"ThenRaises({code})", Handler(attach, detach), nameof(EventArgs));
    public ThenStage<Raised<TIn, EventArgs>> ThenRaises(Action<TIn, EventHandler> attach, Action<TIn, EventHandler> detach, [CallerArgumentExpression(nameof(attach))] string code = "")
        => RaisesCore(null, $"ThenRaises({code})", Handler(attach, detach), nameof(EventArgs));
    public ThenStage<Raised<TIn, TArgs>> ThenRaises<THandler, TArgs>(string desc, Func<Action<object?, TArgs>, THandler> handler, Action<TIn, THandler> attach, Action<TIn, THandler> detach, [CallerArgumentExpression(nameof(attach))] string code = "") where THandler : Delegate
        => RaisesCore(desc, $"ThenRaises<{typeof(THandler).Name}>({code})", Handler(handler, attach, detach), typeof(THandler).Name);
    public ThenStage<Raised<TIn, TArgs>> ThenRaises<THandler, TArgs>(Func<Action<object?, TArgs>, THandler> handler, Action<TIn, THandler> attach, Action<TIn, THandler> detach, [CallerArgumentExpression(nameof(attach))] string code = "") where THandler : Delegate
        => RaisesCore(null, $"ThenRaises<{typeof(THandler).Name}>({code})", Handler(handler, attach, detach), typeof(THandler).Name);
    public ThenStage<Raised<TIn, PropertyChangedEventArgs>> ThenPropertyChanged(string desc, Func<TIn, INotifyPropertyChanged> source, string propertyName, [CallerArgumentExpression(nameof(source))] string code = "")
        => RaisesCore(desc, $"ThenPropertyChanged({code}, \"{propertyName}\")", PropertyHandler(source, propertyName), $"PropertyChanged for '{propertyName}'");
    public ThenStage<Raised<TIn, PropertyChangedEventArgs>> ThenPropertyChanged(Func<TIn, INotifyPropertyChanged> source, string propertyName, [CallerArgumentExpression(nameof(source))] string code = "")
        => RaisesCore(null, $"ThenPropertyChanged({code}, \"{propertyName}\")", PropertyHandler(source, propertyName), $"PropertyChanged for '{propertyName}'");

    // ---- ThenNotRaises: When must not raise the event; the state stays the When result

    public ThenStage<TOut> ThenNotRaises<TArgs>(string desc, Action<TIn, EventHandler<TArgs>> attach, Action<TIn, EventHandler<TArgs>> detach, [CallerArgumentExpression(nameof(attach))] string code = "")
        => NotRaisesCore(desc, $"ThenNotRaises<{typeof(TArgs).Name}>({code})", Handler(attach, detach), typeof(TArgs).Name);
    public ThenStage<TOut> ThenNotRaises<TArgs>(Action<TIn, EventHandler<TArgs>> attach, Action<TIn, EventHandler<TArgs>> detach, [CallerArgumentExpression(nameof(attach))] string code = "")
        => NotRaisesCore(null, $"ThenNotRaises<{typeof(TArgs).Name}>({code})", Handler(attach, detach), typeof(TArgs).Name);
    public ThenStage<TOut> ThenNotRaises(string desc, Action<TIn, EventHandler> attach, Action<TIn, EventHandler> detach, [CallerArgumentExpression(nameof(attach))] string code = "")
        => NotRaisesCore(desc, $"ThenNotRaises({code})", Handler(attach, detach), nameof(EventArgs));
    public ThenStage<TOut> ThenNotRaises(Action<TIn, EventHandler> attach, Action<TIn, EventHandler> detach, [CallerArgumentExpression(nameof(attach))] string code = "")
        => NotRaisesCore(null, $"ThenNotRaises({code})", Handler(attach, detach), nameof(EventArgs));
    public ThenStage<TOut> ThenNotRaises<THandler, TArgs>(string desc, Func<Action<object?, TArgs>, THandler> handler, Action<TIn, THandler> attach, Action<TIn, THandler> detach, [CallerArgumentExpression(nameof(attach))] string code = "") where THandler : Delegate
        => NotRaisesCore(desc, $"ThenNotRaises<{typeof(THandler).Name}>({code})", Handler(handler, attach, detach), typeof(THandler).Name);
    public ThenStage<TOut> ThenNotRaises<THandler, TArgs>(Func<Action<object?, TArgs>, THandler> handler, Action<TIn, THandler> attach, Action<TIn, THandler> detach, [CallerArgumentExpression(nameof(attach))] string code = "") where THandler : Delegate
        => NotRaisesCore(null, $"ThenNotRaises<{typeof(THandler).Name}>({code})", Handler(handler, attach, detach), typeof(THandler).Name);
    public ThenStage<TOut> ThenPropertyNotChanged(string desc, Func<TIn, INotifyPropertyChanged> source, string propertyName, [CallerArgumentExpression(nameof(source))] string code = "")
        => NotRaisesCore(desc, $"ThenPropertyNotChanged({code}, \"{propertyName}\")", PropertyHandler(source, propertyName), $"PropertyChanged for '{propertyName}'");
    public ThenStage<TOut> ThenPropertyNotChanged(Func<TIn, INotifyPropertyChanged> source, string propertyName, [CallerArgumentExpression(nameof(source))] string code = "")
        => NotRaisesCore(null, $"ThenPropertyNotChanged({code}, \"{propertyName}\")", PropertyHandler(source, propertyName), $"PropertyChanged for '{propertyName}'");

    // Subscribes a recorder to the input and returns the unsubscribe action.
    private delegate Action Subscription<TArgs>(TIn input, Action<object?, TArgs> record);

    private static Subscription<TArgs> Handler<TArgs>(Action<TIn, EventHandler<TArgs>> attach, Action<TIn, EventHandler<TArgs>> detach)
        => (input, record) => { EventHandler<TArgs> h = (s, e) => record(s, e); attach(input, h); return () => detach(input, h); };

    private static Subscription<EventArgs> Handler(Action<TIn, EventHandler> attach, Action<TIn, EventHandler> detach)
        => (input, record) => { EventHandler h = (s, e) => record(s, e); attach(input, h); return () => detach(input, h); };

    private static Subscription<TArgs> Handler<THandler, TArgs>(Func<Action<object?, TArgs>, THandler> handler,
        Action<TIn, THandler> attach, Action<TIn, THandler> detach)
        => (input, record) => { var h = handler(record); attach(input, h); return () => detach(input, h); };

    private static Subscription<PropertyChangedEventArgs> PropertyHandler(Func<TIn, INotifyPropertyChanged> source, string propertyName)
        => (input, record) =>
        {
            var target = source(input);
            PropertyChangedEventHandler h = (s, e) => { if (e.PropertyName == propertyName) record(s, e); };
            target.PropertyChanged += h;
            return () => target.PropertyChanged -= h;
        };

    private ThenStage<Raised<TIn, TArgs>> RaisesCore<TArgs>(string? desc, string code, Subscription<TArgs> subscribe, string eventName)
    {
        var (ctx, when) = (_ctx, _when);
        var step = new StepInfo(StepKind.Then, desc, code);

        async ValueTask<Raised<TIn, TArgs>> Run(Recorder rec)
        {
            var (input, _, events) = await Observe(rec, subscribe);
            await Steps.Run(rec, ctx, step, () => events.Count == 0
                ? throw new ScenarioExpectationException(
                    $"Expected When '{when.Description ?? when.Code}' to raise {eventName}, but no event was raised.")
                : default(ValueTask));
            return new Raised<TIn, TArgs>(input, events);
        }

        return new(_ctx, [.. _steps, step], Run, []);
    }

    private ThenStage<TOut> NotRaisesCore<TArgs>(string? desc, string code, Subscription<TArgs> subscribe, string eventName)
    {
        var (ctx, when) = (_ctx, _when);
        var step = new StepInfo(StepKind.Then, desc, code);

        async ValueTask<TOut> Run(Recorder rec)
        {
            var (_, result, events) = await Observe(rec, subscribe);
            await Steps.Run(rec, ctx, step, () => events.Count > 0
                ? throw new ScenarioExpectationException(
                    $"Expected When '{when.Description ?? when.Code}' not to raise {eventName}, but it was raised {events.Count} time(s).")
                : default(ValueTask));
            return result;
        }

        return new(_ctx, [.. _steps, step], Run, []);
    }

    // Runs When as its own step with the subscription active, detaching even when When fails.
    private async ValueTask<(TIn Input, TOut Result, IReadOnlyList<RaisedEvent<TArgs>> Events)> Observe<TArgs>(Recorder rec, Subscription<TArgs> subscribe)
    {
        var input = await _prev(rec);
        var events = new List<RaisedEvent<TArgs>>();
        var result = await Steps.Run(rec, _ctx, _when, async () =>
        {
            // Events may arrive on other threads.
            var unsubscribe = subscribe(input, (sender, args) => { lock (events) events.Add(new RaisedEvent<TArgs>(sender, args)); });
            try { return await _fn(input); }
            finally { unsubscribe(); }
        });
        lock (events) return (input, result, events.ToArray());
    }
}
