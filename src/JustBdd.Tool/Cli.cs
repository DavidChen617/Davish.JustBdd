using System.CommandLine;

namespace Davish.JustBdd.Tool;

internal static class Cli
{
    public static RootCommand Build(TextWriter output, TextWriter error, string currentDirectory)
    {
        var inputs = new Argument<string[]>("inputs")
        {
            Description = $"CTRF files, folders, or file name wildcards. A folder contributes its {string.Join(" and ", HtmlCommand.Patterns)} files. Defaults to {HtmlCommand.DefaultInput}.",
            Arity = ArgumentArity.ZeroOrMore,
        };
        var outputFile = new Option<string>("--output", "-o")
        {
            Description = $"The HTML file to write. Defaults to {HtmlCommand.DefaultOutput} in the first input's folder.",
        };

        var html = new Command("html", "Render the CTRF files of several test projects into one HTML report.") { inputs, outputFile };
        html.SetAction(result => HtmlCommand.Run(result.GetValue(inputs) ?? [], result.GetValue(outputFile), currentDirectory, output, error));

        return new RootCommand("Davish.JustBdd reports.") { html };
    }
}
