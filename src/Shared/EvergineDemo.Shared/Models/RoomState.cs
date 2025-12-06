using Evergine.Mathematics;

namespace EvergineDemo.Shared.Models;

/// <summary>
/// Represents the state of the 3D room simulation
/// </summary>
public class RoomState
{
    /// <summary>
    /// List of models in the room
    /// </summary>
    public List<ModelState> Models { get; set; } = new();

    /// <summary>
    /// Room dimensions
    /// </summary>
    public Vector3 RoomSize { get; set; } = new Vector3(10f, 10f, 10f);

    /// <summary>
    /// Floor Y position
    /// </summary>
    public float FloorY { get; set; } = 0f;

    /// <summary>
    /// Timestamp of last update
    /// </summary>
    public DateTime LastUpdate { get; set; }
}
