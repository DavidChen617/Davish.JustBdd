using System.Text.Json.Nodes;

namespace Davish.JustBdd.Tests;

public class ReportViewTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 24, 3, 0, 0, TimeSpan.Zero);

    private static async Task<JsonObject> Ctrf()
    {
        var c = new ReportCollector();
        c.AddScenario("T.Rename", await new Scenario("Rename").Given("a user", () => "David").Then("named", n => Assert.Equal("David", n)).RunAsync());
        c.SetResult(new TestNodeResult("T.Rename", "errored", 12.6, "T", "Rename")
        {
            Traits = [new("Category", "Smoke"), new("Owner", "team-a"), new("Owner", "team-b")],
        });
        return CtrfWriter.Build(c.BuildAll(), new CtrfRun(Start, Start.AddSeconds(1), "Shop.Tests", null));
    }

    [Fact]
    public async Task Parsed_ctrf_converts_like_an_in_memory_one()
    {
        var ctrf = await Ctrf();

        var parsed = JsonNode.Parse(ctrf.ToJsonString())!.AsObject();

        Assert.Equal(ReportView.FromCtrf(ctrf).ToJsonString(), ReportView.FromCtrf(parsed).ToJsonString());
    }

    [Fact]
    public async Task Ctrf_fields_become_template_data()
    {
        var view = ReportView.FromCtrf(await Ctrf());

        var test = view["tests"]![0]!;
        Assert.Equal(("T.Rename", "errored", 13d, "T", "Rename", "Shop.Tests"), ((string)test["displayName"]!, (string)test["outcome"]!,
            (double)test["durationMs"]!, (string)test["className"]!, (string)test["methodName"]!, (string)test["assembly"]!));
        Assert.Equal([("Category", "Smoke"), ("Owner", "team-a"), ("Owner", "team-b")],
            test["traits"]!.AsArray().Select(t => ((string)t!["key"]!, (string)t["value"]!)));
        Assert.Equal("Rename", (string)test["scenarios"]![0]!["description"]!);
        Assert.Equal((1, 1), ((int)view["summary"]!["total"]!, (int)view["summary"]!["errored"]!));
        Assert.Equal(Start.ToString("O"), (string)view["startTime"]!);
    }

    [Fact]
    public void Ctrf_from_other_producers_falls_back_to_core_fields()
    {
        var ctrf = JsonNode.Parse("""
            {
              "reportFormat": "CTRF", "specVersion": "0.0.0",
              "results": {
                "tool": { "name": "xUnit.net v3" },
                "summary": { "tests": 1, "passed": 0, "failed": 1, "skipped": 0, "pending": 0, "other": 0, "start": 0, "stop": 5 },
                "tests": [ { "name": "Shop.CartTests.Adds", "status": "failed", "duration": 5, "suite": ["Shop", "Shop.CartTests"] } ]
              }
            }
            """)!.AsObject();

        var test = ReportView.FromCtrf(ctrf)["tests"]![0]!;

        Assert.Equal(("failed", "Shop.CartTests"), ((string)test["outcome"]!, (string)test["className"]!));
        Assert.Empty(test["scenarios"]!.AsArray());
        Assert.Null(test["assembly"]);
    }
}
