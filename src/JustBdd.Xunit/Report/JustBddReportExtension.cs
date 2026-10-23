using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Configurations;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.CommandLine;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.TestHost;
using Microsoft.Testing.Platform.Services;
using Xunit;
using Xunit.v3;

namespace Davish.JustBdd;

internal sealed class JustBddReportOptions : ICommandLineOptionsProvider
{
    public const string ReportOption = "report-justbdd-html";
    public const string FileNameOption = "report-justbdd-html-filename";
    public const string DefaultFileName = "justbdd.html";

    public string Uid => nameof(JustBddReportOptions);
    public string Version => "1.0.0";
    public string DisplayName => "Davish.JustBdd HTML report";
    public string Description => "Generates an HTML report of Davish.JustBdd scenarios.";

    public Task<bool> IsEnabledAsync() => Task.FromResult(true);

    public IReadOnlyCollection<CommandLineOption> GetCommandLineOptions() =>
    [
        new(ReportOption, "Generate an HTML report of Davish.JustBdd scenarios in the test results directory.", ArgumentArity.Zero, isHidden: false),
        new(FileNameOption, $"File name of the Davish.JustBdd HTML report (default: {DefaultFileName}). Requires --{ReportOption}.", ArgumentArity.ExactlyOne, isHidden: false),
    ];

    public Task<ValidationResult> ValidateOptionArgumentsAsync(CommandLineOption option, string[] arguments)
        => option.Name == FileNameOption && !arguments[0].EndsWith(".html", StringComparison.OrdinalIgnoreCase)
            ? ValidationResult.InvalidTask($"--{FileNameOption} must end with .html")
            : ValidationResult.ValidTask;

    public Task<ValidationResult> ValidateCommandLineOptionsAsync(ICommandLineOptions options)
        => options.IsOptionSet(FileNameOption) && !options.IsOptionSet(ReportOption)
            ? ValidationResult.InvalidTask($"--{FileNameOption} requires --{ReportOption}")
            : ValidationResult.ValidTask;
}

/// <summary>MTP data consumer, session handler, and scenario sink, enabled by the report option.</summary>
internal sealed class JustBddReporter(ICommandLineOptions options, IConfiguration configuration)
    : IDataConsumer, ITestSessionLifetimeHandler, IScenarioResultSink
{
    private IDisposable? _registration;

    internal ReportCollector Collector { get; } = new();

    public string Uid => nameof(JustBddReporter);
    public string Version => "1.0.0";
    public string DisplayName => "Davish.JustBdd HTML report";
    public string Description => "Generates an HTML report of Davish.JustBdd scenarios.";

    public Type[] DataTypesConsumed => [typeof(TestNodeUpdateMessage)];

    public Task<bool> IsEnabledAsync() => Task.FromResult(options.IsOptionSet(JustBddReportOptions.ReportOption));

    public Task OnTestSessionStartingAsync(ITestSessionContext context)
    {
        _registration = ScenarioResults.AddSink(this);
        return Task.CompletedTask;
    }

    // Scenarios execute within tests, so TestContext.Current identifies their owner and its feature.
    public void Report(ScenarioResult result)
    {
        var context = TestContext.Current;
        var feature = context.TestMethod is IXunitTestMethod method ? FeatureResolver.Resolve(method.Method) : null;
        Collector.AddScenario(context.Test?.TestDisplayName, result, feature);
    }

    public Task ConsumeAsync(IDataProducer dataProducer, IData value, CancellationToken cancellationToken)
    {
        if (value is TestNodeUpdateMessage { TestNode: var node }
            && Outcome(node.Properties.SingleOrDefault<TestNodeStateProperty>()) is { } outcome)
        {
            var method = node.Properties.SingleOrDefault<TestMethodIdentifierProperty>();
            var duration = node.Properties.SingleOrDefault<TimingProperty>()?.GlobalTiming.Duration.TotalMilliseconds ?? 0;
            // xUnit reports each trait as a TestMetadataProperty. PropertyBag order is an implementation detail
            // (it enumerates in reverse insertion order), so sort for a stable report.
            var traits = node.Properties.AsEnumerable().OfType<TestMetadataProperty>()
                .OrderBy(t => t.Key, StringComparer.Ordinal).ThenBy(t => t.Value, StringComparer.Ordinal)
                .Select(t => new KeyValuePair<string, string>(t.Key, t.Value)).ToArray();
            Collector.SetOutcome(node.DisplayName, outcome, duration, method?.TypeName, method?.MethodName, traits);
        }
        return Task.CompletedTask;
    }

    public Task OnTestSessionFinishingAsync(ITestSessionContext context)
    {
        _registration?.Dispose();

        var fileName = options.TryGetOptionArgumentList(JustBddReportOptions.FileNameOption, out var args)
            ? args[0]
            : JustBddReportOptions.DefaultFileName;
        var directory = configuration.GetTestResultDirectory();
        Directory.CreateDirectory(directory);

        var tests = Collector.Build();
        File.WriteAllText(Path.Combine(directory, fileName),
            HtmlWriter.Render(tests, Collector.StartTime, DateTimeOffset.UtcNow, typeof(TestContext).Assembly.GetName().Name));
        return Task.CompletedTask;
    }

    // Handle final outcomes only; intermediate states return null.
    internal static string? Outcome(TestNodeStateProperty? state) => state switch
    {
        PassedTestNodeStateProperty => "passed",
        FailedTestNodeStateProperty => "failed",
        ErrorTestNodeStateProperty => "errored",
#pragma warning disable CS0618 // Older frameworks may still report the obsolete cancellation state.
        CancelledTestNodeStateProperty => "errored",
#pragma warning restore CS0618
        TimeoutTestNodeStateProperty => "timedOut",
        SkippedTestNodeStateProperty => "skipped",
        _ => null,
    };
}
