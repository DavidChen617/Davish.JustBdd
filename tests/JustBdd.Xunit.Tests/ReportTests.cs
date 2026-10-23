using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Testing.Platform.Extensions.Messages;

namespace Davish.JustBdd.Tests;

public class ReportCollectorTests
{
    private static async Task<ScenarioResult> Scenario(string name, bool fail = false)
    {
        try { return await new Scenario(name).Given(() => 1).Then(x => { if (fail) throw new InvalidOperationException(); }); }
        catch (InvalidOperationException ex) { return (ScenarioResult)ex.Data[Davish.JustBdd.Scenario.ResultDataKey]!; }
    }

    [Fact]
    public async Task Groups_scenarios_by_test_and_uses_the_mtp_outcome()
    {
        var c = new ReportCollector();
        c.AddScenario("T.A", await Scenario("a1"));
        c.AddScenario("T.B", await Scenario("b1"));
        c.AddScenario("T.A", await Scenario("a2"));
        c.SetOutcome("T.A", "failed", 12, "T", "A");   // For example, an Assert after the scenario failed
        c.SetOutcome("T.B", "passed", 3, "T", "B");
        c.SetOutcome("T.Plain", "passed", 1, "T", "Plain");   // An ordinary test without scenarios

        var tests = c.Build();

        Assert.Equal(["T.A", "T.B"], tests.Select(t => t.DisplayName));
        Assert.Equal(["a1", "a2"], tests[0].Scenarios.Select(s => s.Description));
        Assert.Equal(("failed", 12d, "T", "A"), (tests[0].Outcome, tests[0].DurationMs, tests[0].ClassName, tests[0].MethodName));
        Assert.All(tests[0].Scenarios, s => Assert.True(s.Passed));
    }

    [Fact]
    public async Task Without_an_mtp_outcome_the_scenarios_decide()
    {
        var c = new ReportCollector();
        c.AddScenario("T.Ok", await Scenario("ok"));
        c.AddScenario("T.Bad", await Scenario("bad", fail: true));

        Assert.Equal(["passed", "failed"], c.Build().Select(t => t.Outcome));
    }

    [Fact]
    public async Task Scenarios_outside_a_test_are_ignored()
    {
        var c = new ReportCollector();
        c.AddScenario(null, await Scenario("x"));

        Assert.Empty(c.Build());
    }

    [Fact]
    public async Task Concurrent_adds_are_all_kept()
    {
        var c = new ReportCollector();
        var scenario = await Scenario("s");

        await Task.WhenAll(Enumerable.Range(0, 100).Select(i => Task.Run(() => c.AddScenario($"T.{i % 10}", scenario), TestContext.Current.CancellationToken)));

        var tests = c.Build();
        Assert.Equal(10, tests.Count);
        Assert.Equal(100, tests.Sum(t => t.Scenarios.Count));
    }
}

public class OutcomeMappingTests
{
    public static TheoryData<TestNodeStateProperty, string?> States => new()
    {
        { PassedTestNodeStateProperty.CachedInstance, "passed" },
        { new FailedTestNodeStateProperty("x"), "failed" },
        { new ErrorTestNodeStateProperty("x"), "errored" },
        { new TimeoutTestNodeStateProperty("x"), "timedOut" },
        { new SkippedTestNodeStateProperty("x"), "skipped" },
        { InProgressTestNodeStateProperty.CachedInstance, null },
        { DiscoveredTestNodeStateProperty.CachedInstance, null },
    };

    [Theory]
    [MemberData(nameof(States))]
    public void Maps_mtp_states_to_report_outcomes(TestNodeStateProperty state, string? expected)
        => Assert.Equal(expected, JustBddReporter.Outcome(state));
}

public class ReporterSinkTests
{
    [Fact]
    public async Task Scenarios_are_grouped_under_the_currently_running_test()
    {
        var reporter = new JustBddReporter(null!, null!);

        reporter.Report(await new Scenario("s").Then(() => { }));

        var test = Assert.Single(reporter.Collector.Build());
        Assert.Equal(TestContext.Current.Test!.TestDisplayName, test.DisplayName);
        Assert.Equal("s", Assert.Single(test.Scenarios).Description);
    }
}

public class HtmlWriterTests
{
    private static async Task<IReadOnlyList<ReportTest>> Tests(string scenarioName = "Rename")
    {
        var c = new ReportCollector();
        c.AddScenario("UserTests.Rename", await new Scenario(scenarioName).Given("a user", () => "David").When(n => n + "sh").Then("renamed", n => Assert.Equal("Davidsh", n)));
        c.SetOutcome("UserTests.Rename", "passed", 5, "UserTests", "Rename");
        return c.Build();
    }

    private static (string Island, JsonElement Data) Island(string html)
    {
        var m = Regex.Match(html, """<script id="mtp-data" type="application/json">(.*?)</script>""", RegexOptions.Singleline);
        Assert.True(m.Success, "The data island was not found");
        return (m.Groups[1].Value, JsonDocument.Parse(m.Groups[1].Value).RootElement);
    }

    [Fact]
    public async Task Embeds_tests_and_scenarios_in_the_template_data_island()
    {
        var (_, data) = Island(HtmlWriter.Render(await Tests(), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "xunit"));

        Assert.Equal(HtmlWriter.Title, data.GetProperty("testApplication").GetString());
        Assert.Equal(1, data.GetProperty("summary").GetProperty("total").GetInt32());
        var test = data.GetProperty("tests")[0];
        Assert.Equal(("UserTests.Rename", "passed"), (test.GetProperty("displayName").GetString(), test.GetProperty("outcome").GetString()));
        var scenario = test.GetProperty("scenarios")[0];
        Assert.Equal("Rename", scenario.GetProperty("description").GetString());
        Assert.Equal(["Given", "When", "Then"], scenario.GetProperty("steps").EnumerateArray().Select(s => s.GetProperty("kind").GetString()));
        Assert.Equal("n => n + \"sh\"", scenario.GetProperty("steps")[1].GetProperty("code").GetString());
    }

    [Fact]
    public async Task Test_content_cannot_break_out_of_the_data_island()
    {
        const string attack = "</script><script>alert(1)</script>";

        var (island, data) = Island(HtmlWriter.Render(await Tests(attack), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "xunit"));

        Assert.DoesNotContain("</script>", island, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(attack, data.GetProperty("tests")[0].GetProperty("scenarios")[0].GetProperty("description").GetString());
    }

    [Fact]
    public async Task No_template_placeholder_is_left_and_attribution_is_kept()
    {
        var html = HtmlWriter.Render(await Tests(), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "xunit");

        Assert.DoesNotContain("__MTP_", html);
        Assert.Contains("Microsoft.Testing.Extensions.HtmlReport", html);
        Assert.Contains("(MIT)", html);
    }

    [Fact]
    public async Task Template_parts_are_inlined_without_placeholders()
    {
        var html = HtmlWriter.Render(await Tests(), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "xunit");

        Assert.DoesNotContain("/*__", html);
        Assert.Contains("function bddScenario(", html);   // justbdd.js
        Assert.Contains(".badge.bdd", html);              // justbdd.css
    }

    private static Func<string, string> Parts(string css = "a{}", string js = "var a;") => name => name switch
    {
        "report.html" => "<style>\n/*__STYLE__*/\n</style><script>\n/*__SCRIPT__*/\n</script>",
        "report.css" => "/*__JUSTBDD_CSS__*/",
        "justbdd.css" => css,
        "report.js" => "/*__JUSTBDD_JS__*/",
        "justbdd.js" => js,
        _ => throw new ArgumentException(name),
    };

    [Theory]
    [InlineData("a{content:'</style>'}", "var a;")]
    [InlineData("a{}", "var a = '</SCRIPT>';")]
    public void Template_rejects_closing_tags_in_parts(string css, string js)
        => Assert.Throws<InvalidOperationException>(() => HtmlWriter.Assemble(Parts(css, js)));

    [Fact]
    public void Template_assembles_parts_in_place()
        => Assert.Equal("<style>\na{}\n</style><script>\nvar a;\n</script>", HtmlWriter.Assemble(Parts("a{}\n", "var a;\n")));
}
