using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Schema;
using Microsoft.Testing.Platform.Extensions.Messages;

namespace Davish.JustBdd.Tests;

public class CtrfWriterTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 24, 3, 0, 0, TimeSpan.Zero);
    private static readonly CtrfRun Run = new(Start, Start.AddMilliseconds(250), "Shop.Tests", ".NETCoreApp,Version=v11.0");

    private static readonly Lazy<JsonSchema> Schema = new(() =>
        JsonSchema.FromText(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Ctrf", "ctrf.schema.json"))));

    private static async Task<ScenarioResult> Passing(string name = "Rename")
        => await new Scenario(name).Given("a user", () => "David").When("renamed", n => n + "sh").Then("it is renamed", n => Assert.Equal("Davidsh", n)).RunAsync();

    private static async Task<ScenarioResult> Failing()
    {
        try { await new Scenario("Fails").Given("one", () => 1).When("doubled", x => x * 2).Then("is three", x => Assert.Equal(3, x)).And("is positive", x => Assert.True(x > 0)).RunAsync(); }
        catch (Exception ex) { return (ScenarioResult)ex.Data[Davish.JustBdd.Scenario.ResultDataKey]!; }
        throw new InvalidOperationException("The scenario should fail.");
    }

    private static async Task<ScenarioResult> Nested()
    {
        try
        {
            await new Scenario("Outer")
                .Given("a prepared user", new Scenario("Inner").Given("a name", () => " David ").When("trimmed", n => n.Trim()).Then("is trimmed", n => Assert.Equal("David", n)))
                .When("upper", n => n.ToUpperInvariant())
                .ThenThrows<InvalidOperationException>("never throws")
                .RunAsync();
        }
        catch (ScenarioExpectationException ex) { return (ScenarioResult)ex.Data[Davish.JustBdd.Scenario.ResultDataKey]!; }
        throw new InvalidOperationException("The scenario should fail.");
    }

    private static TestNodeResult Node(string name, string outcome, string? message = null, string? trace = null) => new(name, outcome, 7, "CartTests", name)
    {
        Uid = "uid-" + name,
        AssemblyName = "Shop.Tests",
        Namespace = "Shop.Tests",
        Start = Start.AddMilliseconds(10),
        Stop = Start.AddMilliseconds(17),
        FilePath = "/src/CartTests.cs",
        Line = 12,
        Message = message,
        Trace = trace,
    };

    private static async Task<IReadOnlyList<ReportTest>> MixedRun()
    {
        var c = new ReportCollector();
        c.AddScenario("Passes", await Passing(), new ReportFeature("Cart", "Cart", "Cart rules", [new FeatureLink("Spec", "https://example.com")], [new("Owner", ["team-a"])]));
        c.SetResult(Node("Passes", "passed") with
        {
            Traits = [new("Category", "Smoke"), new("Owner", "team-a"), new("Owner", "team-b")],
            Stdout = ["first line", "second line"],
            Attachments = [new ReportAttachment("note", "/results/note.txt")],
        });
        c.AddScenario("Fails", await Failing());
        c.SetResult(Node("Fails", "failed", "Assert.Equal() Failure", "at Fails()"));
        c.AddScenario("Nested", await Nested());
        c.SetResult(Node("Nested", "failed", "Expected an exception"));
        c.SetResult(Node("Errors", "errored", "System.InvalidOperationException : boom", "at Errors()"));
        c.SetResult(Node("TimesOut", "timedOut", "Test execution timed out after 50 milliseconds"));
        c.SetResult(Node("Skipped", "skipped", "static reason"));
        return c.BuildAll();
    }

    private static JsonObject Write(IReadOnlyList<ReportTest> tests) => CtrfWriter.Build(tests, Run);

    private static JsonElement Element(JsonObject doc) => JsonDocument.Parse(doc.ToJsonString()).RootElement;

    private static JsonObject Test(JsonObject doc, string name)
        => doc["results"]!["tests"]!.AsArray().Single(t => t!["name"]!.GetValue<string>() == name)!.AsObject();

    [Fact]
    public async Task Document_has_the_required_ctrf_fields()
    {
        var doc = Write(await MixedRun());

        Assert.Equal("CTRF", doc["reportFormat"]!.GetValue<string>());
        Assert.Matches(@"^\d+\.\d+\.\d+$", doc["specVersion"]!.GetValue<string>());
        var results = doc["results"]!;
        Assert.NotNull(results["tool"]!["name"]);
        var summary = results["summary"]!;
        Assert.Equal((6, 1, 4, 1, 0, 0), (summary["tests"]!.GetValue<int>(), summary["passed"]!.GetValue<int>(), summary["failed"]!.GetValue<int>(),
            summary["skipped"]!.GetValue<int>(), summary["pending"]!.GetValue<int>(), summary["other"]!.GetValue<int>()));
        Assert.Equal((Start.ToUnixTimeMilliseconds(), Start.ToUnixTimeMilliseconds() + 250), (summary["start"]!.GetValue<long>(), summary["stop"]!.GetValue<long>()));
        Assert.All(results["tests"]!.AsArray(), t =>
        {
            Assert.False(string.IsNullOrEmpty(t!["name"]!.GetValue<string>()));
            Assert.NotNull(t["status"]);
            Assert.Equal(7, t["duration"]!.GetValue<long>());
        });
    }

    [Fact]
    public async Task Document_validates_against_the_ctrf_schema()
    {
        var doc = Write(await MixedRun());

        var result = Schema.Value.Evaluate(Element(doc), new EvaluationOptions { OutputFormat = OutputFormat.List });

        Assert.True(result.IsValid, string.Join("\n", (result.Details ?? []).Where(d => d.Errors is not null)
            .SelectMany(d => d.Errors!.Select(e => $"{d.InstanceLocation}: {e.Value}"))));
    }

    [Fact]
    public async Task Schema_rejects_fields_outside_extra()
    {
        var doc = Write(await MixedRun());
        Test(doc, "Passes")["feature"] = "Cart";

        Assert.False(Schema.Value.Evaluate(Element(doc)).IsValid);
    }

    [Theory]
    [InlineData("Passes", "passed", "passed")]
    [InlineData("Fails", "failed", "failed")]
    [InlineData("Errors", "failed", "errored")]
    [InlineData("TimesOut", "failed", "timedOut")]
    [InlineData("Skipped", "skipped", "skipped")]
    public async Task Outcomes_map_to_ctrf_statuses(string name, string status, string rawStatus)
    {
        var test = Test(Write(await MixedRun()), name);

        Assert.Equal((status, rawStatus), (test["status"]!.GetValue<string>(), test["rawStatus"]!.GetValue<string>()));
    }

    [Fact]
    public async Task Failed_test_carries_message_and_trace()
    {
        var test = Test(Write(await MixedRun()), "Errors");

        Assert.Equal(("System.InvalidOperationException : boom", "at Errors()"), (test["message"]!.GetValue<string>(), test["trace"]!.GetValue<string>()));
    }

    [Fact]
    public async Task Skipped_test_carries_its_reason()
        => Assert.Equal("static reason", Test(Write(await MixedRun()), "Skipped")["message"]!.GetValue<string>());

    [Fact]
    public async Task Scenario_steps_are_flattened_into_ctrf_steps()
    {
        var doc = Write(await MixedRun());

        static IEnumerable<(string, string)> Steps(JsonObject test)
            => test["steps"]!.AsArray().Select(s => (s!["name"]!.GetValue<string>(), s["status"]!.GetValue<string>()));

        Assert.Equal([("Given a user", "passed"), ("When renamed", "passed"), ("Then it is renamed", "passed")], Steps(Test(doc, "Passes")));
        Assert.Equal([("Given one", "passed"), ("When doubled", "passed"), ("Then is three", "failed"), ("And is positive", "skipped")], Steps(Test(doc, "Fails")));
        // The inner scenario's steps follow the step that ran it.
        Assert.Equal([("Given a prepared user", "passed"), ("Given a name", "passed"), ("When trimmed", "passed"), ("Then is trimmed", "passed"),
                ("When upper", "passed"), ("Then never throws", "failed")],
            Steps(Test(doc, "Nested")));
        Assert.All(doc["results"]!["tests"]!.AsArray().SelectMany(t => t!["steps"]?.AsArray() ?? []), s => Assert.Equal(["name", "status"], s!.AsObject().Select(p => p.Key)));
    }

    [Fact]
    public async Task Step_without_a_description_is_named_by_its_code()
    {
        var c = new ReportCollector();
        c.AddScenario("T", await new Scenario("s").Given(() => 1).Then(x => Assert.Equal(1, x)).RunAsync());
        c.SetResult(Node("T", "passed"));

        var steps = Test(Write(c.BuildAll()), "T")["steps"]!.AsArray().Select(s => s!["name"]!.GetValue<string>());

        Assert.Equal(["Given () => 1", "Then x => Assert.Equal(1, x)"], steps);
    }

    [Fact]
    public async Task Threw_as_expected_counts_as_passed()
    {
        var c = new ReportCollector();
        c.AddScenario("T", await new Scenario("s").Given("one", () => 1).When("fails", int (_) => throw new InvalidOperationException()).ThenThrows<InvalidOperationException>("throws").RunAsync());
        c.SetResult(Node("T", "passed"));

        var steps = Test(Write(c.BuildAll()), "T")["steps"]!.AsArray().Select(s => s!["status"]!.GetValue<string>());

        Assert.Equal(["passed", "passed", "passed"], steps);
    }

    [Fact]
    public async Task Full_scenarios_are_kept_under_extra()
    {
        var doc = Write(await MixedRun());

        var extra = Test(doc, "Passes")["extra"]![Ctrf.ExtensionKey]!;
        Assert.Equal("Cart", extra["feature"]!.GetValue<string>());
        var scenario = extra["scenarios"]![0]!;
        Assert.Equal("Rename", scenario["description"]!.GetValue<string>());
        Assert.Equal(["Given", "When", "Then"], scenario["steps"]!.AsArray().Select(s => s!["kind"]!.GetValue<string>()));
        Assert.Equal("n => n + \"sh\"", scenario["steps"]![1]!["code"]!.GetValue<string>());

        var feature = doc["results"]!["extra"]![Ctrf.ExtensionKey]!["features"]!["Cart"]!;
        Assert.Equal(("Cart rules", "Spec", "Owner"), (feature["description"]!.GetValue<string>(), feature["links"]![0]!["title"]!.GetValue<string>(),
            feature["metadata"]![0]!["key"]!.GetValue<string>()));
    }

    [Fact]
    public async Task Tests_without_scenarios_are_included()
    {
        var test = Test(Write(await MixedRun()), "Errors");

        Assert.Null(test["steps"]);
        var extra = test["extra"]![Ctrf.ExtensionKey]!.AsObject();
        Assert.Equal(["className", "methodName"], extra.Select(p => p.Key));
    }

    [Fact]
    public async Task Test_details_map_to_ctrf_fields()
    {
        var doc = Write(await MixedRun());
        var test = Test(doc, "Passes");

        Assert.Equal("uid-Passes", test["testId"]!.GetValue<string>());
        Assert.Equal(["Shop.Tests", "Shop.Tests", "CartTests"], test["suite"]!.AsArray().Select(s => s!.GetValue<string>()));
        Assert.Equal(("/src/CartTests.cs", 12), (test["filePath"]!.GetValue<string>(), test["line"]!.GetValue<int>()));
        Assert.Equal((Start.ToUnixTimeMilliseconds() + 10, Start.ToUnixTimeMilliseconds() + 17), (test["start"]!.GetValue<long>(), test["stop"]!.GetValue<long>()));
        Assert.Equal("Smoke", test["labels"]!["Category"]!.GetValue<string>());
        Assert.Equal(["team-a", "team-b"], test["labels"]!["Owner"]!.AsArray().Select(v => v!.GetValue<string>()));
        Assert.Equal(["first line", "second line"], test["stdout"]!.AsArray().Select(v => v!.GetValue<string>()));
        var attachment = test["attachments"]![0]!;
        Assert.Equal(("note", "text/plain", "/results/note.txt"), (attachment["name"]!.GetValue<string>(), attachment["contentType"]!.GetValue<string>(), attachment["path"]!.GetValue<string>()));

        var environment = doc["results"]!["environment"]!;
        Assert.NotNull(environment["osPlatform"]);
        Assert.Equal(("Shop.Tests", ".NETCoreApp,Version=v11.0"), (environment["extra"]![Ctrf.ExtensionKey]!["assembly"]!.GetValue<string>(),
            environment["extra"]![Ctrf.ExtensionKey]!["targetFramework"]!.GetValue<string>()));
    }
}

public class TestNodeResultTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 24, 3, 0, 0, TimeSpan.Zero);

    private static TestNode Node(TestNodeStateProperty state, params IProperty[] extra) => new()
    {
        Uid = new TestNodeUid("uid-1"),
        DisplayName = "Shop.Tests.CartTests.Adds(x: 2)",
        Properties = new PropertyBag([
            state,
            new TimingProperty(new TimingInfo(Start, Start.AddMilliseconds(6), TimeSpan.FromMilliseconds(6))),
            new TestFileLocationProperty("/src/CartTests.cs", new LinePositionSpan(new LinePosition(12, -1), new LinePosition(12, -1))),
            new TestMethodIdentifierProperty("Shop.Tests", "Shop.Tests", "CartTests", "Adds", 0, ["System.Int32"], "System.Void"),
            .. extra]),
    };

    [Fact]
    public void Node_properties_map_to_the_test_result()
    {
        var result = JustBddReporter.FromNode(Node(PassedTestNodeStateProperty.CachedInstance,
            new TestMetadataProperty("Category", "Smoke"),
            new StandardOutputProperty("first line\nsecond line\n"),
            new FileArtifactProperty(new FileInfo("/results/note.txt"), "note", null)))!;

        Assert.Equal(("uid-1", "Shop.Tests.CartTests.Adds(x: 2)", "passed", 6d), (result.Uid, result.DisplayName, result.Outcome, result.DurationMs));
        Assert.Equal(("Shop.Tests", "Shop.Tests", "CartTests", "Adds"), (result.AssemblyName, result.Namespace, result.ClassName, result.MethodName));
        Assert.Equal((Start, Start.AddMilliseconds(6)), (result.Start, result.Stop));
        Assert.Equal(("/src/CartTests.cs", 12), (result.FilePath, result.Line));
        Assert.Equal([new KeyValuePair<string, string>("Category", "Smoke")], result.Traits);
        Assert.Equal(["first line", "second line"], result.Stdout);
        Assert.Equal(("note", new FileInfo("/results/note.txt").FullName), (result.Attachments[0].Name, result.Attachments[0].Path));
        Assert.Null(result.Message);
    }

    [Fact]
    public void Failure_exception_becomes_message_and_trace()
    {
        Exception ex;
        try { throw new InvalidOperationException("boom"); } catch (Exception e) { ex = e; }

        var result = JustBddReporter.FromNode(Node(new ErrorTestNodeStateProperty(ex)))!;

        Assert.Equal(("errored", "boom"), (result.Outcome, result.Message));
        Assert.Contains(nameof(Failure_exception_becomes_message_and_trace), result.Trace);
    }

    [Fact]
    public void Skip_explanation_becomes_message()
        => Assert.Equal("static reason", JustBddReporter.FromNode(Node(new SkippedTestNodeStateProperty("static reason")))!.Message);

    [Fact]
    public void In_progress_node_is_ignored()
        => Assert.Null(JustBddReporter.FromNode(Node(InProgressTestNodeStateProperty.CachedInstance)));
}
