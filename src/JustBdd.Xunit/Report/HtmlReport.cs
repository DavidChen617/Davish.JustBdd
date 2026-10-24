namespace Davish.JustBdd;

/// <summary>Renders the single-project HTML report from the same CTRF data the merge tool reads.</summary>
internal static class HtmlReport
{
    public static string Render(IReadOnlyList<ReportTest> tests, DateTimeOffset start, DateTimeOffset stop,
        string? assemblyName = null, string? targetFramework = null)
    {
        var ctrf = CtrfWriter.Build(tests, new CtrfRun(start, stop, assemblyName, targetFramework));
        return HtmlWriter.Render(ReportView.FromCtrf(ctrf), Ctrf.Version(typeof(HtmlReport).Assembly));
    }
}
