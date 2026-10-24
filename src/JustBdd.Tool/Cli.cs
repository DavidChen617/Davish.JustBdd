using System.CommandLine;
using System.CommandLine.Help;

namespace Davish.JustBdd.Tool;

internal static class Cli
{
    public const string CommandName = "justbdd";

    // RootCommand names itself after the executable (Davish.JustBdd.Tool), so help would not match the installed
    // `dotnet justbdd` command; a plain command with its own help and version options does.
    public static Command Build(TextWriter output, TextWriter error, string currentDirectory)
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

        return new Command(CommandName, "Davish.JustBdd reports.")
        {
            new HelpOption { Recursive = true },
            new VersionOption(),
            html,
        };
    }
}
