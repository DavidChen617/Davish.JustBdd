namespace Davish.JustBdd.Tool;

/// <summary><c>justbdd html</c>: merges CTRF files into one HTML report.</summary>
internal static class HtmlCommand
{
    public const string DefaultInput = "TestResults";
    public const string DefaultOutput = "justbdd.html";

    /// <summary>Davish.JustBdd writes <c>*.ctrf.json</c>; xUnit's <c>--report-xunit-ctrf</c> writes <c>*.ctrf</c>.</summary>
    public static readonly string[] Patterns = ["*.ctrf.json", "*.ctrf"];

    public static int Run(IReadOnlyList<string> inputs, string? output, string currentDirectory, TextWriter stdout, TextWriter stderr)
    {
        if (inputs.Count == 0) inputs = [DefaultInput];

        var files = new List<string>();
        foreach (var input in inputs)
        {
            var found = Resolve(Path.GetFullPath(input, currentDirectory));
            if (found is null)
            {
                stderr.WriteLine($"error: {input} does not exist.");
                return 1;
            }
            files.AddRange(found);
        }
        files = [.. files.Distinct(StringComparer.Ordinal)];
        if (files.Count == 0)
        {
            stderr.WriteLine($"error: No CTRF files ({string.Join(", ", Patterns)}) found in {string.Join(", ", inputs)}.");
            return 1;
        }

        var documents = new List<CtrfDocument>();
        foreach (var file in files)
        {
            try { documents.Add(CtrfReader.Read(file)); }
            catch (CtrfFormatException ex)
            {
                stderr.WriteLine($"error: {ex.Message}");
                return 1;
            }
        }

        var view = ReportView.FromCtrf(documents);
        var path = Path.GetFullPath(output ?? Path.Combine(Folder(Path.GetFullPath(inputs[0], currentDirectory)), DefaultOutput), currentDirectory);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, HtmlWriter.Render(view, Ctrf.Version(typeof(HtmlCommand).Assembly)));

        stdout.WriteLine($"Wrote {path} ({view["tests"]!.AsArray().Count} tests from {documents.Count} files).");
        return 0;
    }

    // A file, a folder (its CTRF files, sorted), or a file name wildcard; null when nothing exists at the path.
    private static IEnumerable<string>? Resolve(string path)
    {
        if (File.Exists(path)) return [path];
        if (Directory.Exists(path)) return Patterns.SelectMany(p => Directory.GetFiles(path, p)).Order(StringComparer.Ordinal);

        var name = Path.GetFileName(path);
        var folder = Path.GetDirectoryName(path);
        if (name.IndexOfAny(['*', '?']) >= 0 && Directory.Exists(folder))
            return Directory.GetFiles(folder, name).Order(StringComparer.Ordinal);
        return null;
    }

    private static string Folder(string input) => Directory.Exists(input) ? input : Path.GetDirectoryName(input)!;
}
