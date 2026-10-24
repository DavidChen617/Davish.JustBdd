using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Configurations;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Messages;
using Microsoft.Testing.Platform.Services;
using Microsoft.Testing.Platform.TestHost;

namespace Davish.JustBdd.Tests;

public sealed class ReportOptionTests : IDisposable
{
    private static readonly string Assembly = System.Reflection.Assembly.GetEntryAssembly()!.GetName().Name!;

    private readonly string _directory = Path.Combine(Path.GetTempPath(), "justbdd-" + Guid.NewGuid().ToString("N"));
    private readonly Bus _bus = new();

    public void Dispose()
    {
        if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true);
    }

    private JustBddReporter Reporter(params (string Name, string[] Args)[] options)
    {
        var reporter = new JustBddReporter(new Options(options.ToDictionary(o => o.Name, o => o.Args)), new Configuration(_directory), _bus);
        reporter.Collector.SetOutcome("T.Plain", "passed", 1, "T", "Plain");
        return reporter;
    }

    private static Task Finish(JustBddReporter reporter) => reporter.OnTestSessionFinishingAsync(new Session());

    private string[] Files() => Directory.Exists(_directory) ? [.. Directory.GetFiles(_directory).Select(Path.GetFileName).Order()!] : [];

    [Fact]
    public async Task Xunit_ctrf_option_writes_the_justbdd_ctrf()
    {
        var reporter = Reporter(("report-xunit-ctrf", []));

        await Finish(reporter);

        Assert.Equal([$"justbdd-{Assembly}.ctrf.json"], Files());
        var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(_directory, Files()[0]))).RootElement;
        Assert.Equal("CTRF", doc.GetProperty("reportFormat").GetString());
        // CTRF lists tests without scenarios too.
        Assert.Equal("T.Plain", doc.GetProperty("results").GetProperty("tests")[0].GetProperty("name").GetString());
    }

    [Fact]
    public async Task Xunit_html_option_writes_the_justbdd_html()
    {
        var reporter = Reporter(("report-xunit-html", []));

        await Finish(reporter);

        Assert.Equal([$"justbdd-{Assembly}.html"], Files());
        Assert.Contains("id=\"mtp-data\"", File.ReadAllText(Path.Combine(_directory, Files()[0])));
    }

    [Fact]
    public async Task Html_report_lists_tests_without_scenarios()
    {
        var reporter = Reporter(("report-xunit-html", []));

        await Finish(reporter);

        // The HTML data island escapes quotes inside strings, but test names are plain.
        Assert.Contains("\"displayName\":\"T.Plain\"", File.ReadAllText(Path.Combine(_directory, Files()[0])));
    }

    [Theory]
    [InlineData("report-xunit-ctrf", "report-xunit-ctrf-filename", "x.json", "x.justbdd.ctrf.json")]
    [InlineData("report-xunit-html", "report-xunit-html-filename", "x.html", "x.justbdd.html")]
    public async Task Xunit_file_name_is_followed(string option, string fileNameOption, string fileName, string expected)
    {
        var reporter = Reporter((option, []), (fileNameOption, [fileName]));

        await Finish(reporter);

        Assert.Equal([expected], Files());
    }

    [Fact]
    public async Task No_report_without_an_xunit_report_option()
    {
        Assert.False(await Reporter().IsEnabledAsync());
        Assert.True(await Reporter(("report-xunit-html", [])).IsEnabledAsync());
        Assert.True(await Reporter(("report-xunit-ctrf", [])).IsEnabledAsync());
    }

    [Fact]
    public async Task Report_files_are_published_as_session_artifacts()
    {
        var reporter = Reporter(("report-xunit-html", []), ("report-xunit-ctrf", []));

        await Finish(reporter);

        Assert.Equal(Files().Select(f => Path.Combine(_directory, f)),
            _bus.Published.OfType<SessionFileArtifact>().Select(a => a.FileInfo.FullName).Order());
    }

    private sealed class Options(Dictionary<string, string[]> values) : ICommandLineOptions
    {
        public bool IsOptionSet(string optionName) => values.ContainsKey(optionName);

        public bool TryGetOptionArgumentList(string optionName, [NotNullWhen(true)] out string[]? arguments)
            => values.TryGetValue(optionName, out arguments);
    }

    private sealed class Configuration(string directory) : IConfiguration
    {
        public string? this[string key] => key == "platformOptions:resultDirectory" ? directory : null;
    }

    private sealed class Bus : IMessageBus
    {
        public List<IData> Published { get; } = [];

        public Task PublishAsync(IDataProducer dataProducer, IData data)
        {
            Published.Add(data);
            return Task.CompletedTask;
        }
    }

    private sealed class Session : ITestSessionContext
    {
        public SessionUid SessionUid { get; } = new("session");
        public CancellationToken CancellationToken => TestContext.Current.CancellationToken;
    }
}
