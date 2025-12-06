using System.Text;
using EvergineDemo.Shared.Services;
using EvergineDemo.Shared.Models.Stl;
using Evergine.Mathematics;
using Xunit;

namespace EvergineDemo.Shared.Tests.Services;

public class StlToEvergineConverterTests
{
    [Fact]
    public void GetVertices_SingleTriangle_ReturnsThreeVertices()
    {
        // Arrange
        var parser = new StlParserService();
        string stlContent = @"solid test
facet normal 0 0 1
  outer loop
    vertex 1 2 3
    vertex 4 5 6
    vertex 7 8 9
  endloop
endfacet
endsolid test";
        byte[] stlData = Encoding.UTF8.GetBytes(stlContent);
        var mesh = parser.Parse(stlData);

        // Act
        var vertices = StlToEvergineConverter.GetVertices(mesh);

        // Assert
        Assert.Equal(3, vertices.Length);
        Assert.Equal(new Vector3(1, 2, 3), vertices[0]);
        Assert.Equal(new Vector3(4, 5, 6), vertices[1]);
        Assert.Equal(new Vector3(7, 8, 9), vertices[2]);
    }

    [Fact]
    public void GetNormals_SingleTriangle_ReturnsThreeNormals()
    {
        // Arrange
        var parser = new StlParserService();
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
        var mesh = parser.Parse(stlData);

        // Act
        var normals = StlToEvergineConverter.GetNormals(mesh);

        // Assert
        Assert.Equal(3, normals.Length);
        Assert.All(normals, n => Assert.Equal(new Vector3(0, 0, 1), n));
    }

    [Fact]
    public void GetIndices_SingleTriangle_ReturnsSequentialIndices()
    {
        // Arrange
        var parser = new StlParserService();
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
        var mesh = parser.Parse(stlData);

        // Act
        var indices = StlToEvergineConverter.GetIndices(mesh);

        // Assert
        Assert.Equal(3, indices.Length);
        Assert.Equal(0u, indices[0]);
        Assert.Equal(1u, indices[1]);
        Assert.Equal(2u, indices[2]);
    }

    [Fact]
    public void GetBoundingBox_SimpleMesh_ReturnsCorrectBounds()
    {
        // Arrange
        var parser = new StlParserService();
        string stlContent = @"solid test
facet normal 0 0 1
  outer loop
    vertex 0 0 0
    vertex 2 0 0
    vertex 0 3 0
  endloop
endfacet
facet normal 0 0 1
  outer loop
    vertex 0 0 1
    vertex 2 0 1
    vertex 0 3 1
  endloop
endfacet
endsolid test";
        byte[] stlData = Encoding.UTF8.GetBytes(stlContent);
        var mesh = parser.Parse(stlData);

        // Act
        var (min, max) = StlToEvergineConverter.GetBoundingBox(mesh);

        // Assert
        Assert.Equal(new Vector3(0, 0, 0), min);
        Assert.Equal(new Vector3(2, 3, 1), max);
    }

    [Fact]
    public void GetCenter_SimpleMesh_ReturnsCorrectCenter()
    {
        // Arrange
        var parser = new StlParserService();
        string stlContent = @"solid test
facet normal 0 0 1
  outer loop
    vertex 0 0 0
    vertex 2 0 0
    vertex 0 2 0
  endloop
endfacet
endsolid test";
        byte[] stlData = Encoding.UTF8.GetBytes(stlContent);
        var mesh = parser.Parse(stlData);

        // Act
        var center = StlToEvergineConverter.GetCenter(mesh);

        // Assert
        Assert.Equal(new Vector3(1, 1, 0), center);
    }

    [Fact]
    public void GetSize_SimpleMesh_ReturnsCorrectSize()
    {
        // Arrange
        var parser = new StlParserService();
        string stlContent = @"solid test
facet normal 0 0 1
  outer loop
    vertex 0 0 0
    vertex 4 0 0
    vertex 0 6 0
  endloop
endfacet
facet normal 0 0 1
  outer loop
    vertex 0 0 2
    vertex 4 0 2
    vertex 0 6 2
  endloop
endfacet
endsolid test";
        byte[] stlData = Encoding.UTF8.GetBytes(stlContent);
        var mesh = parser.Parse(stlData);

        // Act
        var size = StlToEvergineConverter.GetSize(mesh);

        // Assert
        Assert.Equal(new Vector3(4, 6, 2), size);
    }

    [Fact]
    public void GetVertices_MultipleTriangles_ReturnsAllVertices()
    {
        // Arrange
        var parser = new StlParserService();
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
        var mesh = parser.Parse(stlData);

        // Act
        var vertices = StlToEvergineConverter.GetVertices(mesh);

        // Assert
        Assert.Equal(6, vertices.Length); // 2 triangles * 3 vertices
    }
}
