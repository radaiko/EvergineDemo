using Evergine.Mathematics;
using EvergineDemo.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EvergineDemo.Frontend.Desktop.Services;

/// <summary>
/// Service that manages Evergine 3D rendering state
/// Note: This version focuses on state management. Full Evergine rendering 
/// will be added in a future iteration once we establish the OpenGL context properly.
/// </summary>
public class EvergineRenderingService : IDisposable
{
    private const int DefaultWidth = 800;
    private const int DefaultHeight = 600;

    private bool _initialized = false;
    private int _width;
    private int _height;
    
    // Scene state
    private readonly Dictionary<string, ModelSceneObject> _sceneModels = new();
    private readonly object _sceneLock = new();

    public class ModelSceneObject
    {
        public string Id { get; set; } = string.Empty;
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }
        public Vector3 Scale { get; set; }
        public string FileName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Initialize rendering system
    /// </summary>
    public void Initialize(int width, int height)
    {
        if (_initialized)
        {
            return;
        }

        _width = width > 0 ? width : DefaultWidth;
        _height = height > 0 ? height : DefaultHeight;

        try
        {
            _initialized = true;
            Console.WriteLine($"EvergineRenderingService initialized ({_width}x{_height})");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to initialize rendering service: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            throw;
        }
    }

    /// <summary>
    /// Render the scene
    /// Note: For now, rendering is handled by EvergineControl using raw OpenGL.
    /// This method is reserved for future Evergine scene graph integration.
    /// </summary>
    public void Render()
    {
        if (!_initialized)
        {
            return;
        }

        // Scene rendering happens in EvergineControl.OnOpenGlRender
        // This method will be used for Evergine scene graph updates in the future
    }

    /// <summary>
    /// Handle resize events
    /// </summary>
    public void Resize(int width, int height)
    {
        if (!_initialized || width <= 0 || height <= 0)
        {
            return;
        }

        _width = width;
        _height = height;

        Console.WriteLine($"Rendering surface resized to {_width}x{_height}");
    }

    /// <summary>
    /// Update scene with new room state
    /// </summary>
    public void UpdateScene(RoomState roomState)
    {
        if (!_initialized)
        {
            return;
        }

        lock (_sceneLock)
        {
            // Remove models that no longer exist
            var modelsToRemove = _sceneModels.Keys.Except(roomState.Models.Select(m => m.Id)).ToList();
            foreach (var modelId in modelsToRemove)
            {
                _sceneModels.Remove(modelId);
                Console.WriteLine($"Removed model from scene: {modelId}");
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
                    _sceneModels[model.Id] = new ModelSceneObject
                    {
                        Id = model.Id,
                        Position = model.Position,
                        Rotation = model.Rotation,
                        Scale = model.Scale,
                        FileName = model.FileName
                    };
                    Console.WriteLine($"Added model to scene: {model.FileName} at {model.Position}");
                }
            }

            Console.WriteLine($"Scene updated: {_sceneModels.Count} models");
        }
    }

    /// <summary>
    /// Get current scene models for rendering
    /// </summary>
    public IReadOnlyList<ModelSceneObject> GetSceneModels()
    {
        lock (_sceneLock)
        {
            return _sceneModels.Values.ToList();
        }
    }

    /// <summary>
    /// Clean up resources
    /// </summary>
    public void Dispose()
    {
        if (_initialized)
        {
            _initialized = false;
            Console.WriteLine("EvergineRenderingService disposed");
        }
    }
}

