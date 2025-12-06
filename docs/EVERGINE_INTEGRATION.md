# Evergine Integration with Avalonia UI

This document explains how the Evergine 3D rendering engine is integrated into the Avalonia desktop application.

## Overview

The integration uses Avalonia's built-in OpenGL support through `OpenGlControlBase` to create a rendering surface that can display 3D content using Evergine.

## Architecture

### Components

1. **EvergineControl** (`Controls/EvergineControl.cs`)
   - Custom Avalonia control extending `OpenGlControlBase`
   - Manages the OpenGL rendering context
   - Runs a 60 FPS rendering loop
   - Handles viewport resize events

2. **EvergineRenderingService** (`Services/EvergineRenderingService.cs`)
   - Manages the 3D scene state
   - Synchronizes with backend server via SignalR
   - Thread-safe model tracking
   - Provides scene data to the rendering control

3. **Integration Points**
   - `MainWindow.axaml`: Embeds the EvergineControl in the viewport
   - `MainWindowViewModel`: Connects SignalR events to scene updates
   - `MainWindow.axaml.cs`: Wires up services and controls

### Data Flow

```
Backend Server (SignalR)
    ↓
MainWindowViewModel (ReceiveRoomState)
    ↓
EvergineRenderingService (UpdateScene)
    ↓
EvergineControl (GetSceneModels)
    ↓
OpenGL Rendering (OnOpenGlRender)
```

## Technical Details

### OpenGL Context

The EvergineControl uses Avalonia's OpenGL interop:

```csharp
public class EvergineControl : OpenGlControlBase
{
    protected override void OnOpenGlInit(GlInterface gl)
    {
        // Initialize OpenGL state
        gl.Enable(GlConsts.GL_DEPTH_TEST);
        gl.DepthFunc(GlConsts.GL_LESS);
    }

    protected override void OnOpenGlRender(GlInterface gl, int fb)
    {
        // Clear and render frame
        gl.ClearColor(0.12f, 0.12f, 0.15f, 1.0f);
        gl.Clear(GlConsts.GL_COLOR_BUFFER_BIT | GlConsts.GL_DEPTH_BUFFER_BIT);
        
        // Render 3D scene here
    }
}
```

### Rendering Loop

- Timer triggers frame requests at ~60 FPS
- Each frame calls `OnOpenGlRender`
- Viewport automatically handles double buffering
- No manual swap buffer management needed

### State Synchronization

The rendering service maintains thread-safe scene state:

```csharp
public void UpdateScene(RoomState roomState)
{
    lock (_sceneLock)
    {
        // Update model positions, rotations, scales
        foreach (var model in roomState.Models)
        {
            if (_sceneModels.ContainsKey(model.Id))
            {
                // Update existing
            }
            else
            {
                // Add new
            }
        }
    }
}
```

### Resize Handling

The control automatically handles window resize events:

```csharp
protected override void OnSizeChanged(SizeChangedEventArgs e)
{
    base.OnSizeChanged(e);
    _renderingService?.Resize((int)e.NewSize.Width, (int)e.NewSize.Height);
}
```

## Cross-Platform Compatibility

The implementation works across all platforms:

### Windows
- Uses WGL (Windows OpenGL) backend
- Avalonia automatically selects appropriate backend

### macOS
- Uses CGL (Core OpenGL) backend
- Native Metal can be used in future versions

### Linux
- Uses GLX (X11 OpenGL) backend
- Works with X11 and Wayland

## MVVM Pattern

The integration follows MVVM principles:

- **Model**: `RoomState`, `ModelState` from shared library
- **View**: `MainWindow.axaml` with `EvergineControl`
- **ViewModel**: `MainWindowViewModel` with observable properties

```csharp
// ViewModel connects services
public void SetRenderingService(EvergineRenderingService service)
{
    _renderingService = service;
}

// SignalR events update scene
_hubConnection.On<RoomState>("ReceiveRoomState", (roomState) =>
{
    Models.Clear();
    foreach (var model in roomState.Models)
    {
        Models.Add(model);
    }
    _renderingService?.UpdateScene(roomState);
});
```

## Future Enhancements

### Short Term
1. Implement camera with view and projection matrices
2. Add shader support for model rendering
3. Parse and display STL geometry
4. Implement model picking with raycasting

### Medium Term
1. Add lighting (directional, point, spot)
2. Implement materials and textures
3. Add shadows
4. Optimize rendering with culling

### Long Term
1. Full Evergine scene graph integration
2. Advanced post-processing effects
3. Physics visualization
4. VR/AR support

## Performance Considerations

### Current Implementation
- Rendering: 60 FPS target
- State updates: As received from server (~6 Hz)
- Memory: Minimal overhead, state only

### Optimization Strategies
1. **Frustum Culling**: Don't render off-screen objects
2. **Level of Detail**: Use simpler models at distance
3. **Instancing**: Render multiple copies efficiently
4. **Occlusion Culling**: Skip hidden objects

## Troubleshooting

### Black Screen
- Check OpenGL context initialization
- Verify viewport size is valid
- Check for OpenGL errors in console

### Performance Issues
- Reduce model complexity
- Check for memory leaks
- Profile rendering loop

### Cross-Platform Issues
- Verify OpenGL version support
- Check platform-specific OpenGL extensions
- Test on target platform

## References

- [Avalonia OpenGL Documentation](https://docs.avaloniaui.net/)
- [Evergine Documentation](https://docs.evergine.com/)
- [OpenGL Reference](https://www.opengl.org/sdk/docs/)
- [Avalonia Samples - OpenGL](https://github.com/AvaloniaUI/Avalonia/tree/master/samples)
