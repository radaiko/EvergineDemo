# STL File Parser Documentation

## Overview

The STL parser implementation provides comprehensive support for parsing STL (STereoLithography) files, which are commonly used for 3D printing and CAD applications. The parser supports both binary and ASCII STL formats and includes utilities for converting STL data to Evergine-compatible mesh format.

## Features

- ✅ **Binary STL Format Support**: Parses the binary STL format with proper validation
- ✅ **ASCII STL Format Support**: Parses text-based STL format with scientific notation support
- ✅ **Auto-Format Detection**: Automatically detects whether a file is binary or ASCII
- ✅ **Comprehensive Error Handling**: Gracefully handles malformed, corrupted, or invalid files
- ✅ **Evergine Integration**: Converts STL mesh data to Evergine Vector3 format
- ✅ **Bounding Box Calculations**: Utilities for computing mesh dimensions and center
- ✅ **Fully Tested**: 22 unit tests covering various scenarios

## Architecture

### Data Models (`EvergineDemo.Shared.Models.Stl`)

#### `StlVertex`
Represents a 3D point in space with X, Y, Z coordinates.

```csharp
public struct StlVertex
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
}
```

#### `StlTriangle`
Represents a triangle face with a normal vector and three vertices.

```csharp
public class StlTriangle
{
    public StlVertex Normal { get; set; }
    public StlVertex Vertex1 { get; set; }
    public StlVertex Vertex2 { get; set; }
    public StlVertex Vertex3 { get; set; }
    public ushort AttributeByteCount { get; set; } // Binary STL only
}
```

#### `StlMesh`
Represents a complete 3D mesh composed of triangles.

```csharp
public class StlMesh
{
    public string Name { get; set; }
    public List<StlTriangle> Triangles { get; set; }
    
    public IEnumerable<StlVertex> GetVertices();
    public IEnumerable<StlVertex> GetNormals();
}
```

### Services

#### `StlParserService`
Main parser service for reading STL files.

```csharp
public class StlParserService
{
    public StlMesh Parse(byte[] fileBytes, string fileName = "");
}
```

**Usage:**
```csharp
var parser = new StlParserService();
byte[] fileData = File.ReadAllBytes("model.stl");
StlMesh mesh = parser.Parse(fileData, "model.stl");

Console.WriteLine($"Loaded {mesh.Name} with {mesh.Triangles.Count} triangles");
```

#### `StlToEvergineConverter`
Static utility for converting STL mesh data to Evergine format.

```csharp
public static class StlToEvergineConverter
{
    public static Vector3[] GetVertices(StlMesh mesh);
    public static Vector3[] GetNormals(StlMesh mesh);
    public static uint[] GetIndices(StlMesh mesh);
    public static (Vector3 Min, Vector3 Max) GetBoundingBox(StlMesh mesh);
    public static Vector3 GetCenter(StlMesh mesh);
    public static Vector3 GetSize(StlMesh mesh);
}
```

**Usage:**
```csharp
var vertices = StlToEvergineConverter.GetVertices(mesh);
var normals = StlToEvergineConverter.GetNormals(mesh);
var indices = StlToEvergineConverter.GetIndices(mesh);
var (min, max) = StlToEvergineConverter.GetBoundingBox(mesh);
```

## STL File Formats

### Binary STL Format

Binary STL files have the following structure:
- **Header** (80 bytes): Optional description/metadata
- **Triangle Count** (4 bytes): Unsigned 32-bit integer
- **Triangles** (50 bytes each):
  - Normal vector (12 bytes: 3 floats)
  - Vertex 1 (12 bytes: 3 floats)
  - Vertex 2 (12 bytes: 3 floats)
  - Vertex 3 (12 bytes: 3 floats)
  - Attribute byte count (2 bytes: unsigned short)

Minimum file size: 84 bytes (80 + 4)

### ASCII STL Format

ASCII STL files are text-based with the following structure:

```
solid <name>
  facet normal <nx> <ny> <nz>
    outer loop
      vertex <x1> <y1> <z1>
      vertex <x2> <y2> <z2>
      vertex <x3> <y3> <z3>
    endloop
  endfacet
  ...
endsolid <name>
```

## Format Detection

The parser automatically detects the file format:

1. **ASCII Detection**: Files starting with "solid" keyword (case-insensitive)
2. **Binary Detection**: All other files
3. **Disambiguation**: For files starting with "solid" but >= 84 bytes, validates against expected binary file size

## Error Handling

The parser throws `ArgumentException` for various error conditions:

- **Null or empty data**: "File data cannot be null or empty"
- **Binary file too small**: "Binary STL file too small (minimum 84 bytes required)"
- **Binary file corrupted**: "Binary STL file corrupted: expected X bytes, got Y"
- **ASCII missing solid keyword**: "ASCII STL file must start with 'solid' keyword"
- **ASCII no triangles**: "ASCII STL file contains no triangles"
- **Invalid coordinates**: "Invalid vertex coordinates: ..."
- **Missing vertices**: Validation errors for incomplete triangles

## Integration with Backend

The STL parser is integrated into the ModelController:

```csharp
[HttpPost("upload")]
public async Task<ActionResult<ModelState>> UploadStl([FromBody] UploadStlRequest request)
{
    byte[] fileBytes = Convert.FromBase64String(request.FileContent);
    
    // Parse STL file
    var stlMesh = _stlParser.Parse(fileBytes, request.FileName);
    
    // Add to simulation
    var model = await _simulationService.AddModelAsync(request.FileName, stlMesh);
    
    return Ok(model);
}
```

## Unit Tests

The implementation includes 22 comprehensive unit tests:

### Binary STL Tests
- Single triangle parsing
- Multiple triangles parsing
- File size validation
- Corrupted data handling

### ASCII STL Tests
- Single triangle parsing
- Multiple triangles parsing
- Scientific notation support
- Missing solid keyword
- Missing vertices
- Invalid coordinates
- Empty files

### Converter Tests
- Vertex extraction
- Normal extraction
- Index generation
- Bounding box calculation
- Center point calculation
- Size calculation

## Performance Considerations

- **Binary parsing**: O(n) where n is the number of triangles
- **ASCII parsing**: O(n) where n is the number of lines
- **Memory**: Stores all triangles in memory; large files (>10k triangles) may require streaming approach
- **Thread-safety**: Service is stateless and thread-safe

## Future Enhancements

Potential improvements for future versions:

1. **Streaming Parser**: Support for very large STL files without loading entire file into memory
2. **Mesh Optimization**: Vertex deduplication and triangle indexing
3. **Format Export**: Write STL files from mesh data
4. **Additional Formats**: OBJ, PLY, 3MF support
5. **Mesh Validation**: Check for manifold edges, normals consistency
6. **Texture Coordinates**: Support for colored STL variants

## References

- [STL File Format Specification](https://en.wikipedia.org/wiki/STL_(file_format))
- [Evergine Documentation](https://docs.evergine.com/)
