using System.Diagnostics;
using System.Net.Http.Json;
using StirTrekDemo.Web.Models;
using StirTrekDemo.Web.Services;

namespace StirTrekDemo.Web;

public class MissionApiClient(HttpClient httpClient, FrontendTelemetry telemetry)
{
    public async Task<Mission[]> GetMissionsAsync(CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            return await httpClient.GetFromJsonAsync<Mission[]>("/api/missions", cancellationToken)
                ?? [];
        }
        finally
        {
            telemetry.ApiCallDuration.Record(sw.Elapsed.TotalMilliseconds,
                new KeyValuePair<string, object?>("endpoint", "GET /api/missions"));
        }
    }

    public async Task<Mission?> GetMissionAsync(string id, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            return await httpClient.GetFromJsonAsync<Mission>($"/api/missions/{id}", cancellationToken);
        }
        finally
        {
            telemetry.ApiCallDuration.Record(sw.Elapsed.TotalMilliseconds,
                new KeyValuePair<string, object?>("endpoint", "GET /api/missions/{id}"));
        }
    }

    public async Task<LaunchResult?> LaunchMissionAsync(
        string id,
        string commanderName,
        string priority,
        bool forceFailure = false,
        CancellationToken cancellationToken = default)
    {
        using var activity = telemetry.ActivitySource.StartActivity("mission.launch.initiated");
        activity?.SetTag("ui.mission.id", id);
        activity?.SetTag("ui.commander.name", commanderName);
        activity?.SetTag("ui.mission.priority", priority);

        // Set W3C Baggage — propagates automatically via HttpClient
        Activity.Current?.SetBaggage("mission.commander", commanderName);
        Activity.Current?.SetBaggage("mission.priority", priority);

        telemetry.MissionLaunches.Add(1,
            new KeyValuePair<string, object?>("mission.id", id),
            new KeyValuePair<string, object?>("mission.priority", priority));

        var sw = Stopwatch.StartNew();
        try
        {
            var response = await httpClient.PostAsync($"/api/missions/{id}/launch{(forceFailure ? "?forceFailure=true" : "")}", null, cancellationToken);
            // Read body regardless of status code (422 = launch failed, still has LaunchResult)
            return await response.Content.ReadFromJsonAsync<LaunchResult>(cancellationToken);
        }
        finally
        {
            telemetry.ApiCallDuration.Record(sw.Elapsed.TotalMilliseconds,
                new KeyValuePair<string, object?>("endpoint", "POST /api/missions/{id}/launch"));
        }
    }

    public async Task<TelemetryReading?> GetTelemetryAsync(string id, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            return await httpClient.GetFromJsonAsync<TelemetryReading>(
                $"/api/missions/{id}/telemetry", cancellationToken);
        }
        finally
        {
            telemetry.ApiCallDuration.Record(sw.Elapsed.TotalMilliseconds,
                new KeyValuePair<string, object?>("endpoint", "GET /api/missions/{id}/telemetry"));
        }
    }

    public async Task<Dictionary<string, string>?> GetBaggageAsync(
        string commanderName = "",
        string priority = "",
        CancellationToken cancellationToken = default)
    {
        using var activity = telemetry.ActivitySource.StartActivity("baggage.inspect");
        
        if (!string.IsNullOrEmpty(commanderName))
        {
            activity?.SetBaggage("mission.commander", commanderName);
        }
        
        if (!string.IsNullOrEmpty(priority))
        {
            activity?.SetBaggage("mission.priority", priority);
        }

        var sw = Stopwatch.StartNew();
        try
        {
            return await httpClient.GetFromJsonAsync<Dictionary<string, string>>(
                "/api/diagnostics/baggage", cancellationToken);
        }
        finally
        {
            telemetry.ApiCallDuration.Record(sw.Elapsed.TotalMilliseconds,
                new KeyValuePair<string, object?>("endpoint", "GET /api/diagnostics/baggage"));
        }
    }

    public async Task<(int Reset, string Message)> ResetMissionsAsync(CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var response = await httpClient.PostAsync("/api/missions/reset", null, cancellationToken);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<ResetResult>(cancellationToken);
            return (result?.Reset ?? 0, result?.Message ?? "Reset complete");
        }
        finally
        {
            telemetry.ApiCallDuration.Record(sw.Elapsed.TotalMilliseconds,
                new KeyValuePair<string, object?>("endpoint", "POST /api/missions/reset"));
        }
    }

    private record ResetResult(int Reset, string Message);
}
