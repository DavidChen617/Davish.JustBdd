using System.ComponentModel;

namespace Davish.JustBdd.Tests;

/// <summary>ThenRaises subscribes to the When input while When runs; ThenNotRaises expects no event.</summary>
public class RaisesTests
{
    private sealed class ChangedEventArgs(int value) : EventArgs
    {
        public int Value { get; } = value;
    }

    private sealed class Counter : INotifyPropertyChanged
    {
        public int Value { get; private set; }

        public event EventHandler<ChangedEventArgs>? Changed;
        public event EventHandler? Reset;
        public event Action<int>? Ticked;
        public event PropertyChangedEventHandler? PropertyChanged;

        public bool HasChangedHandlers => Changed is not null;

        public void Add(int n)
        {
            for (var i = 0; i < n; i++)
            {
                Value++;
                Changed?.Invoke(this, new ChangedEventArgs(Value));
                Ticked?.Invoke(Value);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
            }
        }

        public void Clear()
        {
            Value = 0;
            Reset?.Invoke(this, EventArgs.Empty);
        }

        public void Rename() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Name"));

        public async Task AddLaterAsync()
        {
            await Task.Yield();
            Add(1);
        }
    }

    // ---- ThenRaises

    [Fact]
    public void Generic_event_is_captured_with_the_When_input_as_state()
    {
        Counter? counter = null;
        Raised<Counter, ChangedEventArgs>? raised = null;

        var result = new Scenario("S")
            .Given(() => counter = new Counter())
            .When(c => c.Add(2))
            .ThenRaises<ChangedEventArgs>("Changed is raised", (c, h) => c.Changed += h, (c, h) => c.Changed -= h)
            .Then(r => raised = r)
            .Run();

        Assert.NotNull(raised);
        Assert.Same(counter, raised.State);
        Assert.Equal([1, 2], raised.Events.Select(e => e.Args.Value));
        Assert.Same(counter, raised.Sender);
        Assert.Equal(1, raised.Args.Value);
        Assert.Equal(
            (StepKind.Then, "Changed is raised", "ThenRaises<ChangedEventArgs>((c, h) => c.Changed += h)"),
            (result.Steps[2].Step.Kind, result.Steps[2].Step.Description, result.Steps[2].Step.Code));
        Assert.False(counter!.HasChangedHandlers);   // Detached after When.
    }

    [Fact]
    public void Missing_event_fails_the_ThenRaises_step()
    {
        var ex = Assert.Throws<ScenarioExpectationException>(() =>
            new Scenario("S")
                .Given(() => new Counter())
                .When(c => c.Clear())
                .ThenRaises<ChangedEventArgs>("Changed is raised", (c, h) => c.Changed += h, (c, h) => c.Changed -= h)
                .Run());

        Assert.Equal("Scenario 'S' > Then 'Changed is raised'", ex.Data[Scenario.StepDataKey]);
        var result = Assert.IsType<ScenarioResult>(ex.Data[Scenario.ResultDataKey]);
        Assert.Equal([StepStatus.Passed, StepStatus.Passed, StepStatus.Failed], result.Steps.Select(s => s.Status));
    }

    [Fact]
    public void When_failure_keeps_the_original_exception_and_detaches()
    {
        var counter = new Counter();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            new Scenario("S")
                .Given(() => counter)
                .When("Breaks", c => throw new InvalidOperationException())
                .ThenRaises<ChangedEventArgs>((c, h) => c.Changed += h, (c, h) => c.Changed -= h)
                .Run());

        Assert.Equal("Scenario 'S' > When 'Breaks'", ex.Data[Scenario.StepDataKey]);
        Assert.False(counter.HasChangedHandlers);
    }

    [Fact]
    public void Non_generic_event_handler()
    {
        Raised<Counter, EventArgs>? raised = null;

        new Scenario()
            .Given(() => new Counter())
            .When(c => c.Clear())
            .ThenRaises((c, h) => c.Reset += h, (c, h) => c.Reset -= h)
            .Then(r => raised = r)
            .Run();

        Assert.Same(EventArgs.Empty, Assert.Single(raised!.Events).Args);
    }

    [Fact]
    public void Custom_delegate_through_a_handler_adapter()
    {
        Raised<Counter, int>? raised = null;

        new Scenario()
            .Given(() => new Counter())
            .When(c => c.Add(3))
            .ThenRaises<Action<int>, int>("Ticked", record => n => record(null, n), (c, h) => c.Ticked += h, (c, h) => c.Ticked -= h)
            .Then(r => raised = r)
            .Run();

        Assert.Equal([1, 2, 3], raised!.Events.Select(e => e.Args));
        Assert.Null(raised.Sender);
    }

    [Fact]
    public void Property_changed_counts_only_the_named_property()
    {
        Raised<Counter, PropertyChangedEventArgs>? raised = null;

        new Scenario()
            .Given(() => new Counter())
            .When(c => { c.Rename(); c.Add(1); })
            .ThenPropertyChanged("Value changes", c => c, nameof(Counter.Value))
            .Then(r => raised = r)
            .Run();

        Assert.Equal("Value", Assert.Single(raised!.Events).Args.PropertyName);

        Assert.Throws<ScenarioExpectationException>(() =>
            new Scenario().Given(() => new Counter()).When(c => c.Rename()).ThenPropertyChanged(c => c, nameof(Counter.Value)).Run());
    }

    [Fact]
    public async Task Events_raised_by_an_async_When_are_captured()
    {
        var result = await new Scenario()
            .Given(() => new Counter())
            .When(async c => await c.AddLaterAsync())
            .ThenRaises<ChangedEventArgs>((c, h) => c.Changed += h, (c, h) => c.Changed -= h)
            .Then(r => Assert.Equal(1, r.Args.Value))
            .RunAsync();

        Assert.True(result.Passed);
    }

    // ---- ThenNotRaises

    [Fact]
    public void NotRaises_passes_and_keeps_the_When_result_as_state()
    {
        var observed = 0;

        new Scenario()
            .Given(() => new Counter())
            .When(c => { c.Clear(); return 42; })
            .ThenNotRaises<ChangedEventArgs>("No change", (c, h) => c.Changed += h, (c, h) => c.Changed -= h)
            .Then(x => observed = x)
            .Run();

        Assert.Equal(42, observed);
    }

    [Fact]
    public void NotRaises_fails_when_an_event_is_raised()
    {
        var ex = Assert.Throws<ScenarioExpectationException>(() =>
            new Scenario("S")
                .Given(() => new Counter())
                .When(c => c.Add(2))
                .ThenNotRaises<ChangedEventArgs>("No change", (c, h) => c.Changed += h, (c, h) => c.Changed -= h)
                .Run());

        Assert.Equal("Scenario 'S' > Then 'No change'", ex.Data[Scenario.StepDataKey]);
        Assert.Contains("2", ex.Message);
    }

    [Fact]
    public void NotRaises_for_every_event_form()
    {
        new Scenario().Given(() => new Counter()).When(c => c.Add(1)).ThenNotRaises((c, h) => c.Reset += h, (c, h) => c.Reset -= h).Run();
        new Scenario().Given(() => new Counter()).When(c => c.Clear())
            .ThenNotRaises<Action<int>, int>(record => n => record(null, n), (c, h) => c.Ticked += h, (c, h) => c.Ticked -= h).Run();
        new Scenario().Given(() => new Counter()).When(c => c.Rename()).ThenPropertyNotChanged(c => c, nameof(Counter.Value)).Run();

        Assert.Throws<ScenarioExpectationException>(() =>
            new Scenario().Given(() => new Counter()).When(c => c.Add(1)).ThenPropertyNotChanged("Value stays", c => c, nameof(Counter.Value)).Run());
    }
}
