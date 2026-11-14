using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OrbitHealth.Api.Data;
using OrbitHealth.Api.Hubs;
using OrbitHealth.Api.Services;

var builder = WebApplication.CreateBuilder(args);

var provider = builder.Configuration.GetValue("Database:Provider", "SqlServer");
var connection = builder.Configuration.GetConnectionString("Default")
    ?? "Server=(localdb)\\MSSQLLocalDB;Database=OrbitHealth;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

builder.Services.AddDbContext<AppDbContext>(opt =>
{
    if (string.Equals(provider, "InMemory", StringComparison.OrdinalIgnoreCase))
        opt.UseInMemoryDatabase("OrbitHealth");
    else
        opt.UseSqlServer(connection);
});

builder.Services.AddSignalR()
    .AddJsonProtocol(o => o.PayloadSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase);

builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddSingleton<MissionState>();
builder.Services.AddHostedService<SimulationService>();

var app = builder.Build();

app.UseCors();
app.UseDefaultFiles();
app.UseStaticFiles(new StaticFileOptions
{
    ServeUnknownFileTypes = true,
    DefaultContentType = "application/octet-stream"
});

app.MapHub<HealthHub>("/hub/health");

app.MapGet("/api/health", (MissionState state) => Results.Ok(new
{
    ok = true,
    time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
    simulate = state.Simulate,
    crew = state.Crew.Count,
    storage = state.DbOk ? state.DbProvider : "InMemory",
    dbOk = state.DbOk
}));

app.MapGet("/api/sensors", (MissionState state) => Results.Ok(state.Sensors));

app.MapGet("/api/state", (MissionState state) => Results.Ok(state.Snapshot()));

app.MapPost("/api/telemetry", async (TelemetryRequest req, HttpContext http, MissionState state,
    IConfiguration config, IHubContext<HealthHub> hub) =>
{
    var key = http.Request.Headers["X-Device-Key"].ToString();
    var expected = config["DeviceKey"] ?? "demo-key-123";
    if (key != expected) return Results.Unauthorized();

    var crew = state.IngestTelemetry(req.CrewId, req.Hr, req.Spo2, req.Temp, req.Sbp, req.Dbp, req.Rr, req.Co2, req.Dose);
    if (crew == null) return Results.NotFound(new { error = "crew not found" });
    await hub.Clients.All.SendAsync("state", state.Snapshot());
    return Results.Ok(new { ok = true, status = crew.Status, risk = crew.Risk });
});

app.MapPost("/api/sensors", async (SensorRequest req, MissionState state, IHubContext<HealthHub> hub) =>
{
    var st = state.AddSensor(req.CrewId, req.Kind, req.Metrics ?? new(), req.Note);
    if (st == null) return Results.BadRequest(new { error = "unknown crew or sensor kind" });
    await hub.Clients.All.SendAsync("state", state.Snapshot());
    return Results.Ok(st);
});

app.MapPost("/api/symptoms", async (SymptomRequest req, MissionState state, IHubContext<HealthHub> hub) =>
{
    var entry = state.AddSymptom(req.CrewId, req.Symptom, req.Severity, req.Source ?? "manual", req.Note ?? "");
    if (entry == null) return Results.BadRequest(new { error = "unknown crew or symptom" });
    await hub.Clients.All.SendAsync("state", state.Snapshot());
    return Results.Ok(new { ok = true, entry });
});

app.MapDelete("/api/symptoms/{crewId}/{symptom}", async (string crewId, string symptom,
    MissionState state, IHubContext<HealthHub> hub) =>
{
    var removed = state.ClearSymptom(crewId, symptom);
    await hub.Clients.All.SendAsync("state", state.Snapshot());
    return Results.Ok(new { ok = true, removed });
});

app.MapGet("/api/reports", (MissionState state) => Results.Ok(state.Reports));

app.MapPost("/api/reports", async (string? scope, MissionState state, IHubContext<HealthHub> hub) =>
{
    try
    {
        var rep = state.CreateReport(scope ?? "mission");
        await hub.Clients.All.SendAsync("state", state.Snapshot());
        return Results.Ok(rep);
    }
    catch (ArgumentException e)
    {
        return Results.NotFound(new { error = e.Message });
    }
});

app.MapPost("/api/reports/{id}/send", async (string id, MissionState state, IHubContext<HealthHub> hub) =>
{
    var rep = state.SendReport(id);
    if (rep == null) return Results.BadRequest(new { error = "report not found or already sent" });
    await hub.Clients.All.SendAsync("state", state.Snapshot());
    return Results.Ok(new { ok = true, eta = rep.Eta, delay = state.CommDelay });
});

app.MapPost("/api/reports/{id}/ack", async (string id, MissionState state, IHubContext<HealthHub> hub) =>
{
    var rep = state.AckReport(id);
    if (rep == null) return Results.BadRequest(new { error = "report not received yet" });
    await hub.Clients.All.SendAsync("state", state.Snapshot());
    return Results.Ok(rep);
});

app.MapPost("/api/alerts/ack", async (MissionState state, IHubContext<HealthHub> hub) =>
{
    foreach (var a in state.Alerts) a.Ack = true;
    await hub.Clients.All.SendAsync("state", state.Snapshot());
    return Results.Ok(new { ok = true });
});

app.MapPost("/api/demo/scenario/{name}", async (string name, MissionState state, IHubContext<HealthHub> hub) =>
{
    try
    {
        state.Scenario(name);
        await hub.Clients.All.SendAsync("state", state.Snapshot());
        return Results.Ok(new { ok = true });
    }
    catch (ArgumentException e)
    {
        return Results.BadRequest(new { error = e.Message });
    }
});

app.MapPost("/api/demo/simulate/{enabled}", async (bool enabled, MissionState state, IHubContext<HealthHub> hub) =>
{
    state.Simulate = enabled;
    await hub.Clients.All.SendAsync("state", state.Snapshot());
    return Results.Ok(new { ok = true, simulate = state.Simulate });
});

app.MapPost("/api/demo/auto/{enabled}", async (bool enabled, MissionState state, IHubContext<HealthHub> hub) =>
{
    state.Auto = enabled;
    await hub.Clients.All.SendAsync("state", state.Snapshot());
    return Results.Ok(new { ok = true, auto = state.Auto });
});

if (Directory.Exists(app.Environment.WebRootPath))
{
    app.MapFallbackToFile("index.html");
}

app.Run();

public record TelemetryRequest(string CrewId, double? Hr, double? Spo2, double? Temp,
    double? Sbp, double? Dbp, double? Rr, double? Co2, double? Dose);

public record SensorRequest(string CrewId, string Kind, Dictionary<string, double>? Metrics, string? Note);

public record SymptomRequest(string CrewId, string Symptom, int Severity, string? Source, string? Note);
