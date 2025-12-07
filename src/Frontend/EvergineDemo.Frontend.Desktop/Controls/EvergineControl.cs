using Avalonia.Input;
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
    private RaycastService? _raycastService;
    private bool _initialized = false;
    private bool _firstRender = true;
    private float _rotation = 0f;
    private string? _hoveredModelId = null;
    
    /// <summary>
    /// Event raised when a model is clicked
    /// </summary>
    public event EventHandler<ModelClickedEventArgs>? ModelClicked;
    
    /// <summary>
    /// Event raised when the hovered model changes
    /// </summary>
    public event EventHandler<ModelHoveredEventArgs>? ModelHovered;

    public EvergineControl()
    {
        // Set initial cursor to arrow (will change to hand on hover)
        Cursor = new Cursor(StandardCursorType.Arrow);
        
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
    /// Set the raycast service for model picking
    /// </summary>
    public void SetRaycastService(RaycastService service)
    {
        _raycastService = service;
    }
    
    /// <summary>
    /// Handle pointer moved event for hover detection
    /// </summary>
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        
        if (!_initialized || _raycastService == null || _modelRenderingService == null || _renderingService == null)
        {
            return;
        }

        var point = e.GetPosition(this);
        PerformRaycast(point.X, point.Y, isClick: false);
    }
    
    /// <summary>
    /// Handle pointer pressed event for click detection
    /// </summary>
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        
        if (!_initialized || _raycastService == null || _modelRenderingService == null || _renderingService == null)
        {
            return;
        }

        // Only handle left mouse button clicks
        var point = e.GetCurrentPoint(this);
        if (point.Properties.IsLeftButtonPressed)
        {
            PerformRaycast(point.Position.X, point.Position.Y, isClick: true);
        }
    }
    
    /// <summary>
    /// Perform raycast from screen coordinates
    /// </summary>
    private void PerformRaycast(double screenX, double screenY, bool isClick)
    {
        if (_raycastService == null || _modelRenderingService == null || _renderingService == null)
        {
            return;
        }

        var sceneConfig = _renderingService.GetSceneConfiguration();
        var models = _modelRenderingService.GetModelRenderData();
        
        if (models.Count == 0)
        {
            return;
        }

        // Create ray from screen coordinates
        var ray = _raycastService.CreateRayFromScreenPoint(
            (float)screenX, (float)screenY,
            (float)Bounds.Width, (float)Bounds.Height,
            sceneConfig.Camera.Position,
            sceneConfig.Camera.Orientation,
            sceneConfig.Camera.FieldOfView
        );

        // Raycast against all models
        var hits = _raycastService.RaycastModels(ray, models);

        if (isClick)
        {
            // Handle click - pick the closest model
            if (hits.Count > 0)
            {
                var closestHit = hits[0];
                ModelClicked?.Invoke(this, new ModelClickedEventArgs
                {
                    ModelId = closestHit.ModelId,
                    FileName = closestHit.FileName
                });
            }
        }
        else
        {
            // Handle hover - update cursor and raise hover event
            string? newHoveredModelId = hits.Count > 0 ? hits[0].ModelId : null;
            
            if (_hoveredModelId != newHoveredModelId)
            {
                _hoveredModelId = newHoveredModelId;
                
                ModelHovered?.Invoke(this, new ModelHoveredEventArgs
                {
                    ModelId = _hoveredModelId,
                    FileName = hits.Count > 0 ? hits[0].FileName : null
                });
                
                // Update cursor
                Cursor = _hoveredModelId != null 
                    ? new Cursor(StandardCursorType.Hand) 
                    : new Cursor(StandardCursorType.Arrow);
            }
        }
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
            
            Console.WriteLine($"[EvergineControl] OpenGL context initialized for Evergine rendering. Viewport size: {(int)Bounds.Width}x{(int)Bounds.Height}");
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
            
            if (_firstRender)
            {
                _firstRender = false;
                Console.WriteLine("[EvergineControl] Rendering not initialized yet - showing dark background");
            }
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
                Console.WriteLine($"[EvergineControl] === Scene Configuration ===");
                Console.WriteLine($"[EvergineControl] Floor: {sceneConfig.RoomSize.X}x{sceneConfig.RoomSize.Z} at Y={sceneConfig.FloorY}");
                Console.WriteLine($"[EvergineControl] Camera: Position={sceneConfig.Camera.Position}, FOV={sceneConfig.Camera.FieldOfView}");
                Console.WriteLine($"[EvergineControl] Background Color: R={bgColor.R:F2}, G={bgColor.G:F2}, B={bgColor.B:F2}");
                Console.WriteLine($"[EvergineControl] Directional Light: Position={sceneConfig.DirectionalLight.Position}, Intensity={sceneConfig.DirectionalLight.Intensity}");
                Console.WriteLine($"[EvergineControl] Ambient Light: Position={sceneConfig.AmbientLight.Position}, Range={sceneConfig.AmbientLight.LightRange}");
                Console.WriteLine($"[EvergineControl] Models in scene: {models.Count}");
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
        // Note: Avalonia's OpenGL support uses modern OpenGL (3.0+) which requires shaders.
        // Legacy immediate mode functions (glBegin/glEnd, glVertex, glColor, glMatrixMode, etc.) 
        // are not available in the GlInterface.
        //
        // Full 3D rendering requires:
        // 1. Vertex shaders (GLSL) for transforming 3D coordinates to screen space
        // 2. Fragment shaders (GLSL) for per-pixel color/lighting calculations
        // 3. Vertex Buffer Objects (VBOs) for storing geometry data
        // 4. Proper camera projection matrix (perspective)
        // 5. Model-view transformation matrices
        //
        // The scene is configured with:
        // - Floor plane at Y=0, size 10x10 units
        // - Room boundaries (walls) at X,Z = ±5 units
        // - Camera at (15, 8, 15) looking toward origin
        // - Directional light from (5, 10, 5) at 60° down
        // - Point light at (0, 8, 0) with 50-unit range for ambient-like lighting
        //
        // For now, we show a colored background to indicate the rendering system is active.
        // Future work: Implement shader-based 3D geometry rendering.
    }

    /// <summary>
    /// Render a model with its mesh data and transformations
    /// </summary>
    private void RenderModel(GlInterface gl, ModelRenderingService.ModelRenderData modelData)
    {
        // Note: Rendering with modern OpenGL requires shaders and VBOs
        // Avalonia's GlInterface doesn't support legacy immediate mode OpenGL
        //
        // Full 3D rendering with proper vertex buffers and shaders would:
        // 1. Create vertex buffer objects (VBOs) from modelData.Vertices
        // 2. Create element buffer objects (EBOs) from modelData.Indices
        // 3. Set up vertex shaders with MVP (Model-View-Projection) matrices
        // 4. Apply modelData.Position, modelData.Rotation, modelData.Scale to model matrix
        // 5. Set up fragment shaders for lighting using modelData.Normals
        // 6. Bind textures/materials
        // 7. Issue draw call: gl.DrawElements()
        //
        // Current status: Model data is prepared and transformations are synced from backend
        //
        // RENDERING STATUS:
        // ✓ STL mesh data parsed and converted to Evergine format
        // ✓ Vertices, normals, and indices ready for GPU upload
        // ✓ Position, rotation (spinning), and scale synchronized from backend
        // ✓ Falling animation state tracked
        // ✓ Multiple models supported
        // ✓ Removed models cleaned up
        // ✓ Background color now visible (grey instead of white)
        // - OpenGL vertex buffers and shaders (requires additional implementation)
        
        if (modelData.HasMeshData)
        {
            // Model has complete mesh data ready for OpenGL rendering
            // Geometry: modelData.Vertices (Vector3[]) - vertex positions
            // Lighting: modelData.Normals (Vector3[]) - per-vertex normals for shading
            // Indices: modelData.Indices (uint[]) - triangle indices for indexed drawing
            // Transform: 
            //   - Position: modelData.Position (synced from SimulationService)
            //   - Rotation: modelData.Rotation (spinning at π/5 rad/s = 1 rotation per 10s)
            //   - Scale: modelData.Scale (uniform scaling)
        }
    }

    /// <summary>
    /// Render a placeholder visualization for a model
    /// </summary>
    private void RenderModelPlaceholder(GlInterface gl, EvergineRenderingService.ModelSceneObject model)
    {
        // Note: Model rendering requires modern OpenGL with shaders
        // See RenderModel method for details on what's needed
        // For now, models are tracked and their transformations are synced from the backend
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

/// <summary>
/// Event arguments for model clicked event
/// </summary>
public class ModelClickedEventArgs : EventArgs
{
    public string ModelId { get; set; } = string.Empty;
    public string? FileName { get; set; }
}

/// <summary>
/// Event arguments for model hovered event
/// </summary>
public class ModelHoveredEventArgs : EventArgs
{
    public string? ModelId { get; set; }
    public string? FileName { get; set; }
}

