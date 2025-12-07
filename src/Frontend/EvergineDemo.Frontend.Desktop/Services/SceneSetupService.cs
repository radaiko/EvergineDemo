using Evergine.Common.Graphics;
using Evergine.Components.Cameras;
using Evergine.Components.Graphics3D;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Mathematics;
using System;

namespace EvergineDemo.Frontend.Desktop.Services;

/// <summary>
/// Service responsible for setting up the 3D scene with camera, lighting, and environment
/// </summary>
public class SceneSetupService
{
    private Scene? _scene;
    private Entity? _cameraEntity;
    private Entity? _directionalLightEntity;
    private Entity? _ambientLightEntity;
    private Entity? _floorEntity;
    private ModelRenderingService? _modelRenderingService;
    
    // Room dimensions from configuration
    private Vector3 RoomSize => new Vector3(10f, 10f, 10f);
    
    // Camera settings
    private const float CameraDistance = 15f;
    private const float CameraHeight = 8f;
    private const float CameraFov = 45f;
    
    /// <summary>
    /// Initialize and configure the 3D scene
    /// </summary>
    public void SetupScene(Scene scene, ModelRenderingService? modelRenderingService = null)
    {
        _scene = scene ?? throw new ArgumentNullException(nameof(scene));
        _modelRenderingService = modelRenderingService;
        
        // Set up all scene components
        SetupCamera();
        SetupLighting();
        SetupFloor();
        SetupRoomBoundaries();
        SetupEnvironment();
        
        Console.WriteLine("Scene setup completed successfully");
    }
    
    /// <summary>
    /// Create and configure the perspective camera with orbit position
    /// </summary>
    private void SetupCamera()
    {
        if (_scene == null) return;
        
        // Create camera entity
        _cameraEntity = new Entity("MainCamera")
            .AddComponent(new Transform3D
            {
                LocalPosition = new Vector3(CameraDistance, CameraHeight, CameraDistance),
                LocalOrientation = Quaternion.CreateFromYawPitchRoll(
                    MathHelper.ToRadians(-45f), // Yaw: look toward center
                    MathHelper.ToRadians(-30f), // Pitch: look down
                    0f)
            })
            .AddComponent(new Camera3D
            {
                BackgroundColor = new Color(0.12f, 0.12f, 0.15f), // Dark blue-gray background
                NearPlane = 0.1f,
                FarPlane = 1000f,
                FieldOfView = MathHelper.ToRadians(CameraFov),
                AspectRatio = 16f / 9f // Default aspect ratio, will be updated on resize
            });
        
        _scene.Managers.EntityManager.Add(_cameraEntity);
        Console.WriteLine($"Camera created at position: {_cameraEntity.FindComponent<Transform3D>().LocalPosition}");
    }
    
    /// <summary>
    /// Create directional and ambient lighting for the scene
    /// </summary>
    private void SetupLighting()
    {
        if (_scene == null) return;
        
        // Directional light (main sun-like light from above and slightly to the side)
        _directionalLightEntity = new Entity("DirectionalLight")
            .AddComponent(new Transform3D
            {
                LocalPosition = new Vector3(5f, 10f, 5f),
                LocalOrientation = Quaternion.CreateFromYawPitchRoll(
                    MathHelper.ToRadians(45f),   // Yaw
                    MathHelper.ToRadians(-60f),  // Pitch: angled down
                    0f)
            })
            .AddComponent(new DirectionalLight
            {
                Color = Color.White,
                Intensity = 1.5f
            });
        
        _scene.Managers.EntityManager.Add(_directionalLightEntity);
        
        // Additional point light for ambient-like soft global illumination
        _ambientLightEntity = new Entity("PointLight")
            .AddComponent(new Transform3D
            {
                LocalPosition = new Vector3(0f, 8f, 0f)
            })
            .AddComponent(new PointLight
            {
                Color = new Color(0.3f, 0.3f, 0.35f), // Slightly blue tint
                Intensity = 0.4f,
                LightRange = 50f // Large range to act as ambient-like lighting
            });
        
        _scene.Managers.EntityManager.Add(_ambientLightEntity);
        Console.WriteLine("Lighting setup completed: Directional + PointLight (ambient-like)");
    }
    
    /// <summary>
    /// Create floor plane at Y=0 matching backend room dimensions
    /// </summary>
    private void SetupFloor()
    {
        if (_scene == null) return;
        
        // Create floor entity with a plane mesh
        _floorEntity = new Entity("Floor")
            .AddComponent(new Transform3D
            {
                LocalPosition = new Vector3(0f, 0f, 0f),
                LocalScale = new Vector3(RoomSize.X, 1f, RoomSize.Z)
            })
            .AddComponent(new MaterialComponent())
            .AddComponent(new PlaneMesh
            {
                Width = 1f,
                Height = 1f,
                PlaneNormal = PlaneMesh.NormalAxis.YPositive, // Floor faces up
                TwoSides = true
            })
            .AddComponent(new MeshRenderer());
        
        _scene.Managers.EntityManager.Add(_floorEntity);
        
        // Note: Material configuration for gray floor would be done here
        // In a full implementation, this would use a StandardMaterial with proper PBR properties
        Console.WriteLine($"Floor created at Y=0 with size: {RoomSize.X}x{RoomSize.Z}");
    }
    
    /// <summary>
    /// Create visual boundaries for the room
    /// </summary>
    private void SetupRoomBoundaries()
    {
        if (_scene == null) return;
        
        // Create simple line boundaries or wall planes
        // For now, we'll create thin wall planes at the room boundaries
        
        float wallThickness = 0.1f;
        float wallHeight = RoomSize.Y;
        
        // Back wall (-Z)
        CreateWall("BackWall", 
            new Vector3(0f, wallHeight / 2f, -RoomSize.Z / 2f),
            new Vector3(RoomSize.X, wallHeight, wallThickness));
        
        // Front wall (+Z)
        CreateWall("FrontWall", 
            new Vector3(0f, wallHeight / 2f, RoomSize.Z / 2f),
            new Vector3(RoomSize.X, wallHeight, wallThickness));
        
        // Left wall (-X)
        CreateWall("LeftWall", 
            new Vector3(-RoomSize.X / 2f, wallHeight / 2f, 0f),
            new Vector3(wallThickness, wallHeight, RoomSize.Z));
        
        // Right wall (+X)
        CreateWall("RightWall", 
            new Vector3(RoomSize.X / 2f, wallHeight / 2f, 0f),
            new Vector3(wallThickness, wallHeight, RoomSize.Z));
        
        Console.WriteLine($"Room boundaries created: {RoomSize.X}x{RoomSize.Y}x{RoomSize.Z}");
    }
    
    /// <summary>
    /// Helper method to create a wall entity
    /// </summary>
    private void CreateWall(string name, Vector3 position, Vector3 scale)
    {
        if (_scene == null) return;
        
        var wall = new Entity(name)
            .AddComponent(new Transform3D
            {
                LocalPosition = position,
                LocalScale = scale
            })
            .AddComponent(new MaterialComponent())
            .AddComponent(new CubeMesh
            {
                Size = 1f
            })
            .AddComponent(new MeshRenderer());
        
        _scene.Managers.EntityManager.Add(wall);
    }
    
    /// <summary>
    /// Set up the environment background (skybox or gradient)
    /// </summary>
    private void SetupEnvironment()
    {
        if (_scene == null) return;
        
        // For now, we rely on the camera's background color
        // In a full implementation, this would create a skybox using:
        // - A skybox material with cube textures
        // - Or an environment map for reflections
        // - Or a gradient shader for procedural sky
        
        Console.WriteLine("Environment background configured via camera");
    }
    
    /// <summary>
    /// Get the main camera entity
    /// </summary>
    public Entity? GetCamera()
    {
        return _cameraEntity;
    }
    
    /// <summary>
    /// Update camera aspect ratio when window is resized
    /// </summary>
    public void UpdateCameraAspectRatio(float width, float height)
    {
        if (_cameraEntity != null && width > 0 && height > 0)
        {
            var camera = _cameraEntity.FindComponent<Camera3D>();
            if (camera != null)
            {
                camera.AspectRatio = width / height;
                Console.WriteLine($"Camera aspect ratio updated: {camera.AspectRatio:F2}");
            }
        }
    }
}
