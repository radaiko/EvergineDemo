# Development Guide

This guide covers development workflows, architecture decisions, and best practices for the Evergine 3D Demo application.

## Prerequisites

### Required Software

- **.NET 10 SDK** - [Download](https://dotnet.microsoft.com/download)
- **Git** - Version control
- **IDE** (choose one):
  - Visual Studio 2022 (Windows/Mac)
  - JetBrains Rider
  - Visual Studio Code with C# extension

### Optional Software

- **Docker** - For containerized backend deployment
- **Xcode** - For iOS development (macOS only)
- **Android SDK** - For Android development

## Solution Architecture

### Layer Overview

```
┌─────────────────────────────────────────┐
│         Frontend Applications           │
│  (Desktop, Mobile, Web)                 │
└─────────────────┬───────────────────────┘
                  │
                  │ SignalR + REST
                  │
┌─────────────────▼───────────────────────┐
│         Backend Server                  │
│  (ASP.NET Core + SignalR)              │
│                                         │
│  ┌────────────────────────────────┐   │
│  │   Simulation Service           │   │
│  │   (Physics, State Management)  │   │
│  └────────────────────────────────┘   │
└─────────────────────────────────────────┘
```

### Project Dependencies

```
Backend ──────────┐
                  │
                  ├──> Shared
                  │
Desktop Frontend ─┤
Mobile Frontend ──┤
Web Frontend ─────┘
```

## Development Workflow

### 1. Clone and Setup

```bash
git clone https://github.com/radaiko/EvergineDemo.git
cd EvergineDemo
dotnet restore
dotnet build
```

### 2. Running the Backend

```bash
cd src/Backend/EvergineDemo.Backend
dotnet watch run
```

The backend will:
- Start on http://localhost:5000
- Enable hot reload for development
- Expose SignalR hub at /simulationHub
- Expose REST API at /api/model

### 3. Running the Desktop Frontend

In a new terminal:

```bash
cd src/Frontend/EvergineDemo.Frontend.Desktop
dotnet watch run
```

### 4. Making Changes

#### Backend Changes

1. Modify files in `src/Backend/EvergineDemo.Backend`
2. `dotnet watch` automatically rebuilds and restarts
3. Test endpoints with the included `.http` file

#### Frontend Changes

1. Modify ViewModels in `ViewModels/`
2. Modify Views in `Views/`
3. Changes trigger automatic rebuild via `dotnet watch`

#### Shared Model Changes

1. Modify files in `src/Shared/EvergineDemo.Shared`
2. Rebuild all dependent projects:
   ```bash
   dotnet build
   ```

## Code Organization

### Backend

```
src/Backend/EvergineDemo.Backend/
├── Controllers/          # REST API endpoints
│   └── ModelController.cs
├── Hubs/                # SignalR hubs
│   └── SimulationHub.cs
├── Services/            # Background services
│   └── SimulationService.cs
└── Program.cs           # Application entry point
```

### Frontend

```
src/Frontend/EvergineDemo.Frontend.Desktop/
├── ViewModels/          # MVVM ViewModels
│   ├── MainWindowViewModel.cs
│   └── ViewModelBase.cs
├── Views/               # Avalonia UI Views
│   └── MainWindow.axaml
├── Services/            # Frontend services
│   └── EvergineSceneManager.cs
└── Program.cs           # Application entry point
```

### Shared

```
src/Shared/EvergineDemo.Shared/
├── Models/              # Data transfer objects
│   ├── ModelState.cs
│   ├── RoomState.cs
│   └── UploadStlRequest.cs
└── Hubs/                # SignalR interfaces
    └── ISimulationHub.cs
```

## Key Concepts

### SignalR Communication

**Server → Client (Broadcast)**
```csharp
await _hubContext.Clients.All.ReceiveRoomState(roomState);
```

**Client → Server (Invoke)**
```csharp
await _hubConnection.InvokeAsync("ModelClicked", modelId);
```

**Client Receiving Updates**
```csharp
_hubConnection.On<RoomState>("ReceiveRoomState", (roomState) => {
    // Handle update
});
```

### MVVM Pattern

**ViewModel (with CommunityToolkit.MVVM)**
```csharp
public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _statusText;

    [RelayCommand]
    private async Task LoadStlAsync()
    {
        // Command implementation
    }
}
```

**View (XAML)**
```xml
<Button Command="{Binding LoadStlCommand}"
        Content="Load STL" />
<TextBlock Text="{Binding StatusText}" />
```

### Physics Simulation

The simulation runs at 60Hz and updates:

1. **Rotation**: All models spin continuously
   ```csharp
   AngularVelocity = MathF.PI / 5f; // π/5 rad/s = 1 rotation per 10 seconds
   ```

2. **Gravity**: Falling models accelerate downward
   ```csharp
   Gravity = -9.81f; // m/s²
   ```

3. **Collision**: Models stop at floor level
   ```csharp
   if (model.Position.Y <= _roomState.FloorY)
   {
       model.IsFalling = false;
   }
   ```

## Testing

### Unit Testing

(To be implemented)

```bash
dotnet test
```

### Manual Testing

1. Start backend server
2. Start desktop client
3. Click "Load STL Model" button
4. Verify model appears and spins
5. Click on model in 3D view
6. Verify model falls to floor

### Testing SignalR

Use the included `.http` file to test REST endpoints:

```http
POST http://localhost:5000/api/model/upload
Content-Type: application/json

{
  "fileName": "test.stl",
  "fileContent": "dGVzdCBjb250ZW50"
}
```

## Debugging

### Backend Debugging

1. Set breakpoints in Visual Studio/Rider/VS Code
2. Run backend with F5 or debugger
3. Frontend will connect automatically

### Frontend Debugging

1. Set breakpoints in ViewModels
2. Run with F5
3. Debug XAML with Avalonia DevTools (F12)

### SignalR Debugging

Enable detailed logging in `appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.AspNetCore.SignalR": "Debug",
      "Microsoft.AspNetCore.Http.Connections": "Debug"
    }
  }
}
```

## Performance Optimization

### Backend

1. **Throttle Updates**: Broadcast at 6Hz instead of 60Hz
2. **Use Collections**: `ConcurrentDictionary` for thread-safe state
3. **Async Operations**: Use `async/await` throughout

### Frontend

1. **Update Throttling**: Don't update UI every frame
2. **Object Pooling**: Reuse 3D objects when possible
3. **LOD**: Use level-of-detail for complex models

## Common Issues

### "Connection Failed" Error

- Ensure backend is running on http://localhost:5000
- Check firewall settings
- Verify CORS configuration in backend

### Models Not Spinning

- Check `SimulationService` is running
- Verify `AngularVelocity` is set correctly
- Check SignalR connection is active

### Build Errors

```bash
# Clean and rebuild
dotnet clean
dotnet restore
dotnet build
```

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests (when available)
5. Submit a pull request

## Code Style

- Use C# naming conventions (PascalCase for public members)
- Add XML documentation comments for public APIs
- Keep methods small and focused
- Use async/await for I/O operations
- Follow MVVM pattern in frontend code

## Resources

- [Evergine Documentation](https://evergine.net/documentation/)
- [Avalonia UI Documentation](https://docs.avaloniaui.net/)
- [ASP.NET Core SignalR](https://docs.microsoft.com/aspnet/core/signalr/)
- [CommunityToolkit.MVVM](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/)
