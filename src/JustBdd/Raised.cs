namespace Davish.JustBdd;

/// <summary>One event captured while When ran.</summary>
public sealed record RaisedEvent<TArgs>(object? Sender, TArgs Args);

/// <summary>
/// State following <c>ThenRaises</c> or <c>ThenPropertyChanged</c>.
/// </summary>
/// <param name="State">The input to When, whose events were observed.</param>
/// <param name="Events">Every captured event, in the order raised; never empty.</param>
public sealed record Raised<TState, TArgs>(TState State, IReadOnlyList<RaisedEvent<TArgs>> Events)
{
    /// <summary>The sender of the first event.</summary>
    public object? Sender => Events[0].Sender;

    /// <summary>The arguments of the first event.</summary>
    public TArgs Args => Events[0].Args;
}
