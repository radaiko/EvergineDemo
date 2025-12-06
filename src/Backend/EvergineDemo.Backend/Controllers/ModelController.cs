using Microsoft.AspNetCore.Mvc;
using EvergineDemo.Shared.Models;
using EvergineDemo.Backend.Services;

namespace EvergineDemo.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ModelController : ControllerBase
{
    private readonly SimulationService _simulationService;
    private readonly ILogger<ModelController> _logger;

    public ModelController(SimulationService simulationService, ILogger<ModelController> logger)
    {
        _simulationService = simulationService;
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

            // Add model to simulation
            var model = await _simulationService.AddModelAsync(request.FileName);

            return Ok(model);
        }
        catch (FormatException)
        {
            return BadRequest("Invalid file content format");
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
