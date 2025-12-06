namespace EvergineDemo.Shared.Models.Stl;

/// <summary>
/// Represents a triangle in an STL file
/// </summary>
public class StlTriangle
{
    /// <summary>
    /// Normal vector of the triangle
    /// </summary>
    public StlVertex Normal { get; set; }

    /// <summary>
    /// First vertex of the triangle
    /// </summary>
    public StlVertex Vertex1 { get; set; }

    /// <summary>
    /// Second vertex of the triangle
    /// </summary>
    public StlVertex Vertex2 { get; set; }

    /// <summary>
    /// Third vertex of the triangle
    /// </summary>
    public StlVertex Vertex3 { get; set; }

    /// <summary>
    /// Attribute byte count (binary STL only)
    /// </summary>
    public ushort AttributeByteCount { get; set; }
}
