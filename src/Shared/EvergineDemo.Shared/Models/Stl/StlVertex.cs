namespace EvergineDemo.Shared.Models.Stl;

/// <summary>
/// Represents a vertex in 3D space
/// </summary>
public struct StlVertex
{
    /// <summary>
    /// X coordinate
    /// </summary>
    public float X { get; set; }

    /// <summary>
    /// Y coordinate
    /// </summary>
    public float Y { get; set; }

    /// <summary>
    /// Z coordinate
    /// </summary>
    public float Z { get; set; }

    public StlVertex(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }
}
