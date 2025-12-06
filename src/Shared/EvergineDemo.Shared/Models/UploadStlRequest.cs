namespace EvergineDemo.Shared.Models;

/// <summary>
/// Request to upload an STL file
/// </summary>
public class UploadStlRequest
{
    /// <summary>
    /// File name
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// File content (base64 encoded)
    /// </summary>
    public string FileContent { get; set; } = string.Empty;
}
