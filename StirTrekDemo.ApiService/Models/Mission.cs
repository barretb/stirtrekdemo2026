namespace StirTrekDemo.ApiService.Models;

public record Mission(
    string Id,
    string Name,
    string DestinationPlanet,
    int CrewCount,
    MissionStatus Status,
    DateTimeOffset LaunchDate
);

public enum MissionStatus { Preparing, Launched, InTransit, Complete, Aborted }

public record LaunchResult(
    bool Success,
    string Message,
    DateTimeOffset? LaunchTimestamp,
    string TraceId,
    string SpanId
);

public record TelemetryReading(
    string MissionId,
    double FuelLevel,
    double ShieldStrength,
    double WarpCoreTemp,
    DateTimeOffset Timestamp
);
