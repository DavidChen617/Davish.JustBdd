using System.Text.Json.Nodes;

namespace Davish.JustBdd.Tests;

public class ReportMergeTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 24, 3, 0, 0, TimeSpan.Zero);

    private static async Task<CtrfDocument> JustBdd(string assembly, string test, DateTimeOffset start, DateTimeOffset stop, ReportFeature? feature = null)
    {
        var c = new ReportCollector();
        c.AddScenario(test, await new Scenario("s").Given("one", () => 1).Then("is one", x => Assert.Equal(1, x)).RunAsync(), feature);
        c.SetResult(new TestNodeResult(test, "passed", 3, "CartTests", "Adds"));
        var json = CtrfWriter.Build(c.BuildAll(), new CtrfRun(start, stop, assembly, null)).ToJsonString();
        return CtrfReader.Parse(json, $"justbdd-{assembly}.ctrf.json");
    }

    // The shape xUnit 4.0.1 writes for --report-xunit-ctrf (reduced).
    private static CtrfDocument Xunit(string assembly, string test, string status = "passed", long start = 0, long stop = 10) => CtrfReader.Parse($$"""
        {
          "reportFormat": "CTRF", "specVersion": "0.0.0",
          "results": {
            "tool": { "name": "xUnit.net v3", "version": "4.0.1" },
            "summary": { "tests": 1, "passed": 1, "failed": 0, "pending": 0, "skipped": 0, "other": 0, "start": {{start}}, "stop": {{stop}} },
            "tests": [
              {
                "name": "{{test}}", "status": "{{status}}", "duration": 4, "message": "Assert.Equal() Failure", "trace": "at {{test}}()",
                "suite": ["89b62fc2", "f6e6f9b4", "{{assembly}}.CartTests", "Adds"],
                "extra": { "id": "9a77", "collection": "f6e6", "type": "{{assembly}}.CartTests", "method": "Adds" }
              }
            ],
            "extra": { "suites": [ { "id": "89b62fc2", "filePath": "/build/{{assembly}}/bin/Debug/net11.0/{{assembly}}.dll" } ] }
          }
        }
        """, $"{assembly}.ctrf");

    private static JsonArray Tests(JsonObject view) => view["tests"]!.AsArray();

    [Fact]
    public async Task Merge_concatenates_tests_with_their_assembly()
    {
        var view = ReportView.FromCtrf([await JustBdd("Shop.Tests", "Shop.A", Start, Start.AddSeconds(1)), await JustBdd("Billing.Tests", "Billing.B", Start, Start.AddSeconds(1))]);

        Assert.Equal([("Shop.A", "Shop.Tests"), ("Billing.B", "Billing.Tests")],
            Tests(view).Select(t => ((string)t!["displayName"]!, (string)t["assembly"]!)));
        Assert.Equal([0, 1], Tests(view).Select(t => (int)t!["rowKey"]!));
    }

    [Fact]
    public async Task Merge_keeps_the_first_feature_of_a_name()
    {
        var first = new ReportFeature("Checkout", "Checkout", "First", [], []);
        var second = new ReportFeature("Checkout", "Checkout", "Second", [], []);

        var view = ReportView.FromCtrf([await JustBdd("Shop.Tests", "Shop.A", Start, Start, first), await JustBdd("Billing.Tests", "Billing.B", Start, Start, second)]);

        var feature = Assert.Single(view["features"]!.AsObject());
        Assert.Equal(("Checkout", "First"), (feature.Key, (string)feature.Value!["description"]!));
        Assert.All(Tests(view), t => Assert.Equal("Checkout", (string)t!["featureId"]!));
    }

    [Fact]
    public async Task Merge_keeps_each_projects_own_feature_definitions()
    {
        var shop = new ReportFeature("Checkout", "Checkout", "Shop's checkout", [], []);
        var billing = new ReportFeature("Checkout", "Checkout", "Billing's checkout", [], []);

        var view = ReportView.FromCtrf([await JustBdd("Shop.Tests", "Shop.A", Start, Start, shop), await JustBdd("Billing.Tests", "Billing.B", Start, Start, billing)]);

        var projects = view["projectFeatures"]!.AsObject();
        Assert.Equal("Shop's checkout", (string)projects["Shop.Tests"]!["Checkout"]!["description"]!);
        Assert.Equal("Billing's checkout", (string)projects["Billing.Tests"]!["Checkout"]!["description"]!);
    }

    [Fact]
    public async Task Single_project_has_no_project_features()
        => Assert.Null(ReportView.FromCtrf([await JustBdd("Shop.Tests", "Shop.A", Start, Start, new ReportFeature("Checkout", "Checkout", null, [], []))])["projectFeatures"]);

    [Fact]
    public async Task Merge_spans_the_earliest_start_and_latest_stop()
    {
        var view = ReportView.FromCtrf([
            await JustBdd("Shop.Tests", "Shop.A", Start.AddSeconds(5), Start.AddSeconds(8)),
            await JustBdd("Billing.Tests", "Billing.B", Start, Start.AddSeconds(6))]);

        Assert.Equal((Start.ToString("O"), Start.AddSeconds(8).ToString("O")), ((string)view["startTime"]!, (string)view["endTime"]!));
        Assert.Equal((2, 2, 6d), ((int)view["summary"]!["total"]!, (int)view["summary"]!["passed"]!, (double)view["summary"]!["totalDurationMs"]!));
    }

    [Fact]
    public async Task Merge_accepts_ctrf_from_other_producers()
    {
        var view = ReportView.FromCtrf([await JustBdd("Shop.Tests", "Shop.A", Start, Start), Xunit("Billing.Tests", "Billing.Tests.CartTests.Adds", "failed")]);

        var xunit = Tests(view)[1]!;
        Assert.Equal(("failed", "CartTests", "Adds", "Billing.Tests"), ((string)xunit["outcome"]!, (string)xunit["className"]!,
            (string)xunit["methodName"]!, (string)xunit["assembly"]!));
        Assert.Empty(xunit["scenarios"]!.AsArray());
        Assert.Null(xunit["featureId"]);
        Assert.Equal(1, (int)view["summary"]!["failed"]!);
    }

    [Fact]
    public async Task Merge_skips_the_xunit_ctrf_of_an_assembly_with_a_justbdd_ctrf()
    {
        var view = ReportView.FromCtrf([
            Xunit("Shop.Tests", "Shop.Tests.CartTests.Adds"),
            await JustBdd("Shop.Tests", "Shop.Tests.CartTests.Adds", Start, Start),
            Xunit("Billing.Tests", "Billing.Tests.CartTests.Adds")]);

        Assert.Equal([("Shop.Tests.CartTests.Adds", 1), ("Billing.Tests.CartTests.Adds", 0)],
            Tests(view).Select(t => ((string)t!["displayName"]!, t["scenarios"]!.AsArray().Count)));
    }

    [Fact]
    public void Statuses_without_a_raw_status_map_to_report_outcomes()
    {
        var view = ReportView.FromCtrf([Xunit("A", "A.T.Passes"), Xunit("B", "B.T.Pending", "pending"), Xunit("C", "C.T.Other", "other")]);

        Assert.Equal(["passed", "skipped", "errored"], Tests(view).Select(t => (string)t!["outcome"]!));
    }

    [Fact]
    public void Test_message_trace_and_output_are_shown()
    {
        var test = Tests(ReportView.FromCtrf([Xunit("A", "A.T.Fails", "failed")]))[0]!;

        Assert.Equal(("Assert.Equal() Failure", "at A.T.Fails()"), ((string)test["errorMessage"]!, (string)test["stackTrace"]!));
    }

    [Fact]
    public async Task Standard_output_lines_are_joined()
    {
        var c = new ReportCollector();
        c.SetResult(new TestNodeResult("T", "passed", 1, "T", "M") { Stdout = ["first", "second"] });

        var test = Tests(ReportView.FromCtrf(CtrfWriter.Build(c.BuildAll(), new CtrfRun(Start, Start, "A", null))))[0]!;

        Assert.Equal("first\nsecond", (string)test["standardOutput"]!);
    }
}

public class CtrfReaderTests
{
    private const string Valid = """
        { "reportFormat": "CTRF", "specVersion": "0.2.0",
          "results": { "tool": { "name": "x" }, "summary": { "tests": 1, "passed": 1, "failed": 0, "pending": 0, "skipped": 0, "other": 0, "start": 0, "stop": 1 },
                       "tests": [ { "name": "T", "status": "passed", "duration": 1 } ] } }
        """;

    [Theory]
    [InlineData("{", "is not valid JSON")]
    [InlineData("[]", "must be a JSON object")]
    [InlineData("""{ "reportFormat": "JUnit" }""", "reportFormat")]
    [InlineData("""{ "reportFormat": "CTRF", "specVersion": "0.2.0" }""", "results")]
    [InlineData("""{ "reportFormat": "CTRF", "specVersion": "0.2.0", "results": { "tool": { "name": "x" }, "summary": {} } }""", "results.tests")]
    [InlineData("""{ "reportFormat": "CTRF", "specVersion": "0.2.0", "results": { "tool": { "name": "x" }, "summary": {}, "tests": [ { "status": "passed", "duration": 1 } ] } }""", "results.tests[0].name")]
    [InlineData("""{ "reportFormat": "CTRF", "specVersion": "0.2.0", "results": { "tool": { "name": "x" }, "summary": {}, "tests": [ { "name": "T", "status": "passed" } ] } }""", "results.tests[0].duration")]
    public void Invalid_document_is_rejected_with_the_reason(string json, string reason)
    {
        var ex = Assert.Throws<CtrfFormatException>(() => CtrfReader.Parse(json, "bad.ctrf.json"));

        Assert.StartsWith("bad.ctrf.json: ", ex.Message);
        Assert.Contains(reason, ex.Message);
    }

    [Fact]
    public void Valid_document_is_read()
        => Assert.Equal("T", (string)CtrfReader.Parse(Valid, "ok.ctrf.json").Root["results"]!["tests"]![0]!["name"]!);

    [Fact]
    public void Xunit_ctrf_assembly_comes_from_its_suite_file_path()
    {
        var doc = CtrfReader.Parse(Valid.Replace("\"tests\": [", "\"extra\": { \"suites\": [ { \"filePath\": \"/b/Shop.Tests.dll\" } ] }, \"tests\": ["), "x.ctrf");

        Assert.Equal(("Shop.Tests", false), (doc.AssemblyName, doc.IsJustBdd));
    }
}
