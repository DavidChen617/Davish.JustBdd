using System.Runtime.CompilerServices;

namespace Davish.JustBdd;

/// <summary>
/// Parameterless When, Then, and And for stages whose state is <see cref="Unit"/>, such as after
/// <c>Given(() => { ... })</c> or a void When: <c>.Given(() => { }).When(() => { }).Then(() => { })</c>.
/// Stages with a real state keep only the overloads that receive it.
/// </summary>
public static class UnitStageExtensions
{
    // ---- GivenStage<Unit>

    public static WhenStage<Unit, TOut> When<TOut>(this GivenStage<Unit> stage, string desc, Func<TOut> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => stage.WhenCore(desc, code, Steps.Ignore(Steps.Produce(fn)));
    [OverloadResolutionPriority(1)]
    public static WhenStage<Unit, TOut> When<TOut>(this GivenStage<Unit> stage, string desc, Func<Task<TOut>> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => stage.WhenCore(desc, code, Steps.Ignore(Steps.ProduceTask(fn)));
    public static WhenStage<Unit, TOut> When<TOut>(this GivenStage<Unit> stage, string desc, Func<ValueTask<TOut>> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => stage.WhenCore(desc, code, Steps.Ignore(fn));
    public static WhenStage<Unit, Unit> When(this GivenStage<Unit> stage, string desc, Action fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => stage.WhenCore(desc, code, Steps.Ignore(Steps.ProduceUnit(fn)));
    [OverloadResolutionPriority(1)]
    public static WhenStage<Unit, Unit> When(this GivenStage<Unit> stage, string desc, Func<Task> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => stage.WhenCore(desc, code, Steps.Ignore(Steps.ProduceUnitTask(fn)));
    public static WhenStage<Unit, Unit> When(this GivenStage<Unit> stage, string desc, Func<ValueTask> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => stage.WhenCore(desc, code, Steps.Ignore(Steps.ProduceUnitValueTask(fn)));

    public static WhenStage<Unit, TOut> When<TOut>(this GivenStage<Unit> stage, Func<TOut> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => stage.WhenCore(null, code, Steps.Ignore(Steps.Produce(fn)));
    [OverloadResolutionPriority(1)]
    public static WhenStage<Unit, TOut> When<TOut>(this GivenStage<Unit> stage, Func<Task<TOut>> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => stage.WhenCore(null, code, Steps.Ignore(Steps.ProduceTask(fn)));
    public static WhenStage<Unit, TOut> When<TOut>(this GivenStage<Unit> stage, Func<ValueTask<TOut>> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => stage.WhenCore(null, code, Steps.Ignore(fn));
    public static WhenStage<Unit, Unit> When(this GivenStage<Unit> stage, Action fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => stage.WhenCore(null, code, Steps.Ignore(Steps.ProduceUnit(fn)));
    [OverloadResolutionPriority(1)]
    public static WhenStage<Unit, Unit> When(this GivenStage<Unit> stage, Func<Task> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => stage.WhenCore(null, code, Steps.Ignore(Steps.ProduceUnitTask(fn)));
    public static WhenStage<Unit, Unit> When(this GivenStage<Unit> stage, Func<ValueTask> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => stage.WhenCore(null, code, Steps.Ignore(Steps.ProduceUnitValueTask(fn)));

    public static ThenStage<Unit> Then(this GivenStage<Unit> stage, string desc, Action fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => stage.ThenCore(desc, code, Steps.CheckUnit(fn));
    [OverloadResolutionPriority(1)]
    public static ThenStage<Unit> Then(this GivenStage<Unit> stage, string desc, Func<Task> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => stage.ThenCore(desc, code, Steps.CheckUnitTask(fn));
    public static ThenStage<Unit> Then(this GivenStage<Unit> stage, string desc, Func<ValueTask> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => stage.ThenCore(desc, code, Steps.CheckUnitValueTask(fn));
    public static ThenStage<Unit> Then<TIgnored>(this GivenStage<Unit> stage, string desc, Func<ValueTask<TIgnored>> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => stage.ThenCore(desc, code, Steps.CheckUnitValueTaskOf(fn));

    public static ThenStage<Unit> Then(this GivenStage<Unit> stage, Action fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => stage.ThenCore(null, code, Steps.CheckUnit(fn));
    [OverloadResolutionPriority(1)]
    public static ThenStage<Unit> Then(this GivenStage<Unit> stage, Func<Task> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => stage.ThenCore(null, code, Steps.CheckUnitTask(fn));
    public static ThenStage<Unit> Then(this GivenStage<Unit> stage, Func<ValueTask> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => stage.ThenCore(null, code, Steps.CheckUnitValueTask(fn));
    public static ThenStage<Unit> Then<TIgnored>(this GivenStage<Unit> stage, Func<ValueTask<TIgnored>> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => stage.ThenCore(null, code, Steps.CheckUnitValueTaskOf(fn));

    // ---- WhenStage<TIn, Unit>

    public static ThenStage<Unit> Then<TIn>(this WhenStage<TIn, Unit> stage, string desc, Action fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => stage.ThenCore(desc, code, Steps.CheckUnit(fn));
    [OverloadResolutionPriority(1)]
    public static ThenStage<Unit> Then<TIn>(this WhenStage<TIn, Unit> stage, string desc, Func<Task> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => stage.ThenCore(desc, code, Steps.CheckUnitTask(fn));
    public static ThenStage<Unit> Then<TIn>(this WhenStage<TIn, Unit> stage, string desc, Func<ValueTask> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => stage.ThenCore(desc, code, Steps.CheckUnitValueTask(fn));
    public static ThenStage<Unit> Then<TIn, TIgnored>(this WhenStage<TIn, Unit> stage, string desc, Func<ValueTask<TIgnored>> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => stage.ThenCore(desc, code, Steps.CheckUnitValueTaskOf(fn));

    public static ThenStage<Unit> Then<TIn>(this WhenStage<TIn, Unit> stage, Action fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => stage.ThenCore(null, code, Steps.CheckUnit(fn));
    [OverloadResolutionPriority(1)]
    public static ThenStage<Unit> Then<TIn>(this WhenStage<TIn, Unit> stage, Func<Task> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => stage.ThenCore(null, code, Steps.CheckUnitTask(fn));
    public static ThenStage<Unit> Then<TIn>(this WhenStage<TIn, Unit> stage, Func<ValueTask> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => stage.ThenCore(null, code, Steps.CheckUnitValueTask(fn));
    public static ThenStage<Unit> Then<TIn, TIgnored>(this WhenStage<TIn, Unit> stage, Func<ValueTask<TIgnored>> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => stage.ThenCore(null, code, Steps.CheckUnitValueTaskOf(fn));

    // ---- ThenStage<Unit>

    public static ThenStage<Unit> Then(this ThenStage<Unit> stage, string desc, Action fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => stage.AddStep(StepKind.Then, desc, code, Steps.CheckUnit(fn));
    [OverloadResolutionPriority(1)]
    public static ThenStage<Unit> Then(this ThenStage<Unit> stage, string desc, Func<Task> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => stage.AddStep(StepKind.Then, desc, code, Steps.CheckUnitTask(fn));
    public static ThenStage<Unit> Then(this ThenStage<Unit> stage, string desc, Func<ValueTask> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => stage.AddStep(StepKind.Then, desc, code, Steps.CheckUnitValueTask(fn));
    public static ThenStage<Unit> Then<TIgnored>(this ThenStage<Unit> stage, string desc, Func<ValueTask<TIgnored>> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => stage.AddStep(StepKind.Then, desc, code, Steps.CheckUnitValueTaskOf(fn));

    public static ThenStage<Unit> Then(this ThenStage<Unit> stage, Action fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => stage.AddStep(StepKind.Then, null, code, Steps.CheckUnit(fn));
    [OverloadResolutionPriority(1)]
    public static ThenStage<Unit> Then(this ThenStage<Unit> stage, Func<Task> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => stage.AddStep(StepKind.Then, null, code, Steps.CheckUnitTask(fn));
    public static ThenStage<Unit> Then(this ThenStage<Unit> stage, Func<ValueTask> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => stage.AddStep(StepKind.Then, null, code, Steps.CheckUnitValueTask(fn));
    public static ThenStage<Unit> Then<TIgnored>(this ThenStage<Unit> stage, Func<ValueTask<TIgnored>> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => stage.AddStep(StepKind.Then, null, code, Steps.CheckUnitValueTaskOf(fn));

    public static ThenStage<Unit> And(this ThenStage<Unit> stage, string desc, Action fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => stage.AddStep(StepKind.And, desc, code, Steps.CheckUnit(fn));
    [OverloadResolutionPriority(1)]
    public static ThenStage<Unit> And(this ThenStage<Unit> stage, string desc, Func<Task> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => stage.AddStep(StepKind.And, desc, code, Steps.CheckUnitTask(fn));
    public static ThenStage<Unit> And(this ThenStage<Unit> stage, string desc, Func<ValueTask> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => stage.AddStep(StepKind.And, desc, code, Steps.CheckUnitValueTask(fn));
    public static ThenStage<Unit> And<TIgnored>(this ThenStage<Unit> stage, string desc, Func<ValueTask<TIgnored>> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => stage.AddStep(StepKind.And, desc, code, Steps.CheckUnitValueTaskOf(fn));

    public static ThenStage<Unit> And(this ThenStage<Unit> stage, Action fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => stage.AddStep(StepKind.And, null, code, Steps.CheckUnit(fn));
    [OverloadResolutionPriority(1)]
    public static ThenStage<Unit> And(this ThenStage<Unit> stage, Func<Task> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => stage.AddStep(StepKind.And, null, code, Steps.CheckUnitTask(fn));
    public static ThenStage<Unit> And(this ThenStage<Unit> stage, Func<ValueTask> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => stage.AddStep(StepKind.And, null, code, Steps.CheckUnitValueTask(fn));
    public static ThenStage<Unit> And<TIgnored>(this ThenStage<Unit> stage, Func<ValueTask<TIgnored>> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => stage.AddStep(StepKind.And, null, code, Steps.CheckUnitValueTaskOf(fn));

    public static WhenStage<Unit, TOut> When<TOut>(this ThenStage<Unit> stage, string desc, Func<TOut> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => stage.WhenCore(desc, code, Steps.Ignore(Steps.Produce(fn)));
    [OverloadResolutionPriority(1)]
    public static WhenStage<Unit, TOut> When<TOut>(this ThenStage<Unit> stage, string desc, Func<Task<TOut>> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => stage.WhenCore(desc, code, Steps.Ignore(Steps.ProduceTask(fn)));
    public static WhenStage<Unit, TOut> When<TOut>(this ThenStage<Unit> stage, string desc, Func<ValueTask<TOut>> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => stage.WhenCore(desc, code, Steps.Ignore(fn));
    public static WhenStage<Unit, Unit> When(this ThenStage<Unit> stage, string desc, Action fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => stage.WhenCore(desc, code, Steps.Ignore(Steps.ProduceUnit(fn)));
    [OverloadResolutionPriority(1)]
    public static WhenStage<Unit, Unit> When(this ThenStage<Unit> stage, string desc, Func<Task> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => stage.WhenCore(desc, code, Steps.Ignore(Steps.ProduceUnitTask(fn)));
    public static WhenStage<Unit, Unit> When(this ThenStage<Unit> stage, string desc, Func<ValueTask> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => stage.WhenCore(desc, code, Steps.Ignore(Steps.ProduceUnitValueTask(fn)));

    public static WhenStage<Unit, TOut> When<TOut>(this ThenStage<Unit> stage, Func<TOut> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => stage.WhenCore(null, code, Steps.Ignore(Steps.Produce(fn)));
    [OverloadResolutionPriority(1)]
    public static WhenStage<Unit, TOut> When<TOut>(this ThenStage<Unit> stage, Func<Task<TOut>> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => stage.WhenCore(null, code, Steps.Ignore(Steps.ProduceTask(fn)));
    public static WhenStage<Unit, TOut> When<TOut>(this ThenStage<Unit> stage, Func<ValueTask<TOut>> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => stage.WhenCore(null, code, Steps.Ignore(fn));
    public static WhenStage<Unit, Unit> When(this ThenStage<Unit> stage, Action fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => stage.WhenCore(null, code, Steps.Ignore(Steps.ProduceUnit(fn)));
    [OverloadResolutionPriority(1)]
    public static WhenStage<Unit, Unit> When(this ThenStage<Unit> stage, Func<Task> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => stage.WhenCore(null, code, Steps.Ignore(Steps.ProduceUnitTask(fn)));
    public static WhenStage<Unit, Unit> When(this ThenStage<Unit> stage, Func<ValueTask> fn, [CallerArgumentExpression(nameof(fn))] string code = "")
        => stage.WhenCore(null, code, Steps.Ignore(Steps.ProduceUnitValueTask(fn)));
}
