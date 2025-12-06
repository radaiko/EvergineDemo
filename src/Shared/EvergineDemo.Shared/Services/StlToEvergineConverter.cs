using Evergine.Mathematics;
using EvergineDemo.Shared.Models.Stl;

namespace EvergineDemo.Shared.Services;

/// <summary>
/// Converts STL mesh data to Evergine-compatible format
/// </summary>
public static class StlToEvergineConverter
{
    /// <summary>
    /// Convert STL vertices to Evergine Vector3 array
    /// </summary>
    public static Vector3[] GetVertices(StlMesh mesh)
    {
        var vertices = new List<Vector3>();
        
        foreach (var triangle in mesh.Triangles)
        {
            vertices.Add(ConvertVertex(triangle.Vertex1));
            vertices.Add(ConvertVertex(triangle.Vertex2));
            vertices.Add(ConvertVertex(triangle.Vertex3));
        }
        
        return vertices.ToArray();
    }

    /// <summary>
    /// Convert STL normals to Evergine Vector3 array (one per triangle, expanded to per-vertex)
    /// </summary>
    public static Vector3[] GetNormals(StlMesh mesh)
    {
        var normals = new List<Vector3>();
        
        foreach (var triangle in mesh.Triangles)
        {
            var normal = ConvertVertex(triangle.Normal);
            // Each triangle has 3 vertices, so duplicate the normal 3 times
            normals.Add(normal);
            normals.Add(normal);
            normals.Add(normal);
        }
        
        return normals.ToArray();
    }

    /// <summary>
    /// Get triangle indices (simple sequential indices since STL already has individual triangles)
    /// </summary>
    public static uint[] GetIndices(StlMesh mesh)
    {
        int vertexCount = mesh.Triangles.Count * 3;
        var indices = new uint[vertexCount];
        
        for (uint i = 0; i < vertexCount; i++)
        {
            indices[i] = i;
        }
        
        return indices;
    }

    /// <summary>
    /// Calculate bounding box of the mesh
    /// </summary>
    public static (Vector3 Min, Vector3 Max) GetBoundingBox(StlMesh mesh)
    {
        if (mesh.Triangles.Count == 0)
        {
            return (Vector3.Zero, Vector3.Zero);
        }

        float minX = float.MaxValue, minY = float.MaxValue, minZ = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue, maxZ = float.MinValue;

        foreach (var triangle in mesh.Triangles)
        {
            UpdateMinMax(triangle.Vertex1, ref minX, ref minY, ref minZ, ref maxX, ref maxY, ref maxZ);
            UpdateMinMax(triangle.Vertex2, ref minX, ref minY, ref minZ, ref maxX, ref maxY, ref maxZ);
            UpdateMinMax(triangle.Vertex3, ref minX, ref minY, ref minZ, ref maxX, ref maxY, ref maxZ);
        }

        return (new Vector3(minX, minY, minZ), new Vector3(maxX, maxY, maxZ));
    }

    /// <summary>
    /// Get the center point of the mesh
    /// </summary>
    public static Vector3 GetCenter(StlMesh mesh)
    {
        var (min, max) = GetBoundingBox(mesh);
        return (min + max) * 0.5f;
    }

    /// <summary>
    /// Get the size of the mesh bounding box
    /// </summary>
    public static Vector3 GetSize(StlMesh mesh)
    {
        var (min, max) = GetBoundingBox(mesh);
        return max - min;
    }

    private static Vector3 ConvertVertex(StlVertex vertex)
    {
        return new Vector3(vertex.X, vertex.Y, vertex.Z);
    }

    private static void UpdateMinMax(StlVertex vertex, ref float minX, ref float minY, ref float minZ, 
        ref float maxX, ref float maxY, ref float maxZ)
    {
        minX = Math.Min(minX, vertex.X);
        minY = Math.Min(minY, vertex.Y);
        minZ = Math.Min(minZ, vertex.Z);
        maxX = Math.Max(maxX, vertex.X);
        maxY = Math.Max(maxY, vertex.Y);
        maxZ = Math.Max(maxZ, vertex.Z);
    }
}
