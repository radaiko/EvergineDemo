using Microsoft.AspNetCore.SignalR;
using EvergineDemo.Shared.Hubs;
using EvergineDemo.Shared.Models;
using EvergineDemo.Backend.Services;

namespace EvergineDemo.Backend.Hubs;

/// <summary>
/// SignalR hub for real-time simulation updates
/// </summary>
public class SimulationHub : Hub<ISimulationHub>
{
    private readonly SimulationService _simulationService;
    private readonly ILogger<SimulationHub> _logger;

    public SimulationHub(SimulationService simulationService, ILogger<SimulationHub> logger)
    {
        _simulationService = simulationService;
        _logger = logger;
    }

    /// <summary>
    /// Called when a client connects
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        
        // Send current room state to the new client
        var roomState = _simulationService.GetRoomState();
        await Clients.Caller.ReceiveRoomState(roomState);
        
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects
    /// </summary>
    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Handle model click from client
    /// </summary>
    public async Task ModelClicked(string modelId)
    {
        _logger.LogInformation("Model clicked: {ModelId}", modelId);
        _simulationService.HandleModelClick(modelId);
    }
}
