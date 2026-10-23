namespace Davish.JustBdd;

/// <summary>
/// Wraps <c>Xunit.Assert</c> as extension methods on <see cref="ThenStage{T}"/>.
/// <list type="bullet">
/// <item>Each assertion has overloads with and without a description; omitted descriptions display code.</item>
/// <item>CallerArgumentExpression captures the call, such as <c>Equal("Davish", u => u.Name)</c>, excluding comparers.</item>
/// <item>Assertions check values without transforming state. Use <c>ThenStage.Assert(lambda)</c> when a return value is needed.</item>
/// </list>
/// </summary>
public static partial class XunitAssertions
{
    private static ThenStage<T> Check<T>(ThenStage<T> a, string? desc, string code, Action<T> check)
        => a.AddAssertion(x => { check(x); return default; }, desc, code);

    private static ThenStage<T> CheckAsync<T>(ThenStage<T> a, string? desc, string code, Func<T, Task> check)
        => a.AddAssertion(x => new ValueTask(check(x)), desc, code);

    private static string Code(string name, params string[] args) => $"{name}({string.Join(", ", args)})";
}
