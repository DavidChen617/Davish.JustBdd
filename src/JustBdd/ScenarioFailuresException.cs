namespace Davish.JustBdd;

/// <summary>
/// Raised by a scenario with <c>ContinueOnFailure()</c> when several checks fail. <see cref="AggregateException.InnerExceptions"/>
/// holds the original exceptions in step order; each carries its own step path in <c>Data[Scenario.StepDataKey]</c>.
/// A single failure throws the original exception instead.
/// </summary>
public sealed class ScenarioFailuresException(string scenario, IReadOnlyList<Exception> failures)
    : AggregateException(failures)
{
    private readonly string _message = BuildMessage(scenario, failures);

    // AggregateException.Message appends every inner message; this message already lists them by step.
    public override string Message => _message;

    private static string BuildMessage(string scenario, IReadOnlyList<Exception> failures)
        => $"{failures.Count} checks failed in scenario '{scenario}':" + string.Concat(
            failures.Select(f => Environment.NewLine + "- " + (f.Data[Steps.StepKey] as string ?? f.GetType().Name) + ": " + f.Message));
}
