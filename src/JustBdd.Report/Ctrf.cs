using System.Reflection;

namespace Davish.JustBdd;

/// <summary>Common Test Report Format (https://github.com/ctrf-io/ctrf) constants shared by the writer and the readers.</summary>
internal static class Ctrf
{
    public const string SpecVersion = "0.2.0";

    /// <summary>CTRF rejects unknown properties outside <c>extra</c> objects; Davish.JustBdd data lives under this key.</summary>
    public const string ExtensionKey = "davish.justbdd";

    /// <summary>The assembly's informational version without the source revision suffix.</summary>
    public static string Version(Assembly assembly)
    {
        var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0";
        var plus = version.IndexOf('+');
        return plus < 0 ? version : version.Substring(0, plus);
    }
}
