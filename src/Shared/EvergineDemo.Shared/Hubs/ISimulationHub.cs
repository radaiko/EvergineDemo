using EvergineDemo.Shared.Models;

namespace EvergineDemo.Shared.Hubs;

/// <summary>
/// SignalR hub interface for simulation updates
/// </summary>
public interface ISimulationHub
{
    /// <summary>
    /// Receive the complete room state update
    /// </summary>
    Task ReceiveRoomState(RoomState roomState);

    /// <summary>
    /// Receive a model state update
    /// </summary>
    Task ReceiveModelUpdate(ModelState modelState);

    /// <summary>
    /// Receive notification that a new model was added
    /// </summary>
    Task ModelAdded(ModelState modelState);

    /// <summary>
    /// Receive notification that a model was removed
    /// </summary>
    Task ModelRemoved(string modelId);
}
