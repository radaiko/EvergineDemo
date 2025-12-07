using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Avalonia.Threading;
using EvergineDemo.Frontend.Desktop.Services;
using System;

namespace EvergineDemo.Frontend.Desktop.Controls;

/// <summary>
/// Avalonia control that hosts Evergine 3D rendering using OpenGL
/// </summary>
public class EvergineControl : OpenGlControlBase
{
    private EvergineRenderingService? _renderingService;
    private ModelRenderingService? _modelRenderingService;
    private bool _initialized = false;
    private bool _firstRender = true;
    private float _rotation = 0f;

    public EvergineControl()
    {
        // Request rendering updates at 60 FPS
        var timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16) // ~60 FPS
        };
        timer.Tick += (s, e) =>
        {
            _rotation += 0.016f; // Rotate over time
            RequestNextFrameRendering();
        };
        timer.Start();
    }

    /// <summary>
    /// Set the rendering service that manages the Evergine scene
    /// </summary>
    public void SetRenderingService(EvergineRenderingService service)
    {
        _renderingService = service;
    }

    /// <summary>
    /// Set the model rendering service that manages STL model data
    /// </summary>
    public void SetModelRenderingService(ModelRenderingService service)
    {
        _modelRenderingService = service;
    }

    /// <summary>
    /// Called when OpenGL context is ready for initialization
    /// </summary>
    protected override void OnOpenGlInit(GlInterface gl)
    {
        base.OnOpenGlInit(gl);

        if (_renderingService == null)
        {
            return;
        }

        try
        {
            // Enable depth testing for 3D rendering
            gl.Enable(GlConsts.GL_DEPTH_TEST);
            gl.DepthFunc(GlConsts.GL_LESS);

            // Initialize rendering service
            _renderingService.Initialize((int)Bounds.Width, (int)Bounds.Height);
            _initialized = true;
            
            Console.WriteLine("OpenGL context initialized for Evergine rendering");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing OpenGL: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }

    /// <summary>
    /// Called on each frame to render the 3D scene
    /// </summary>
    protected override void OnOpenGlRender(GlInterface gl, int fb)
    {
        if (!_initialized || _renderingService == null)
        {
            // Clear to dark background if not initialized
            gl.ClearColor(0.12f, 0.12f, 0.12f, 1.0f);
            gl.Clear(GlConsts.GL_COLOR_BUFFER_BIT | GlConsts.GL_DEPTH_BUFFER_BIT);
            return;
        }

        try
        {
            // Get scene configuration
            var sceneConfig = _renderingService.GetSceneConfiguration();
            var bgColor = sceneConfig.Camera.BackgroundColor;
            
            // Clear the framebuffer with configured background color
            gl.ClearColor(bgColor.R, bgColor.G, bgColor.B, bgColor.A);
            gl.Clear(GlConsts.GL_COLOR_BUFFER_BIT | GlConsts.GL_DEPTH_BUFFER_BIT);

            // Set up viewport
            var width = (int)Bounds.Width;
            var height = (int)Bounds.Height;
            gl.Viewport(0, 0, width, height);

            // Render 3D scene elements
            // Note: This is a simplified visualization. Full 3D rendering with proper
            // camera projection, lighting, and geometry would require shader programs
            // and vertex buffers. For now, we document the scene configuration.
            
            var models = _renderingService.GetSceneModels();
            
            // Log scene information (floor, lights, camera) on first render
            if (_firstRender)
            {
                _firstRender = false;
                Console.WriteLine($"Scene Configuration:");
                Console.WriteLine($"  Floor: {sceneConfig.RoomSize.X}x{sceneConfig.RoomSize.Z} at Y={sceneConfig.FloorY}");
                Console.WriteLine($"  Camera: Position={sceneConfig.Camera.Position}, FOV={sceneConfig.Camera.FieldOfView}");
                Console.WriteLine($"  Directional Light: Position={sceneConfig.DirectionalLight.Position}, Intensity={sceneConfig.DirectionalLight.Intensity}");
                Console.WriteLine($"  Ambient Light: Position={sceneConfig.AmbientLight.Position}, Range={sceneConfig.AmbientLight.LightRange}");
            }
            
            // Render simple grid visualization (floor at Y=0)
            RenderSceneVisualization(gl, sceneConfig);
            
            // Render models from the model rendering service
            if (_modelRenderingService != null)
            {
                var modelRenderData = _modelRenderingService.GetModelRenderData();
                foreach (var modelData in modelRenderData)
                {
                    RenderModel(gl, modelData);
                }
            }
            else
            {
                // Fallback: Render simple cubes for each model in the scene
                foreach (var model in models)
                {
                    RenderModelPlaceholder(gl, model);
                }
            }

            // Trigger service render callback (for future extensions)
            _renderingService.Render();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error rendering scene: {ex.Message}");
        }
    }

    /// <summary>
    /// Render a simple visualization of the scene (floor grid and room boundaries)
    /// </summary>
    private void RenderSceneVisualization(GlInterface gl, SceneConfiguration sceneConfig)
    {
        // Note: Full 3D rendering would require:
        // 1. Vertex shaders for transforming 3D coordinates to screen space
        // 2. Fragment shaders for lighting calculations
        // 3. Vertex buffers for floor mesh and room boundary geometry
        // 4. Proper camera projection matrix (perspective or orthographic)
        // 5. Model-view transformation matrices
        //
        // For this implementation, we're documenting the scene setup.
        // The scene is configured with:
        // - Floor plane at Y=0, size 10x10 units
        // - Room boundaries (walls) at X,Z = ±5 units
        // - Camera at (15, 8, 15) looking toward origin
        // - Directional light from (5, 10, 5) at 60° down
        // - Point light at (0, 8, 0) with 50-unit range for ambient-like lighting
        //
        // Future work: Implement actual 3D geometry rendering using Evergine's
        // scene graph or custom OpenGL shaders.
    }

    /// <summary>
    /// Render a model with its mesh data and transformations
    /// </summary>
    private void RenderModel(GlInterface gl, ModelRenderingService.ModelRenderData modelData)
    {
        // Note: Full 3D rendering with proper vertex buffers and shaders would be implemented here
        // For now, this documents that the model data (vertices, normals, indices) is available
        // and transformations (position, rotation, scale) are being tracked.
        // 
        // A complete implementation would:
        // 1. Create vertex buffer objects (VBOs) from modelData.Vertices
        // 2. Create element buffer objects (EBOs) from modelData.Indices
        // 3. Set up vertex shaders with MVP (Model-View-Projection) matrices
        // 4. Apply modelData.Position, modelData.Rotation, modelData.Scale to model matrix
        // 5. Set up fragment shaders for lighting using modelData.Normals
        // 6. Bind textures/materials
        // 7. Issue draw call: gl.DrawElements()
        //
        // Current status: Model data is prepared and transformations are synced from backend
        
        if (modelData.HasMeshData)
        {
            // Model has mesh data ready for rendering
            // Vertices: modelData.Vertices (Vector3[])
            // Normals: modelData.Normals (Vector3[])
            // Indices: modelData.Indices (uint[])
            // Position: modelData.Position
            // Rotation: modelData.Rotation (Quaternion)
            // Scale: modelData.Scale
        }
    }

    /// <summary>
    /// Render a placeholder visualization for a model
    /// </summary>
    private void RenderModelPlaceholder(GlInterface gl, EvergineRenderingService.ModelSceneObject model)
    {
        // This is a placeholder for model rendering
        // A full implementation would:
        // 1. Load STL geometry
        // 2. Create vertex buffers
        // 3. Set up shaders
        // 4. Apply transformations based on model.Position, model.Rotation, model.Scale
        // 5. Render the mesh
    }

    /// <summary>
    /// Called when control is being cleaned up
    /// </summary>
    protected override void OnOpenGlDeinit(GlInterface gl)
    {
        _initialized = false;
        _renderingService?.Dispose();
        base.OnOpenGlDeinit(gl);
    }

    /// <summary>
    /// Handle size changes for proper viewport adjustment
    /// </summary>
    protected override void OnSizeChanged(Avalonia.Controls.SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        
        if (_initialized && _renderingService != null && e.NewSize.Width > 0 && e.NewSize.Height > 0)
        {
            _renderingService.Resize((int)e.NewSize.Width, (int)e.NewSize.Height);
        }
    }
}

