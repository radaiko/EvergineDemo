using Evergine.Mathematics;
using EvergineDemo.Shared.Models;
using System.Collections.Generic;
using System.Linq;

namespace EvergineDemo.Frontend.Desktop.Services;

/// <summary>
/// Manages the Evergine 3D scene
/// </summary>
public class EvergineSceneManager
{
    private readonly Dictionary<string, SceneModel> _sceneModels = new();

    public class SceneModel
    {
        public string Id { get; set; } = string.Empty;
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }
        public Vector3 Scale { get; set; }
        public string FileName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Update the scene with new room state
    /// </summary>
    public void UpdateScene(RoomState roomState)
    {
        // Remove models that no longer exist
        var modelsToRemove = _sceneModels.Keys.Except(roomState.Models.Select(m => m.Id)).ToList();
        foreach (var modelId in modelsToRemove)
        {
            _sceneModels.Remove(modelId);
        }

        // Add or update models
        foreach (var model in roomState.Models)
        {
            if (_sceneModels.ContainsKey(model.Id))
            {
                // Update existing model
                var sceneModel = _sceneModels[model.Id];
                sceneModel.Position = model.Position;
                sceneModel.Rotation = model.Rotation;
                sceneModel.Scale = model.Scale;
            }
            else
            {
                // Add new model
                _sceneModels[model.Id] = new SceneModel
                {
                    Id = model.Id,
                    Position = model.Position,
                    Rotation = model.Rotation,
                    Scale = model.Scale,
                    FileName = model.FileName
                };
            }
        }
    }

    /// <summary>
    /// Get all scene models
    /// </summary>
    public IReadOnlyCollection<SceneModel> GetModels()
    {
        return _sceneModels.Values.ToList();
    }
}
