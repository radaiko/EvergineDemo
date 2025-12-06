using System.Text;
using EvergineDemo.Shared.Services;
using EvergineDemo.Shared.Models.Stl;
using Xunit;

namespace EvergineDemo.Shared.Tests.Services;

public class StlParserServiceTests
{
    private readonly StlParserService _parser;

    public StlParserServiceTests()
    {
        _parser = new StlParserService();
    }

    #region Binary STL Tests

    [Fact]
    public void Parse_BinaryStl_SingleTriangle_Success()
    {
        // Arrange - Create a simple binary STL with one triangle
        byte[] stlData = CreateBinaryStl(1, new[]
        {
            new Triangle(
                new Vertex(0, 0, 1),    // Normal
                new Vertex(0, 0, 0),    // Vertex 1
                new Vertex(1, 0, 0),    // Vertex 2
                new Vertex(0, 1, 0)     // Vertex 3
            )
        });

        // Act
        var mesh = _parser.Parse(stlData, "test.stl");

        // Assert
        Assert.NotNull(mesh);
        Assert.Equal("test.stl", mesh.Name);
        Assert.Single(mesh.Triangles);
        
        var triangle = mesh.Triangles[0];
        Assert.Equal(0, triangle.Normal.X);
        Assert.Equal(0, triangle.Normal.Y);
        Assert.Equal(1, triangle.Normal.Z);
        
        Assert.Equal(0, triangle.Vertex1.X);
        Assert.Equal(1, triangle.Vertex2.X);
        Assert.Equal(1, triangle.Vertex3.Y);
    }

    [Fact]
    public void Parse_BinaryStl_MultipleTriangles_Success()
    {
        // Arrange - Create binary STL with 3 triangles
        byte[] stlData = CreateBinaryStl(3, new[]
        {
            new Triangle(
                new Vertex(0, 0, 1),
                new Vertex(0, 0, 0),
                new Vertex(1, 0, 0),
                new Vertex(0, 1, 0)
            ),
            new Triangle(
                new Vertex(1, 0, 0),
                new Vertex(1, 1, 1),
                new Vertex(2, 1, 1),
                new Vertex(1, 2, 1)
            ),
            new Triangle(
                new Vertex(0, 1, 0),
                new Vertex(0, 0, 1),
                new Vertex(1, 0, 1),
                new Vertex(0, 1, 1)
            )
        });

        // Act
        var mesh = _parser.Parse(stlData);

        // Assert
        Assert.NotNull(mesh);
        Assert.Equal(3, mesh.Triangles.Count);
    }

    [Fact]
    public void Parse_BinaryStl_TooSmall_ThrowsException()
    {
        // Arrange - File too small (less than 84 bytes)
        byte[] stlData = new byte[50];

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _parser.Parse(stlData));
        Assert.Contains("too small", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_BinaryStl_CorruptedData_ThrowsException()
    {
        // Arrange - Header says 10 triangles but data is incomplete
        var data = new List<byte>();
        data.AddRange(new byte[80]); // Header
        data.AddRange(BitConverter.GetBytes((uint)10)); // Triangle count = 10
        data.AddRange(new byte[100]); // Only partial data

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _parser.Parse(data.ToArray()));
        Assert.Contains("corrupted", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region ASCII STL Tests

    [Fact]
    public void Parse_AsciiStl_SingleTriangle_Success()
    {
        // Arrange
        string stlContent = @"solid test_cube
facet normal 0 0 1
  outer loop
    vertex 0 0 0
    vertex 1 0 0
    vertex 0 1 0
  endloop
endfacet
endsolid test_cube";

        byte[] stlData = Encoding.UTF8.GetBytes(stlContent);

        // Act
        var mesh = _parser.Parse(stlData);

        // Assert
        Assert.NotNull(mesh);
        Assert.Equal("test_cube", mesh.Name);
        Assert.Single(mesh.Triangles);
        
        var triangle = mesh.Triangles[0];
        Assert.Equal(0, triangle.Normal.X);
        Assert.Equal(0, triangle.Normal.Y);
        Assert.Equal(1, triangle.Normal.Z);
        
        Assert.Equal(0, triangle.Vertex1.X);
        Assert.Equal(0, triangle.Vertex1.Y);
        Assert.Equal(0, triangle.Vertex1.Z);
        
        Assert.Equal(1, triangle.Vertex2.X);
        Assert.Equal(0, triangle.Vertex2.Y);
        Assert.Equal(0, triangle.Vertex2.Z);
    }

    [Fact]
    public void Parse_AsciiStl_MultipleTriangles_Success()
    {
        // Arrange
        string stlContent = @"solid cube
facet normal 0 0 1
  outer loop
    vertex 0 0 1
    vertex 1 0 1
    vertex 1 1 1
  endloop
endfacet
facet normal 0 0 1
  outer loop
    vertex 0 0 1
    vertex 1 1 1
    vertex 0 1 1
  endloop
endfacet
endsolid cube";

        byte[] stlData = Encoding.UTF8.GetBytes(stlContent);

        // Act
        var mesh = _parser.Parse(stlData);

        // Assert
        Assert.NotNull(mesh);
        Assert.Equal("cube", mesh.Name);
        Assert.Equal(2, mesh.Triangles.Count);
    }

    [Fact]
    public void Parse_AsciiStl_NoSolidKeyword_ThrowsException()
    {
        // Arrange - A file that looks like it should be ASCII but doesn't start with "solid"
        // Will be detected as binary and fail with binary parsing error
        string stlContent = @"invalid file
facet normal 0 0 1
  outer loop
    vertex 0 0 0
    vertex 1 0 0
    vertex 0 1 0
  endloop
endfacet";

        byte[] stlData = Encoding.UTF8.GetBytes(stlContent);

        // Act & Assert - Without "solid" keyword, will be treated as binary and fail
        var exception = Assert.Throws<ArgumentException>(() => _parser.Parse(stlData));
        // Should throw an error (either binary corrupted or ASCII without solid)
        Assert.NotNull(exception.Message);
    }

    [Fact]
    public void Parse_AsciiStl_MissingVertex_ThrowsException()
    {
        // Arrange - Missing third vertex
        string stlContent = @"solid test
facet normal 0 0 1
  outer loop
    vertex 0 0 0
    vertex 1 0 0
  endloop
endfacet
endsolid test";

        byte[] stlData = Encoding.UTF8.GetBytes(stlContent);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _parser.Parse(stlData));
        Assert.Contains("vertex", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_AsciiStl_InvalidCoordinates_ThrowsException()
    {
        // Arrange
        string stlContent = @"solid test
facet normal 0 0 1
  outer loop
    vertex abc def ghi
    vertex 1 0 0
    vertex 0 1 0
  endloop
endfacet
endsolid test";

        byte[] stlData = Encoding.UTF8.GetBytes(stlContent);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _parser.Parse(stlData));
        Assert.Contains("coordinates", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_AsciiStl_NoTriangles_ThrowsException()
    {
        // Arrange
        string stlContent = @"solid empty
endsolid empty";

        byte[] stlData = Encoding.UTF8.GetBytes(stlContent);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _parser.Parse(stlData));
        Assert.Contains("no triangles", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_AsciiStl_WithScientificNotation_Success()
    {
        // Arrange
        string stlContent = @"solid scientific
facet normal 0.0e0 0.0e0 1.0e0
  outer loop
    vertex 1.5e-3 2.0e-2 3.0e-1
    vertex 1.0e0 0.0e0 0.0e0
    vertex 0.0e0 1.0e0 0.0e0
  endloop
endfacet
endsolid scientific";

        byte[] stlData = Encoding.UTF8.GetBytes(stlContent);

        // Act
        var mesh = _parser.Parse(stlData);

        // Assert
        Assert.NotNull(mesh);
        Assert.Single(mesh.Triangles);
        Assert.Equal(0.0015f, mesh.Triangles[0].Vertex1.X, 6);
        Assert.Equal(0.02f, mesh.Triangles[0].Vertex1.Y, 6);
        Assert.Equal(0.3f, mesh.Triangles[0].Vertex1.Z, 6);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public void Parse_NullData_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _parser.Parse(null!));
    }

    [Fact]
    public void Parse_EmptyData_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _parser.Parse(Array.Empty<byte>()));
    }

    #endregion

    #region Mesh Data Tests

    [Fact]
    public void StlMesh_GetVertices_ReturnsAllVertices()
    {
        // Arrange
        string stlContent = @"solid test
facet normal 0 0 1
  outer loop
    vertex 0 0 0
    vertex 1 0 0
    vertex 0 1 0
  endloop
endfacet
endsolid test";

        byte[] stlData = Encoding.UTF8.GetBytes(stlContent);
        var mesh = _parser.Parse(stlData);

        // Act
        var vertices = mesh.GetVertices().ToList();

        // Assert
        Assert.Equal(3, vertices.Count);
    }

    [Fact]
    public void StlMesh_GetNormals_ReturnsAllNormals()
    {
        // Arrange
        string stlContent = @"solid test
facet normal 0 0 1
  outer loop
    vertex 0 0 0
    vertex 1 0 0
    vertex 0 1 0
  endloop
endfacet
facet normal 1 0 0
  outer loop
    vertex 1 0 0
    vertex 1 1 0
    vertex 1 0 1
  endloop
endfacet
endsolid test";

        byte[] stlData = Encoding.UTF8.GetBytes(stlContent);
        var mesh = _parser.Parse(stlData);

        // Act
        var normals = mesh.GetNormals().ToList();

        // Assert
        Assert.Equal(2, normals.Count);
        Assert.Equal(0, normals[0].X);
        Assert.Equal(0, normals[0].Y);
        Assert.Equal(1, normals[0].Z);
        Assert.Equal(1, normals[1].X);
    }

    #endregion

    #region Helper Methods

    private byte[] CreateBinaryStl(uint triangleCount, Triangle[] triangles)
    {
        var data = new List<byte>();

        // Header (80 bytes)
        data.AddRange(new byte[80]);

        // Triangle count (4 bytes)
        data.AddRange(BitConverter.GetBytes(triangleCount));

        // Triangles
        foreach (var triangle in triangles)
        {
            // Normal
            data.AddRange(BitConverter.GetBytes(triangle.Normal.X));
            data.AddRange(BitConverter.GetBytes(triangle.Normal.Y));
            data.AddRange(BitConverter.GetBytes(triangle.Normal.Z));

            // Vertex 1
            data.AddRange(BitConverter.GetBytes(triangle.Vertex1.X));
            data.AddRange(BitConverter.GetBytes(triangle.Vertex1.Y));
            data.AddRange(BitConverter.GetBytes(triangle.Vertex1.Z));

            // Vertex 2
            data.AddRange(BitConverter.GetBytes(triangle.Vertex2.X));
            data.AddRange(BitConverter.GetBytes(triangle.Vertex2.Y));
            data.AddRange(BitConverter.GetBytes(triangle.Vertex2.Z));

            // Vertex 3
            data.AddRange(BitConverter.GetBytes(triangle.Vertex3.X));
            data.AddRange(BitConverter.GetBytes(triangle.Vertex3.Y));
            data.AddRange(BitConverter.GetBytes(triangle.Vertex3.Z));

            // Attribute byte count (2 bytes)
            data.AddRange(BitConverter.GetBytes((ushort)0));
        }

        return data.ToArray();
    }

    private record struct Vertex(float X, float Y, float Z);
    private record Triangle(Vertex Normal, Vertex Vertex1, Vertex Vertex2, Vertex Vertex3);

    #endregion
}
