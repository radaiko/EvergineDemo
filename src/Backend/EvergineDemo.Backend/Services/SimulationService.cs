using Microsoft.AspNetCore.SignalR;
using EvergineDemo.Shared.Models;
using EvergineDemo.Shared.Models.Stl;
using EvergineDemo.Shared.Hubs;
using EvergineDemo.Backend.Hubs;
using Evergine.Mathematics;

namespace EvergineDemo.Backend.Services;

/// <summary>
/// Service that manages the 3D simulation state
/// </summary>
public class SimulationService : IHostedService, IDisposable
{
    private readonly IHubContext<SimulationHub, ISimulationHub> _hubContext;
    private readonly ILogger<SimulationService> _logger;
    private Timer? _updateTimer;
    private readonly RoomState _roomState;
    private readonly object _stateLock = new();
    private readonly Dictionary<string, StlMesh> _modelMeshes = new();
    private DateTime _lastBroadcastTime = DateTime.MinValue;
    
    // Physics constants
    private const float Gravity = -9.81f; // m/s^2
    private const float AngularVelocity = MathF.PI / 5f; // 1 rotation per 10 seconds (2π/10 = π/5)
    private const float UpdateFrequency = 60f; // 60 Hz
    private const float DeltaTime = 1f / UpdateFrequency;
    private const float BroadcastFrequency = 6f; // Broadcast updates 6 times per second
    private const double BroadcastInterval = 1.0 / BroadcastFrequency; // ~166ms

    public SimulationService(
        IHubContext<SimulationHub, ISimulationHub> hubContext,
        ILogger<SimulationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
        _roomState = new RoomState
        {
            FloorY = 0f,
            RoomSize = new Vector3(10f, 10f, 10f),
            LastUpdate = DateTime.UtcNow
        };
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Simulation service starting");
        _updateTimer = new Timer(UpdateSimulation, null, TimeSpan.Zero, TimeSpan.FromSeconds(DeltaTime));
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Simulation service stopping");
        _updateTimer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _updateTimer?.Dispose();
    }

    /// <summary>
    /// Get the current room state
    /// </summary>
    public RoomState GetRoomState()
    {
        lock (_stateLock)
        {
            return new RoomState
            {
                Models = _roomState.Models.Select(m => new ModelState
                {
                    Id = m.Id,
                    Position = m.Position,
                    Rotation = m.Rotation,
                    Scale = m.Scale,
                    AngularVelocity = m.AngularVelocity,
                    IsFalling = m.IsFalling,
                    FileName = m.FileName,
                    LastUpdate = m.LastUpdate
                }).ToList(),
                RoomSize = _roomState.RoomSize,
                FloorY = _roomState.FloorY,
                LastUpdate = _roomState.LastUpdate
            };
        }
    }

    /// <summary>
    /// Add a new model to the simulation
    /// </summary>
    /// <param name="fileName">Name of the STL file</param>
    /// <param name="stlMesh">Optional parsed STL mesh data</param>
    public async Task<ModelState> AddModelAsync(string fileName, StlMesh? stlMesh = null)
    {
        var model = new ModelState
        {
            Id = Guid.NewGuid().ToString(),
            Position = new Vector3(0f, 5f, 0f), // Start in the air
            Rotation = Quaternion.Identity,
            Scale = Vector3.One,
            AngularVelocity = AngularVelocity,
            IsFalling = false,
            FileName = fileName,
            LastUpdate = DateTime.UtcNow
        };

        lock (_stateLock)
        {
            _roomState.Models.Add(model);
            _roomState.LastUpdate = DateTime.UtcNow;
            
            // Store STL mesh data if provided
            if (stlMesh != null)
            {
                _modelMeshes[model.Id] = stlMesh;
            }
        }

        if (stlMesh != null)
        {
            _logger.LogInformation("Added new model: {ModelId} - {FileName} with {TriangleCount} triangles", 
                model.Id, fileName, stlMesh.Triangles.Count);
        }
        else
        {
            _logger.LogInformation("Added new model: {ModelId} - {FileName}", model.Id, fileName);
        }
        
        // Notify all clients
        await _hubContext.Clients.All.ModelAdded(model);
        
        return model;
    }

    /// <summary>
    /// Handle a model click (drops the model)
    /// </summary>
    public void HandleModelClick(string modelId)
    {
        lock (_stateLock)
        {
            var model = _roomState.Models.FirstOrDefault(m => m.Id == modelId);
            if (model != null && !model.IsFalling)
            {
                model.IsFalling = true;
                model.LastUpdate = DateTime.UtcNow;
                _logger.LogInformation("Model dropped: {ModelId}", modelId);
            }
        }
    }

    /// <summary>
    /// Remove a model from the simulation
    /// </summary>
    public async Task RemoveModelAsync(string modelId)
    {
        lock (_stateLock)
        {
            var model = _roomState.Models.FirstOrDefault(m => m.Id == modelId);
            if (model != null)
            {
                _roomState.Models.Remove(model);
                _modelMeshes.Remove(modelId); // Clean up mesh data to prevent memory leak
                _roomState.LastUpdate = DateTime.UtcNow;
                _logger.LogInformation("Removed model: {ModelId}", modelId);
            }
        }

        // Notify all clients
        await _hubContext.Clients.All.ModelRemoved(modelId);
    }

    /// <summary>
    /// Get the STL mesh data for a given model ID
    /// </summary>
    public StlMesh? GetModelMesh(string modelId)
    {
        lock (_stateLock)
        {
            return _modelMeshes.TryGetValue(modelId, out var mesh) ? mesh : null;
        }
    }

    /// <summary>
    /// Update simulation state (called on timer)
    /// </summary>
    private void UpdateSimulation(object? state)
    {
        lock (_stateLock)
        {
            bool stateChanged = false;

            foreach (var model in _roomState.Models)
            {
                // Update rotation (spinning)
                var rotationDelta = Quaternion.CreateFromAxisAngle(Vector3.UnitY, model.AngularVelocity * DeltaTime);
                model.Rotation = Quaternion.Multiply(model.Rotation, rotationDelta);
                model.Rotation = Quaternion.Normalize(model.Rotation);
                stateChanged = true;

                // Update falling physics
                if (model.IsFalling)
                {
                    // Simple gravity simulation
                    var velocity = Gravity * DeltaTime;
                    model.Position = new Vector3(
                        model.Position.X,
                        model.Position.Y + velocity,
                        model.Position.Z
                    );

                    // Check floor collision
                    if (model.Position.Y <= _roomState.FloorY)
                    {
                        model.Position = new Vector3(
                            model.Position.X,
                            _roomState.FloorY,
                            model.Position.Z
                        );
                        model.IsFalling = false;
                        _logger.LogInformation("Model landed: {ModelId}", model.Id);
                    }

                    model.LastUpdate = DateTime.UtcNow;
                }
            }

            if (stateChanged)
            {
                _roomState.LastUpdate = DateTime.UtcNow;
            }
        }

        // Broadcast updates to all connected clients (throttled based on time)
        var now = DateTime.UtcNow;
        if ((now - _lastBroadcastTime).TotalSeconds >= BroadcastInterval)
        {
            _lastBroadcastTime = now;
            _ = BroadcastRoomStateAsync();
        }
    }

    /// <summary>
    /// Broadcast the current room state to all clients
    /// </summary>
    private async Task BroadcastRoomStateAsync()
    {
        try
        {
            var roomState = GetRoomState();
            await _hubContext.Clients.All.ReceiveRoomState(roomState);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting room state");
        }
    }
}
