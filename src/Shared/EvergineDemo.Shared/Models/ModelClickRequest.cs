namespace EvergineDemo.Shared.Models;

/// <summary>
/// Request to indicate a model was clicked
/// </summary>
public class ModelClickRequest
{
    /// <summary>
    /// ID of the clicked model
    /// </summary>
    public string ModelId { get; set; } = string.Empty;
}
