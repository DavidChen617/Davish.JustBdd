using System.CommandLine;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Davish.JustBdd.Tool.Tests;

public sealed class ToolTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "justbdd-tool-" + Guid.NewGuid().ToString("N"));
    private readonly StringWriter _output = new();
    private readonly StringWriter _error = new();

    public ToolTests() => Directory.CreateDirectory(_root);

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private static string Ctrf(string assembly, params string[] tests) => $$"""
        {
          "reportFormat": "CTRF", "specVersion": "0.2.0",
          "results": {
            "tool": { "name": "xUnit.net v3" },
            "summary": { "tests": {{tests.Length}}, "passed": {{tests.Length}}, "failed": 0, "pending": 0, "skipped": 0, "other": 0, "start": 0, "stop": 10 },
            "tests": [ {{string.Join(", ", tests.Select(t => $$"""{ "name": "{{t}}", "status": "passed", "duration": 1 }"""))}} ],
            "environment": { "extra": { "davish.justbdd": { "assembly": "{{assembly}}" } } }
          }
        }
        """;

    private string Write(string relativePath, string content)
    {
        // GetFullPath normalizes separators, so on Windows "results/x" matches the paths the tool reports.
        var path = Path.GetFullPath(Path.Combine(_root, relativePath));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
        return path;
    }

    private int Run(params string[] args)
        => Cli.Build(_output, _error, _root).Parse(args).Invoke(new InvocationConfiguration { Output = _output, Error = _error });

    private static string[] TestNames(string htmlPath)
    {
        var island = Regex.Match(File.ReadAllText(htmlPath), """<script id="mtp-data" type="application/json">(.*?)</script>""", RegexOptions.Singleline);
        return [.. JsonDocument.Parse(island.Groups[1].Value).RootElement.GetProperty("tests").EnumerateArray()
            .Select(t => t.GetProperty("displayName").GetString()!)];
    }

    [Fact]
    public void Html_command_renders_all_ctrf_files_in_a_folder()
    {
        Write("results/justbdd-Shop.Tests.ctrf.json", Ctrf("Shop.Tests", "Shop.A", "Shop.B"));
        Write("results/someone_2026.ctrf", Ctrf("Billing.Tests", "Billing.C"));
        Write("results/notes.json", "not a report");

        var exit = Run("html", "results", "-o", "out/report.html");

        Assert.Equal(0, exit);
        Assert.Equal(["Billing.C", "Shop.A", "Shop.B"], TestNames(Path.Combine(_root, "out", "report.html")).Order(StringComparer.Ordinal));
        Assert.Contains("3 tests from 2 files", _output.ToString());
    }

    [Fact]
    public void Output_defaults_to_justbdd_html_in_the_first_input_folder()
    {
        Write("results/justbdd-Shop.Tests.ctrf.json", Ctrf("Shop.Tests", "Shop.A"));

        Assert.Equal(0, Run("html", "results"));

        Assert.True(File.Exists(Path.Combine(_root, "results", "justbdd.html")));
    }

    [Fact]
    public void Inputs_default_to_the_test_results_folder()
    {
        Write("TestResults/justbdd-Shop.Tests.ctrf.json", Ctrf("Shop.Tests", "Shop.A"));

        Assert.Equal(0, Run("html"));

        Assert.Equal(["Shop.A"], TestNames(Path.Combine(_root, "TestResults", "justbdd.html")));
    }

    [Fact]
    public void Files_and_wildcards_are_accepted()
    {
        Write("a/justbdd-Shop.Tests.ctrf.json", Ctrf("Shop.Tests", "Shop.A"));
        Write("b/justbdd-Billing.Tests.ctrf.json", Ctrf("Billing.Tests", "Billing.B"));
        Write("b/justbdd-Other.Tests.ctrf.json", Ctrf("Other.Tests", "Other.C"));

        Assert.Equal(0, Run("html", "a/justbdd-Shop.Tests.ctrf.json", "b/justbdd-Bill*.ctrf.json", "-o", "report.html"));

        Assert.Equal(["Billing.B", "Shop.A"], TestNames(Path.Combine(_root, "report.html")).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void Invalid_input_fails_with_a_nonzero_exit_code()
    {
        Write("results/justbdd-Shop.Tests.ctrf.json", Ctrf("Shop.Tests", "Shop.A"));
        var bad = Write("results/broken.ctrf.json", """{ "reportFormat": "CTRF", "specVersion": "0.2.0" }""");

        var exit = Run("html", "results");

        Assert.NotEqual(0, exit);
        Assert.Contains(bad, _error.ToString());
        Assert.Contains("results is missing", _error.ToString());
        Assert.False(File.Exists(Path.Combine(_root, "results", "justbdd.html")));
    }

    [Fact]
    public void No_input_files_fails_with_a_message()
    {
        Directory.CreateDirectory(Path.Combine(_root, "empty"));

        var exit = Run("html", "empty");

        Assert.NotEqual(0, exit);
        Assert.Contains("No CTRF files", _error.ToString());
    }

    [Fact]
    public void Missing_input_path_fails_with_a_message()
    {
        var exit = Run("html", "nowhere");

        Assert.NotEqual(0, exit);
        Assert.Contains("nowhere", _error.ToString());
    }

    [Fact]
    public void Help_names_the_installed_command()
    {
        Assert.Equal(0, Run("html", "--help"));

        Assert.Contains("justbdd html [<inputs>...] [options]", _output.ToString());
    }

    [Fact]
    public void Unknown_option_is_a_parse_error()
        => Assert.NotEqual(0, Run("html", "--nope"));
}
