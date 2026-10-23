using Microsoft.Testing.Platform.Builder;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Services;

namespace Davish.JustBdd;

/// <summary>
/// MTP entry point registered automatically by build/Davish.JustBdd.XunitV3.props.
/// Writes scenario reports to the results directory when the report option is enabled.
/// </summary>
public static class TestingPlatformBuilderHook
{
    public static void AddExtensions(ITestApplicationBuilder builder, string[] _)
    {
        builder.CommandLine.AddProvider(() => new JustBddReportOptions());
        var factory = new CompositeExtensionFactory<JustBddReporter>(sp => new JustBddReporter(sp.GetCommandLineOptions(), sp.GetConfiguration()));
        builder.TestHost.AddDataConsumer(factory);
        builder.TestHost.AddTestSessionLifetimeHandler(factory);
    }
}

