namespace Davish.JustBdd;

/// <summary>
/// State following <c>ThenThrows</c>.
/// </summary>
/// <param name="State">The input to When, which threw instead of returning a value.</param>
/// <param name="Exception">The expected exception thrown by When.</param>
public sealed record Thrown<TState, TEx>(TState State, TEx Exception) where TEx : Exception;

/// <summary>Raised when <c>ThenThrows</c> expects an exception but When completes normally.</summary>
public sealed class ScenarioExpectationException(string message) : Exception(message);
