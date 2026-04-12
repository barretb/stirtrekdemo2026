using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace StirTrekDemo.Web.Services;

public sealed class FrontendTelemetry : IDisposable
{
    public const string SourceName = "StirTrekDemo.Web";

    public ActivitySource ActivitySource { get; } = new(SourceName);
    public Meter Meter { get; } = new(SourceName);

    public Counter<long> MissionLaunches { get; }
    public Counter<long> PageViews { get; }
    public Histogram<double> ApiCallDuration { get; }

    public FrontendTelemetry()
    {
        MissionLaunches = Meter.CreateCounter<long>(
            "ui.mission.launches",
            description: "Number of mission launches initiated from the UI");

        PageViews = Meter.CreateCounter<long>(
            "ui.page.views",
            description: "Number of page views in the UI");

        ApiCallDuration = Meter.CreateHistogram<double>(
            "ui.api.call.duration",
            unit: "ms",
            description: "Duration of API calls from the UI");
    }

    public void Dispose()
    {
        ActivitySource.Dispose();
        Meter.Dispose();
    }
}
