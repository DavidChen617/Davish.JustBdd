using System.Reflection;
using System.Runtime.Versioning;
using System.Text.Json;
using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Configurations;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.TestHost;
using Microsoft.Testing.Platform.Services;
using Xunit;
using Xunit.v3;
using IMessageBus = Microsoft.Testing.Platform.Messages.IMessageBus;

namespace Davish.JustBdd;

/// <summary>
/// xUnit's report options. Davish.JustBdd writes its reports alongside xUnit's instead of registering options of its own,
/// so a solution with test projects that do not reference Davish.JustBdd can still pass them to every project.
/// </summary>
internal static class XunitReportOptions
{
    public const string Html = "report-xunit-html";
    public const string HtmlFileName = "report-xunit-html-filename";
    public const string Ctrf = "report-xunit-ctrf";
    public const string CtrfFileName = "report-xunit-ctrf-filename";
}

/// <summary>MTP data consumer, session handler, and scenario sink, enabled by xUnit's HTML or CTRF report option.</summary>
internal sealed class JustBddReporter(ICommandLineOptions options, IConfiguration configuration, IMessageBus? bus = null)
    : IDataConsumer, IDataProducer, ITestSessionLifetimeHandler, IScenarioResultSink
{
    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true };

    private IDisposable? _registration;

    internal ReportCollector Collector { get; } = new();

    public string Uid => nameof(JustBddReporter);
    public string Version => "1.0.0";
    public string DisplayName => "Davish.JustBdd report";
    public string Description => "Writes Davish.JustBdd HTML and CTRF reports next to xUnit's reports.";

    public Type[] DataTypesConsumed => [typeof(TestNodeUpdateMessage)];
    public Type[] DataTypesProduced => [typeof(SessionFileArtifact)];

    public Task<bool> IsEnabledAsync()
        => Task.FromResult(options.IsOptionSet(XunitReportOptions.Html) || options.IsOptionSet(XunitReportOptions.Ctrf));

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
        if (value is TestNodeUpdateMessage { TestNode: var node } && FromNode(node) is { } result)
            Collector.SetResult(result);
        return Task.CompletedTask;
    }

    /// <summary>Reads a finished test from its node; returns null for intermediate states.</summary>
    internal static TestNodeResult? FromNode(TestNode node)
    {
        var state = node.Properties.SingleOrDefault<TestNodeStateProperty>();
        if (Outcome(state) is not { } outcome) return null;

        var method = node.Properties.SingleOrDefault<TestMethodIdentifierProperty>();
        var timing = node.Properties.SingleOrDefault<TimingProperty>()?.GlobalTiming;
        var location = node.Properties.SingleOrDefault<TestFileLocationProperty>();
        var exception = state switch
        {
            FailedTestNodeStateProperty failed => failed.Exception,
            ErrorTestNodeStateProperty error => error.Exception,
            TimeoutTestNodeStateProperty timeout => timeout.Exception,
#pragma warning disable CS0618 // Older frameworks may still report the obsolete cancellation state.
            CancelledTestNodeStateProperty cancelled => cancelled.Exception,
#pragma warning restore CS0618
            _ => null,
        };
        var properties = node.Properties.AsEnumerable().ToArray();

        return new TestNodeResult(node.DisplayName, outcome, timing?.Duration.TotalMilliseconds ?? 0, method?.TypeName, method?.MethodName)
        {
            Uid = node.Uid.Value,
            AssemblyName = method?.AssemblyFullName,
            Namespace = method?.Namespace,
            Start = timing?.StartTime,
            Stop = timing?.EndTime,
            FilePath = location?.FilePath,
            Line = location?.LineSpan.Start.Line,
            Message = exception?.Message ?? state!.Explanation,
            Trace = exception?.StackTrace,
            // xUnit reports each trait as a TestMetadataProperty. PropertyBag order is an implementation detail
            // (it enumerates in reverse insertion order), so sort for a stable report.
            Traits = properties.OfType<TestMetadataProperty>()
                .OrderBy(t => t.Key, StringComparer.Ordinal).ThenBy(t => t.Value, StringComparer.Ordinal)
                .Select(t => new KeyValuePair<string, string>(t.Key, t.Value)).ToArray(),
            Stdout = Lines(node.Properties.SingleOrDefault<StandardOutputProperty>()?.StandardOutput),
            Attachments = properties.OfType<FileArtifactProperty>()
                .Select(a => new ReportAttachment(a.DisplayName, a.FileInfo.FullName)).ToArray(),
        };
    }

    private static string[] Lines(string? text)
    {
        if (string.IsNullOrEmpty(text)) return [];
        var lines = text!.Split('\n').Select(l => l.TrimEnd('\r')).ToList();
        if (lines[lines.Count - 1].Length == 0) lines.RemoveAt(lines.Count - 1);
        return [.. lines];
    }

    public async Task OnTestSessionFinishingAsync(ITestSessionContext context)
    {
        _registration?.Dispose();

        var directory = configuration.GetTestResultDirectory();
        Directory.CreateDirectory(directory);
        var stop = DateTimeOffset.UtcNow;
        var testAssembly = Assembly.GetEntryAssembly();
        var assemblyName = testAssembly?.GetName().Name ?? "tests";

        if (options.IsOptionSet(XunitReportOptions.Html))
        {
            var path = Path.Combine(directory, FileName(XunitReportOptions.HtmlFileName, assemblyName, ".html"));
            File.WriteAllText(path, HtmlReport.Render(Collector.BuildAll(), Collector.StartTime, stop, assemblyName, TargetFramework(testAssembly)));
            await PublishAsync(context, path, "Davish.JustBdd HTML report", "Scenarios of the tests in this run");
        }

        if (options.IsOptionSet(XunitReportOptions.Ctrf))
        {
            var path = Path.Combine(directory, FileName(XunitReportOptions.CtrfFileName, assemblyName, ".ctrf.json"));
            var run = new CtrfRun(Collector.StartTime, stop, assemblyName, TargetFramework(testAssembly));
            File.WriteAllText(path, CtrfWriter.Build(Collector.BuildAll(), run).ToJsonString(Indented));
            await PublishAsync(context, path, "Davish.JustBdd CTRF report", "Test results with scenarios in Common Test Report Format");
        }
    }

    private static string? TargetFramework(Assembly? assembly) => assembly?.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName;

    // Follow xUnit's file name when one is given ("x.html" becomes "x.justbdd.html"); otherwise name the file after the
    // test assembly so the reports of several test projects in one results directory do not overwrite each other.
    private string FileName(string fileNameOption, string assemblyName, string suffix)
    {
        if (!options.TryGetOptionArgumentList(fileNameOption, out var args) || args is not [var xunitFileName, ..])
            return $"justbdd-{assemblyName}{suffix}";
        var stem = Path.Combine(Path.GetDirectoryName(xunitFileName) ?? "", Path.GetFileNameWithoutExtension(xunitFileName));
        return stem + ".justbdd" + suffix;
    }

    private Task PublishAsync(ITestSessionContext context, string path, string displayName, string description)
        => bus?.PublishAsync(this, new SessionFileArtifact(context.SessionUid, new FileInfo(path), displayName, description)) ?? Task.CompletedTask;

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
