using Microsoft.AspNetCore.SignalR;
using OrbitHealth.Api.Services;

namespace OrbitHealth.Api.Hubs;

public class HealthHub(MissionState state) : Hub
{
    public override async Task OnConnectedAsync()
    {
        await Clients.Caller.SendAsync("state", state.Snapshot());
        await base.OnConnectedAsync();
    }

    public Task Ping() => Clients.Caller.SendAsync("pong", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
}

public class SimulationService(MissionState state, IHubContext<HealthHub> hub, ILogger<SimulationService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                if (state.Simulate)
                {
                    state.Tick();
                    await hub.Clients.All.SendAsync("state", state.Snapshot(), stoppingToken);
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "simulation tick failed");
            }
        }
    }
}
