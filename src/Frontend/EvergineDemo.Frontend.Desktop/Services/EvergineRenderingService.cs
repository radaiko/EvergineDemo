using Evergine.Common.Graphics;
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
    
    // Scene configuration
    private readonly SceneConfiguration _sceneConfig = new();
    
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
            Console.WriteLine($"[EvergineRenderingService] Initialized rendering service ({_width}x{_height})");
            Console.WriteLine($"[EvergineRenderingService] Scene configuration: Room size={_sceneConfig.RoomSize}, Floor Y={_sceneConfig.FloorY}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EvergineRenderingService] Failed to initialize rendering service: {ex.Message}");
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

        Console.WriteLine($"[EvergineRenderingService] Rendering surface resized to {_width}x{_height}");
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
                Console.WriteLine($"[EvergineRenderingService] Removed model from scene: {modelId}");
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
                    Console.WriteLine($"[EvergineRenderingService] Added model to scene: {model.FileName} at {model.Position}");
                }
            }

            Console.WriteLine($"[EvergineRenderingService] Scene updated: {_sceneModels.Count} models");
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
    /// Get scene configuration for rendering
    /// </summary>
    public SceneConfiguration GetSceneConfiguration()
    {
        return _sceneConfig;
    }

    /// <summary>
    /// Clean up resources
    /// </summary>
    public void Dispose()
    {
        if (_initialized)
        {
            _initialized = false;
            Console.WriteLine("[EvergineRenderingService] Rendering service disposed");
        }
    }
}

/// <summary>
/// Configuration for the 3D scene including camera, lighting, and environment
/// </summary>
public class SceneConfiguration
{
    // Room dimensions (matching backend)
    public Vector3 RoomSize { get; init; } = new Vector3(10f, 10f, 10f);
    public float FloorY { get; init; } = 0f;
    
    // Camera configuration
    public CameraConfig Camera { get; init; } = new CameraConfig();
    
    // Lighting configuration
    public DirectionalLightConfig DirectionalLight { get; init; } = new DirectionalLightConfig();
    public PointLightConfig AmbientLight { get; init; } = new PointLightConfig();
    
    public class CameraConfig
    {
        public Vector3 Position { get; init; } = new Vector3(15f, 8f, 15f);
        public Quaternion Orientation { get; init; } = Quaternion.CreateFromYawPitchRoll(
            MathHelper.ToRadians(-45f), // Yaw: look toward center
            MathHelper.ToRadians(-30f), // Pitch: look down  
            0f);
        public float FieldOfView { get; init; } = MathHelper.ToRadians(45f);
        public float NearPlane { get; init; } = 0.1f;
        public float FarPlane { get; init; } = 1000f;
        // Grey background (0.5, 0.5, 0.5, 1.0) for better visibility
        // TODO: Consider making this configurable through dependency injection or configuration file
        public Color BackgroundColor { get; init; } = new Color(0.5f, 0.5f, 0.5f, 1.0f);
    }
    
    public class DirectionalLightConfig
    {
        public Vector3 Position { get; init; } = new Vector3(5f, 10f, 5f);
        public Quaternion Orientation { get; init; } = Quaternion.CreateFromYawPitchRoll(
            MathHelper.ToRadians(45f),
            MathHelper.ToRadians(-60f),
            0f);
        public Color Color { get; init; } = Color.White;
        public float Intensity { get; init; } = 1.5f;
    }
    
    public class PointLightConfig
    {
        public Vector3 Position { get; init; } = new Vector3(0f, 8f, 0f);
        public Color Color { get; init; } = new Color(0.3f, 0.3f, 0.35f);
        public float Intensity { get; init; } = 0.4f;
        public float LightRange { get; init; } = 50f;
    }
}

