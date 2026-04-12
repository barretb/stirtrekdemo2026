using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace StirTrekDemo.ApiService.Services;

/// <summary>
/// Owns the custom ActivitySource and Meter instruments for the Mission Control demo.
/// Registered as a singleton so all instrumentation shares the same source/meter.
/// </summary>
public sealed class MissionTelemetryService : IDisposable
{
    public const string SourceName = "StirTrekDemo.ApiService";

    public readonly ActivitySource ActivitySource = new(SourceName);
    public readonly Meter Meter;

    public readonly Counter<long> LaunchCounter;
    public readonly Counter<long> SuccessCounter;
    public readonly UpDownCounter<int> ActiveMissions;
    public readonly Histogram<double> LaunchDuration;

    private double _warpCoreTemp = 2500.0;
    private readonly Random _random = Random.Shared;

    // Per-mission state for observable gauges
    private readonly Dictionary<string, double> _fuelLevels = new();
    private readonly Dictionary<string, double> _shieldStrengths = new();

    public MissionTelemetryService()
    {
        Meter = new Meter(SourceName);

        LaunchCounter = Meter.CreateCounter<long>(
            "missions.launches.total",
            description: "Total number of mission launches attempted");

        SuccessCounter = Meter.CreateCounter<long>(
            "missions.launches.succeeded",
            description: "Total successful mission launches");

        ActiveMissions = Meter.CreateUpDownCounter<int>(
            "missions.active",
            description: "Number of currently active missions");

        LaunchDuration = Meter.CreateHistogram<double>(
            "missions.launch.duration",
            unit: "ms",
            description: "Duration of mission launch sequence in milliseconds");

        // Observable gauge — slowly drifts so the dashboard shows live movement
        Meter.CreateObservableGauge<double>(
            "missions.warpcore.temperature",
            GetAverageWarpCoreTemp,
            unit: "celsius",
            description: "Average warp core temperature across active missions");

        // Observable gauges for per-mission fuel and shield
        Meter.CreateObservableGauge<double>(
            "missions.fuel.level",
            () => _fuelLevels.Select(kv => new Measurement<double>(kv.Value, new KeyValuePair<string, object?>("mission.id", kv.Key))),
            unit: "%",
            description: "Fuel level percentage per mission");

        Meter.CreateObservableGauge<double>(
            "missions.shield.strength",
            () => _shieldStrengths.Select(kv => new Measurement<double>(kv.Value, new KeyValuePair<string, object?>("mission.id", kv.Key))),
            unit: "%",
            description: "Shield strength percentage per mission");
    }

    private double GetAverageWarpCoreTemp()
    {
        _warpCoreTemp += (_random.NextDouble() - 0.48) * 50.0;
        _warpCoreTemp = Math.Clamp(_warpCoreTemp, 2000.0, 3500.0);
        return Math.Round(_warpCoreTemp, 1);
    }

    public void UpdateFuelLevel(string missionId, double value)
    {
        _fuelLevels[missionId] = value;
    }

    public void UpdateShieldStrength(string missionId, double value)
    {
        _shieldStrengths[missionId] = value;
    }

    public void Dispose()
    {
        ActivitySource.Dispose();
        Meter.Dispose();
    }
}
