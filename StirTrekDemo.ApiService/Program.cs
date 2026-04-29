using StirTrekDemo.ApiService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

// Register Mission Control services as singletons
builder.Services.AddSingleton<MissionTelemetryService>();
builder.Services.AddSingleton<MissionService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/", () => "Space Mission Control API is online. See /api/missions to list missions.");

// ── Mission endpoints ────────────────────────────────────────────────────────

app.MapGet("/api/missions", (MissionService missions) =>
    Results.Ok(missions.GetAllMissions()))
    .WithName("GetMissions");

app.MapGet("/api/missions/{id}", (string id, MissionService missions) =>
{
    var mission = missions.GetMission(id);
    return mission is not null ? Results.Ok(mission) : Results.NotFound(new { error = $"Mission '{id}' not found." });
})
.WithName("GetMission");

app.MapPost("/api/missions/{id}/launch", async (string id, MissionService missions, bool forceFailure = false) =>
{
    var result = await missions.LaunchMissionAsync(id, forceFailure);
    return result.Success ? Results.Ok(result) : Results.UnprocessableEntity(result);
})
.WithName("LaunchMission");

app.MapGet("/api/missions/{id}/telemetry", (string id, MissionService missions) =>
{
    if (missions.GetMission(id) is null)
        return Results.NotFound(new { error = $"Mission '{id}' not found." });

    var reading = missions.GetTelemetry(id);
    return Results.Ok(reading);
})
.WithName("GetMissionTelemetry");

app.MapPost("/api/missions/reset", (MissionService missions) =>
{
    var count = missions.ResetMissions();
    return Results.Ok(new { reset = count, message = "All missions reset to Preparing." });
})
.WithName("ResetMissions");

// Diagnostics endpoint — shows W3C Baggage received from the frontend
app.MapGet("/api/diagnostics/baggage", (MissionService missions) =>
    Results.Ok(missions.GetCurrentBaggage()))
    .WithName("GetBaggage");

// ────────────────────────────────────────────────────────────────────────────

app.MapDefaultEndpoints();

app.Run();
