using Microsoft.AspNetCore.Mvc;
using EvergineDemo.Shared.Models;
using EvergineDemo.Backend.Services;
using EvergineDemo.Shared.Services;

namespace EvergineDemo.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ModelController : ControllerBase
{
    private readonly SimulationService _simulationService;
    private readonly StlParserService _stlParser;
    private readonly ILogger<ModelController> _logger;

    public ModelController(SimulationService simulationService, StlParserService stlParser, ILogger<ModelController> logger)
    {
        _simulationService = simulationService;
        _stlParser = stlParser;
        _logger = logger;
    }

    /// <summary>
    /// Upload an STL file and add it to the simulation
    /// </summary>
    [HttpPost("upload")]
    [RequestSizeLimit(100 * 1024 * 1024)] // 100 MB limit
    public async Task<ActionResult<ModelState>> UploadStl([FromBody] UploadStlRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FileName))
        {
            return BadRequest("File name is required");
        }

        if (string.IsNullOrWhiteSpace(request.FileContent))
        {
            return BadRequest("File content is required");
        }

        try
        {
            // Validate base64 content
            byte[] fileBytes = Convert.FromBase64String(request.FileContent);
            _logger.LogInformation("Received STL file: {FileName}, Size: {Size} bytes", 
                request.FileName, fileBytes.Length);

            // Parse STL file
            var stlMesh = _stlParser.Parse(fileBytes, request.FileName);
            _logger.LogInformation("Parsed STL file: {MeshName}, Triangles: {TriangleCount}", 
                stlMesh.Name, stlMesh.Triangles.Count);

            // Add model to simulation
            var model = await _simulationService.AddModelAsync(request.FileName, stlMesh);

            return Ok(model);
        }
        catch (FormatException)
        {
            return BadRequest("Invalid file content format");
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid STL file: {FileName}", request.FileName);
            return BadRequest($"Invalid STL file: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading STL file");
            return StatusCode(500, "Error processing file");
        }
    }

    /// <summary>
    /// Get current room state
    /// </summary>
    [HttpGet("state")]
    public ActionResult<RoomState> GetState()
    {
        return Ok(_simulationService.GetRoomState());
    }
}
