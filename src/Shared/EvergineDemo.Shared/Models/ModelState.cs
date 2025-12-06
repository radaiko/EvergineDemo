using Evergine.Mathematics;

namespace EvergineDemo.Shared.Models;

/// <summary>
/// Represents the state of a 3D model in the simulation
/// </summary>
public class ModelState
{
    /// <summary>
    /// Unique identifier for the model
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Position of the model in 3D space
    /// </summary>
    public Vector3 Position { get; set; }

    /// <summary>
    /// Rotation of the model (quaternion)
    /// </summary>
    public Quaternion Rotation { get; set; }

    /// <summary>
    /// Scale of the model
    /// </summary>
    public Vector3 Scale { get; set; } = Vector3.One;

    /// <summary>
    /// Angular velocity for spinning (radians per second)
    /// </summary>
    public float AngularVelocity { get; set; }

    /// <summary>
    /// Whether the model is currently falling
    /// </summary>
    public bool IsFalling { get; set; }

    /// <summary>
    /// File name of the STL model
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp of last update
    /// </summary>
    public DateTime LastUpdate { get; set; }
}
