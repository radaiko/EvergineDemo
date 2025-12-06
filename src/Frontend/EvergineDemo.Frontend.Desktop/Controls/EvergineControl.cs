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
    private bool _initialized = false;
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
            // Clear the framebuffer with a dark blue/gray color
            gl.ClearColor(0.12f, 0.12f, 0.15f, 1.0f);
            gl.Clear(GlConsts.GL_COLOR_BUFFER_BIT | GlConsts.GL_DEPTH_BUFFER_BIT);

            // Set up viewport
            var width = (int)Bounds.Width;
            var height = (int)Bounds.Height;
            gl.Viewport(0, 0, width, height);

            // Render 3D scene
            // For now, we'll render a simple visualization
            // In a full implementation, this would iterate through models and render them
            
            var models = _renderingService.GetSceneModels();
            
            // Simple visualization: draw a grid to show the 3D space is working
            RenderGrid(gl);
            
            // Render simple cubes for each model in the scene
            foreach (var model in models)
            {
                RenderModelPlaceholder(gl, model);
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
    /// Render a simple grid to visualize the 3D space
    /// </summary>
    private void RenderGrid(GlInterface gl)
    {
        // This is a placeholder for grid rendering
        // A full implementation would use shaders and vertex buffers
        // For now, we just clear to indicate the system is working
    }

    /// <summary>
    /// Render a placeholder visualization for a model
    /// </summary>
    private void RenderModelPlaceholder(GlInterface gl, object model)
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

