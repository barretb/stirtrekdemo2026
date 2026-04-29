using System.Diagnostics;
using OpenTelemetry;
using StirTrekDemo.ApiService.Models;

namespace StirTrekDemo.ApiService.Services;

public class MissionService
{
    private readonly MissionTelemetryService _telemetry;
    private readonly ILogger<MissionService> _logger;
    private readonly Random _random = Random.Shared;

    // In-memory mission store — five Star Trek vessels
    private readonly Dictionary<string, Mission> _missions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["enterprise"] = new("enterprise", "Enterprise NCC-1701", "Alpha Centauri", 430,
            MissionStatus.Preparing, DateTimeOffset.UtcNow.AddDays(7)),
        ["voyager"] = new("voyager", "Voyager NCC-74656", "Delta Quadrant", 141,
            MissionStatus.Preparing, DateTimeOffset.UtcNow.AddDays(14)),
        ["ds9"] = new("ds9", "Deep Space Nine", "Bajoran Wormhole", 2500,
            MissionStatus.Preparing, DateTimeOffset.UtcNow.AddDays(3)),
        ["discovery"] = new("discovery", "Discovery NCC-1031", "Terran Empire", 136,
            MissionStatus.Preparing, DateTimeOffset.UtcNow.AddDays(21)),
        ["defiant"] = new("defiant", "Defiant NX-74205", "Gamma Quadrant", 50,
            MissionStatus.Preparing, DateTimeOffset.UtcNow.AddDays(1)),
    };

    // Tracks whether the Defiant has already had its "guaranteed" first-attempt failure
    private readonly HashSet<string> _guaranteedFailureFired = new(StringComparer.OrdinalIgnoreCase);

    public MissionService(MissionTelemetryService telemetry, ILogger<MissionService> logger)
    {
        _telemetry = telemetry;
        _logger = logger;
    }

    public IEnumerable<Mission> GetAllMissions() => _missions.Values;

    public Mission? GetMission(string id) =>
        _missions.TryGetValue(id, out var m) ? m : null;

    public async Task<LaunchResult> LaunchMissionAsync(string id, bool forceFailure = false)
    {
        if (!_missions.TryGetValue(id, out var mission))
            return new LaunchResult(false, $"Mission '{id}' not found.", null, "", "");

        // Read W3C Baggage propagated from the frontend
        var commander = Baggage.Current.GetBaggage("mission.commander") ?? "Unknown Commander";
        var priority = Baggage.Current.GetBaggage("mission.priority") ?? "Normal";

        _telemetry.LaunchCounter.Add(1,
            new TagList { { "mission.id", id }, { "mission.destination", mission.DestinationPlanet } });

        var sw = Stopwatch.StartNew();

        // Parent span — encompasses the entire launch sequence
        using var parentSpan = _telemetry.ActivitySource.StartActivity("mission.launch");
        parentSpan?.SetTag("mission.id", id);
        parentSpan?.SetTag("mission.name", mission.Name);
        parentSpan?.SetTag("mission.destination", mission.DestinationPlanet);
        parentSpan?.SetTag("mission.crew_count", mission.CrewCount);
        parentSpan?.SetTag("baggage.mission.commander", commander);
        parentSpan?.SetTag("baggage.mission.priority", priority);

        parentSpan?.AddEvent(new ActivityEvent("launch.initiated",
            tags: new ActivityTagsCollection
            {
                { "commander", commander },
                { "priority", priority },
                { "crew_count", mission.CrewCount }
            }));

        _logger.LogInformation(
            "Mission launch initiated {MissionId} {MissionName} {Destination} {CrewCount}",
            id, mission.Name, mission.DestinationPlanet, mission.CrewCount);
        _logger.LogInformation(
            "Launch authorized by commander {Commander} with priority {Priority}",
            commander, priority);

        // Determine failure up-front: forceFailure param overrides all other logic
        bool shouldFail;
        if (forceFailure)
        {
            shouldFail = true;
        }
        else if (id.Equals("defiant", StringComparison.OrdinalIgnoreCase)
            && !_guaranteedFailureFired.Contains(id))
        {
            shouldFail = true;
            _guaranteedFailureFired.Add(id);
        }
        else
        {
            shouldFail = _random.NextDouble() < 0.10;
        }

        // Pre-select which phase fails (if any) so the span hierarchy looks authentic
        string? failPhase = null;
        string? failReason = null;
        if (shouldFail)
        {
            (failPhase, failReason) = _random.Next(4) switch
            {
                0 => ("preflight-check", "Navigation array calibration failure"),
                1 => ("fuel-validation", "Dilithium crystal matrix destabilized"),
                2 => ("crew-boarding", "Transporter malfunction during crew boarding"),
                _ => ("systems-initialization", "Warp drive plasma injector failure"),
            };
        }

        // ── Phase 1: Preflight check ────────────────────────────────────────
        using (var span = _telemetry.ActivitySource.StartActivity("mission.preflight-check"))
        {
            span?.SetTag("mission.id", id);
            var duration = _random.Next(50, 150);
            await Task.Delay(duration);
            _logger.LogDebug("Preflight check complete for {MissionId}, duration: {DurationMs}ms", id, duration);
            span?.SetTag("check.duration_ms", duration);
            span?.SetTag("check.result", failPhase == "preflight-check" ? "fail" : "pass");
        }

        // ── Phase 2: Fuel validation ────────────────────────────────────────
        using (var span = _telemetry.ActivitySource.StartActivity("mission.fuel-validation"))
        {
            span?.SetTag("mission.id", id);
            var duration = _random.Next(30, 80);
            await Task.Delay(duration);
            _logger.LogDebug("Fuel validation complete for {MissionId}, duration: {DurationMs}ms", id, duration);
            span?.SetTag("check.duration_ms", duration);
            span?.SetTag("check.result", failPhase == "fuel-validation" ? "fail" : "pass");
        }

        // ── Phase 3: Crew boarding ──────────────────────────────────────────
        using (var span = _telemetry.ActivitySource.StartActivity("mission.crew-boarding"))
        {
            span?.SetTag("mission.id", id);
            span?.SetTag("crew.count", mission.CrewCount);
            var duration = _random.Next(100, 200);
            await Task.Delay(duration);
            _logger.LogDebug("Crew boarding complete for {MissionId}, duration: {DurationMs}ms", id, duration);
            span?.SetTag("check.duration_ms", duration);
            span?.SetTag("check.result", failPhase == "crew-boarding" ? "fail" : "pass");
        }

        // ── Phase 4: Systems initialization ────────────────────────────────
        using (var span = _telemetry.ActivitySource.StartActivity("mission.systems-initialization"))
        {
            span?.SetTag("mission.id", id);
            var duration = _random.Next(50, 100);
            await Task.Delay(duration);
            _logger.LogDebug("Systems initialization complete for {MissionId}, duration: {DurationMs}ms", id, duration);
            span?.SetTag("check.duration_ms", duration);
            span?.SetTag("check.result", failPhase == "systems-initialization" ? "fail" : "pass");
        }

        sw.Stop();
        _telemetry.LaunchDuration.Record(sw.Elapsed.TotalMilliseconds,
            new TagList { { "mission.id", id }, { "success", !shouldFail } });

        // ── Failure path ────────────────────────────────────────────────────
        if (shouldFail && failPhase is not null)
        {
            parentSpan?.SetStatus(ActivityStatusCode.Error, $"Launch failed during {failPhase}: {failReason}");
            parentSpan?.SetTag("error", true);
            parentSpan?.SetTag("failure.phase", failPhase);
            parentSpan?.SetTag("failure.reason", failReason);

            _missions[id] = mission with { Status = MissionStatus.Aborted };
            _telemetry.ActiveMissions.Add(-1, new TagList { { "mission.id", id } });

            _logger.LogError("Mission {MissionId} launch FAILED during {Phase}: {Reason}",
                id, failPhase, failReason);

            return new LaunchResult(
                false,
                $"Launch aborted: {failReason}",
                null,
                parentSpan?.TraceId.ToString() ?? "",
                parentSpan?.SpanId.ToString() ?? "");
        }

        // ── Success path ────────────────────────────────────────────────────
        parentSpan?.SetStatus(ActivityStatusCode.Ok);
        parentSpan?.AddEvent(new ActivityEvent("launch.complete",
            tags: new ActivityTagsCollection
            {
                { "duration_ms", (long)sw.Elapsed.TotalMilliseconds }
            }));

        var now = DateTimeOffset.UtcNow;
        _missions[id] = mission with { Status = MissionStatus.Launched, LaunchDate = now };
        _telemetry.SuccessCounter.Add(1,
            new TagList { { "mission.id", id }, { "mission.destination", mission.DestinationPlanet } });
        _telemetry.ActiveMissions.Add(1, new TagList { { "mission.id", id } });

        _logger.LogInformation(
            "Mission {MissionId} {MissionName} successfully launched to {Destination}. TraceId: {TraceId}",
            id, mission.Name, mission.DestinationPlanet, parentSpan?.TraceId.ToString());

        return new LaunchResult(
            true,
            $"Mission '{mission.Name}' successfully launched to {mission.DestinationPlanet}!",
            now,
            parentSpan?.TraceId.ToString() ?? "",
            parentSpan?.SpanId.ToString() ?? "");
    }

    public TelemetryReading GetTelemetry(string id)
    {
        using var span = _telemetry.ActivitySource.StartActivity("mission.telemetry-read");
        span?.SetTag("mission.id", id);
        span?.SetTag("readings.count", 1);

        var reading = new TelemetryReading(
            id,
            Math.Round(_random.NextDouble() * 100, 1),
            Math.Round(_random.NextDouble() * 100, 1),
            Math.Round(2000 + _random.NextDouble() * 1500, 1),
            DateTimeOffset.UtcNow);

        // Push readings to telemetry service for observable gauges
        _telemetry.UpdateFuelLevel(id, reading.FuelLevel);
        _telemetry.UpdateShieldStrength(id, reading.ShieldStrength);

        _logger.LogDebug(
            "Telemetry read for {MissionId}: fuel={FuelLevel:F1}% shields={ShieldStrength:F1}%",
            id, reading.FuelLevel, reading.ShieldStrength);

        return reading;
    }

    public IReadOnlyDictionary<string, string> GetCurrentBaggage() =>
        Baggage.Current.GetBaggage();

    public int ResetMissions()
    {
        int count = 0;
        var now = DateTimeOffset.UtcNow;

        // Reset Enterprise
        if (_missions.ContainsKey("enterprise"))
        {
            _missions["enterprise"] = new Mission("enterprise", "Enterprise NCC-1701", "Alpha Centauri", 430,
                MissionStatus.Preparing, now.AddDays(7));
            count++;
        }

        // Reset Voyager
        if (_missions.ContainsKey("voyager"))
        {
            _missions["voyager"] = new Mission("voyager", "Voyager NCC-74656", "Delta Quadrant", 141,
                MissionStatus.Preparing, now.AddDays(14));
            count++;
        }

        // Reset DS9
        if (_missions.ContainsKey("ds9"))
        {
            _missions["ds9"] = new Mission("ds9", "Deep Space Nine", "Bajoran Wormhole", 2500,
                MissionStatus.Preparing, now.AddDays(3));
            count++;
        }

        // Reset Discovery
        if (_missions.ContainsKey("discovery"))
        {
            _missions["discovery"] = new Mission("discovery", "Discovery NCC-1031", "Terran Empire", 136,
                MissionStatus.Preparing, now.AddDays(21));
            count++;
        }

        // Reset Defiant
        if (_missions.ContainsKey("defiant"))
        {
            _missions["defiant"] = new Mission("defiant", "Defiant NX-74205", "Gamma Quadrant", 50,
                MissionStatus.Preparing, now.AddDays(1));
            count++;
        }

        // Clear the guaranteed failure tracker so Defiant will fail again on next launch
        _guaranteedFailureFired.Clear();

        _logger.LogInformation("Missions reset: {Count} missions returned to Preparing", count);

        return count;
    }
}
