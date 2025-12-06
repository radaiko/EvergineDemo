using System.Text;
using EvergineDemo.Shared.Models.Stl;

namespace EvergineDemo.Shared.Services;

/// <summary>
/// Service for parsing STL files (both binary and ASCII formats)
/// </summary>
public class StlParserService
{
    private const int BinaryHeaderSize = 80;
    private const int BinaryTriangleSize = 50; // 12 floats * 4 bytes + 2 bytes attribute
    private const string AsciiSolidKeyword = "solid";
    private const string AsciiEndSolidKeyword = "endsolid";

    /// <summary>
    /// Parse an STL file from byte array, auto-detecting the format
    /// </summary>
    /// <param name="fileBytes">The raw bytes of the STL file</param>
    /// <param name="fileName">The file name (optional, used for naming)</param>
    /// <returns>Parsed STL mesh</returns>
    /// <exception cref="ArgumentException">Thrown when file is invalid or corrupted</exception>
    public StlMesh Parse(byte[] fileBytes, string fileName = "")
    {
        if (fileBytes == null || fileBytes.Length == 0)
        {
            throw new ArgumentException("File data cannot be null or empty", nameof(fileBytes));
        }

        // Auto-detect format
        if (IsBinaryStl(fileBytes))
        {
            return ParseBinary(fileBytes, fileName);
        }
        else
        {
            return ParseAscii(fileBytes, fileName);
        }
    }

    /// <summary>
    /// Determine if the STL file is in binary format
    /// </summary>
    private bool IsBinaryStl(byte[] fileBytes)
    {
        // Check if it starts with "solid" (ASCII marker)
        if (fileBytes.Length >= 5)
        {
            string header = Encoding.ASCII.GetString(fileBytes, 0, 5);
            if (header.Equals(AsciiSolidKeyword, StringComparison.OrdinalIgnoreCase))
            {
                // Could be ASCII or binary with "solid" in header
                // For files >= 84 bytes, check if size matches binary format
                if (fileBytes.Length >= 84)
                {
                    uint triangleCount = BitConverter.ToUInt32(fileBytes, 80);
                    
                    // Calculate expected file size for binary format
                    long expectedSize = 84 + (triangleCount * BinaryTriangleSize);
                    
                    // If size matches binary format exactly, it's likely binary
                    if (fileBytes.Length == expectedSize)
                    {
                        return true;
                    }
                }
                
                // Otherwise, assume ASCII
                return false;
            }
        }

        // No "solid" keyword or file too small to check - must be binary
        // Binary STL files must be at least 84 bytes (80 header + 4 triangle count)
        return true;
    }

    /// <summary>
    /// Parse a binary STL file
    /// </summary>
    private StlMesh ParseBinary(byte[] fileBytes, string fileName)
    {
        if (fileBytes.Length < 84)
        {
            throw new ArgumentException("Binary STL file too small (minimum 84 bytes required)");
        }

        var mesh = new StlMesh
        {
            Name = string.IsNullOrWhiteSpace(fileName) ? "BinarySTL" : fileName
        };

        // Read triangle count (bytes 80-83)
        uint triangleCount = BitConverter.ToUInt32(fileBytes, 80);

        // Validate file size
        long expectedSize = 84 + (triangleCount * BinaryTriangleSize);
        if (fileBytes.Length < expectedSize)
        {
            throw new ArgumentException($"Binary STL file corrupted: expected {expectedSize} bytes, got {fileBytes.Length}");
        }

        // Parse triangles
        int offset = 84;
        for (int i = 0; i < triangleCount; i++)
        {
            if (offset + BinaryTriangleSize > fileBytes.Length)
            {
                throw new ArgumentException($"Binary STL file corrupted: insufficient data for triangle {i}");
            }

            var triangle = new StlTriangle
            {
                Normal = ReadVertex(fileBytes, offset),
                Vertex1 = ReadVertex(fileBytes, offset + 12),
                Vertex2 = ReadVertex(fileBytes, offset + 24),
                Vertex3 = ReadVertex(fileBytes, offset + 36),
                AttributeByteCount = BitConverter.ToUInt16(fileBytes, offset + 48)
            };

            mesh.Triangles.Add(triangle);
            offset += BinaryTriangleSize;
        }

        return mesh;
    }

    /// <summary>
    /// Read a vertex from binary data at the specified offset
    /// </summary>
    private StlVertex ReadVertex(byte[] data, int offset)
    {
        return new StlVertex(
            BitConverter.ToSingle(data, offset),
            BitConverter.ToSingle(data, offset + 4),
            BitConverter.ToSingle(data, offset + 8)
        );
    }

    /// <summary>
    /// Parse an ASCII STL file
    /// </summary>
    private StlMesh ParseAscii(byte[] fileBytes, string fileName)
    {
        string content;
        try
        {
            content = Encoding.UTF8.GetString(fileBytes);
        }
        catch (Exception ex)
        {
            throw new ArgumentException("Failed to decode ASCII STL file", ex);
        }

        var mesh = new StlMesh();
        var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        
        if (lines.Length == 0)
        {
            throw new ArgumentException("ASCII STL file is empty");
        }

        int lineIndex = 0;

        // Parse "solid <name>" line
        var firstLine = lines[0].Trim();
        if (!firstLine.StartsWith(AsciiSolidKeyword, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("ASCII STL file must start with 'solid' keyword");
        }

        mesh.Name = firstLine.Length > 6 ? firstLine.Substring(6).Trim() : fileName;
        if (string.IsNullOrWhiteSpace(mesh.Name))
        {
            mesh.Name = string.IsNullOrWhiteSpace(fileName) ? "AsciiSTL" : fileName;
        }

        lineIndex++;

        // Parse triangles
        while (lineIndex < lines.Length)
        {
            var line = lines[lineIndex].Trim();

            if (line.StartsWith(AsciiEndSolidKeyword, StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            if (line.StartsWith("facet", StringComparison.OrdinalIgnoreCase))
            {
                var triangle = ParseAsciiTriangle(lines, ref lineIndex);
                mesh.Triangles.Add(triangle);
            }
            else
            {
                lineIndex++;
            }
        }

        if (mesh.Triangles.Count == 0)
        {
            throw new ArgumentException("ASCII STL file contains no triangles");
        }

        return mesh;
    }

    /// <summary>
    /// Parse a single triangle from ASCII format
    /// </summary>
    private StlTriangle ParseAsciiTriangle(string[] lines, ref int lineIndex)
    {
        var triangle = new StlTriangle();

        // Parse "facet normal nx ny nz"
        var facetLine = lines[lineIndex].Trim();
        var normalParts = facetLine.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        if (normalParts.Length >= 5 && normalParts[0].Equals("facet", StringComparison.OrdinalIgnoreCase))
        {
            triangle.Normal = ParseVertex(normalParts, 2);
        }
        else
        {
            throw new ArgumentException($"Invalid facet line at index {lineIndex}: {facetLine}");
        }

        lineIndex++;

        // Expect "outer loop"
        if (lineIndex >= lines.Length || !lines[lineIndex].Trim().Equals("outer loop", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"Expected 'outer loop' at line {lineIndex}");
        }

        lineIndex++;

        // Parse three vertices
        triangle.Vertex1 = ParseAsciiVertex(lines, ref lineIndex);
        triangle.Vertex2 = ParseAsciiVertex(lines, ref lineIndex);
        triangle.Vertex3 = ParseAsciiVertex(lines, ref lineIndex);

        // Expect "endloop"
        if (lineIndex >= lines.Length || !lines[lineIndex].Trim().Equals("endloop", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"Expected 'endloop' at line {lineIndex}");
        }

        lineIndex++;

        // Expect "endfacet"
        if (lineIndex >= lines.Length || !lines[lineIndex].Trim().Equals("endfacet", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"Expected 'endfacet' at line {lineIndex}");
        }

        lineIndex++;

        return triangle;
    }

    /// <summary>
    /// Parse a vertex line from ASCII format
    /// </summary>
    private StlVertex ParseAsciiVertex(string[] lines, ref int lineIndex)
    {
        if (lineIndex >= lines.Length)
        {
            throw new ArgumentException($"Unexpected end of file at line {lineIndex}");
        }

        var line = lines[lineIndex].Trim();
        var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length < 4 || !parts[0].Equals("vertex", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"Invalid vertex line at index {lineIndex}: {line}");
        }

        lineIndex++;
        return ParseVertex(parts, 1);
    }

    /// <summary>
    /// Parse vertex coordinates from string parts
    /// </summary>
    private StlVertex ParseVertex(string[] parts, int startIndex)
    {
        if (parts.Length < startIndex + 3)
        {
            throw new ArgumentException("Insufficient coordinates for vertex");
        }

        try
        {
            return new StlVertex(
                float.Parse(parts[startIndex], System.Globalization.CultureInfo.InvariantCulture),
                float.Parse(parts[startIndex + 1], System.Globalization.CultureInfo.InvariantCulture),
                float.Parse(parts[startIndex + 2], System.Globalization.CultureInfo.InvariantCulture)
            );
        }
        catch (FormatException ex)
        {
            throw new ArgumentException($"Invalid vertex coordinates: {string.Join(", ", parts.Skip(startIndex).Take(3))}", ex);
        }
    }
}
