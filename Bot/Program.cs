using System;
using Microsoft.ApplicationInsights.DependencyCollector;
using Microsoft.ApplicationInsights.DiagnosticSourceListener;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using RightpointLabs.BotLib;

namespace RightpointLabs.ConferenceRoom.Bot
{
    public class Program
    {
        public static void Main(string[] args)
        {
            TelemetryConfiguration.Active.InstrumentationKey = Config.GetAppSetting("APPINSIGHTS_INSTRUMENTATIONKEY") ??
                                                               Config.GetAppSetting("BotDevAppInsightsKey");

            new DependencyTrackingTelemetryModule().Initialize(TelemetryConfiguration.Active);
            new DiagnosticSourceTelemetryModule().Initialize(TelemetryConfiguration.Active);
            TelemetryConfiguration.Active.TelemetryInitializers.Add(new OperationCorrelationTelemetryInitializer());
            TelemetryConfiguration.Active.TelemetryInitializers.Add(new HttpDependenciesParsingTelemetryInitializer());

            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .UseApplicationInsights(Config.GetAppSetting("APPINSIGHTS_INSTRUMENTATIONKEY") ??
                                        Config.GetAppSetting("BotDevAppInsightsKey"))
                .Build();
    }
}
