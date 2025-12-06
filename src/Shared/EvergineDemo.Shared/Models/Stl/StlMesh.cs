namespace EvergineDemo.Shared.Models.Stl;

/// <summary>
/// Represents a complete STL mesh
/// </summary>
public class StlMesh
{
    /// <summary>
    /// Name of the mesh (from ASCII STL or generated for binary)
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// List of triangles that make up the mesh
    /// </summary>
    public List<StlTriangle> Triangles { get; set; } = new();

    /// <summary>
    /// Gets all unique vertices from the mesh
    /// </summary>
    public IEnumerable<StlVertex> GetVertices()
    {
        foreach (var triangle in Triangles)
        {
            yield return triangle.Vertex1;
            yield return triangle.Vertex2;
            yield return triangle.Vertex3;
        }
    }

    /// <summary>
    /// Gets all normals from the mesh
    /// </summary>
    public IEnumerable<StlVertex> GetNormals()
    {
        return Triangles.Select(t => t.Normal);
    }
}
