using System.CommandLine;
using Davish.JustBdd.Tool;

return Cli.Build(Console.Out, Console.Error, Environment.CurrentDirectory)
    .Parse(args)
    .Invoke(new InvocationConfiguration { Output = Console.Out, Error = Console.Error });
