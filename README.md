# Evergine 3D Demo Application

A cross-platform 3D demonstration application built with .NET 10, Evergine 3D engine, and Avalonia UI.

## Overview

This project demonstrates a synchronized 3D rendering system where:
- Multiple clients can connect to a central backend server
- Users can load STL models via a UI button
- Models spin continuously in a 3D room (1 rotation per 10 seconds)
- Clicking a model causes it to drop to the floor with physics simulation
- All connected clients see synchronized rendering in real-time

## Architecture

### Backend (`src/Backend/EvergineDemo.Backend`)
- **ASP.NET Core Web API** (.NET 10)
- **SignalR Hub** for real-time communication
- **SimulationService** - Hosted service that manages the 3D simulation state
  - Continuous physics simulation at 60Hz
  - Gravity simulation for falling objects
  - Automatic rotation of models (π/5 radians per second = 1 rotation per 10 seconds)
- **ModelController** - REST API for uploading STL files

### Shared Library (`src/Shared/EvergineDemo.Shared`)
- **Models** - Shared data transfer objects
  - `ModelState` - Represents a 3D model in the simulation
  - `RoomState` - Complete simulation state
  - `UploadStlRequest` - STL file upload request
  - `ModelClickRequest` - Model interaction request
- **ISimulationHub** - SignalR hub interface for client-server communication

### Frontend (`src/Frontend/EvergineDemo.Frontend.Desktop`)
- **Avalonia UI** - Cross-platform desktop UI framework
- **CommunityToolkit.MVVM** - MVVM pattern implementation
- **Evergine** - 3D rendering engine
- **SignalR Client** - Real-time server communication
- **MainWindowViewModel** - Main application view model with:
  - Server connection management
  - STL file loading functionality
  - Real-time model updates from server
- **EvergineSceneManager** - Manages 3D scene state

## Technologies

- **.NET 10** - Latest .NET framework
- **Evergine 2025.10.21.59** - 3D rendering engine
- **Avalonia UI 11.3.9** - Cross-platform UI framework
- **ASP.NET Core SignalR** - Real-time communication
- **CommunityToolkit.MVVM 8.2.1** - MVVM helpers
- **Evergine.Mathematics** - 3D math library

## Platform Support

The architecture supports the following platforms:

### Current Implementation
- ✅ **Windows** - Desktop application (OpenGL)
- ✅ **macOS** - Desktop application (OpenGL)
- ✅ **Linux** - Desktop application (OpenGL)

### Planned Platform Support
- 🔲 **iOS** - Mobile application (requires iOS workload)
- 🔲 **Android** - Mobile application (requires Android workload)
- 🔲 **WebAssembly** - Browser-based application (requires wasm-tools workload)

**Note:** Cross-platform projects (iOS, Android, Web) are scaffolded but require additional .NET workloads to build. Install them with:
```bash
dotnet workload install android ios wasm-tools
```

## Documentation

- 📖 [Quick Start Guide](QUICKSTART.md) - Get running in 5 minutes
- 🏗️ [Architecture Overview](ARCHITECTURE.md) - Detailed system design
- 💻 [Development Guide](DEVELOPMENT.md) - Developer workflows and best practices
- 🌍 [Cross-Platform Support](CROSS_PLATFORM.md) - Platform-specific details

## Getting Started

### Prerequisites

- .NET 10 SDK
- Visual Studio 2022 / VS Code / Rider (optional)

### Quick Start

See the [Quick Start Guide](QUICKSTART.md) for step-by-step instructions.

### Building the Solution

```bash
# Clone the repository
git clone https://github.com/radaiko/EvergineDemo.git
cd EvergineDemo

# Build the solution
dotnet build

# Or build specific projects
dotnet build src/Backend/EvergineDemo.Backend
dotnet build src/Frontend/EvergineDemo.Frontend.Desktop
```

### Running the Application

1. **Start the Backend Server**:
```bash
cd src/Backend/EvergineDemo.Backend
dotnet run
```
The server will start on `http://localhost:5000`

2. **Start the Frontend Client** (in a new terminal):
```bash
cd src/Frontend/EvergineDemo.Frontend.Desktop
dotnet run
```

3. **Using the Application**:
   - The client automatically connects to the backend server
   - Click "Load STL Model" to add a new model to the simulation
   - Models will spin continuously (1 rotation per 10 seconds)
   - Click on a model in the 3D view to drop it to the floor
   - Multiple clients can connect and will see synchronized updates

## Project Structure

```
EvergineDemo/
├── src/
│   ├── Backend/
│   │   └── EvergineDemo.Backend/
│   │       ├── Controllers/          # REST API controllers
│   │       ├── Hubs/                 # SignalR hubs
│   │       ├── Services/             # Background services
│   │       └── Program.cs            # Application entry point
│   ├── Shared/
│   │   └── EvergineDemo.Shared/
│   │       ├── Models/               # Shared DTOs
│   │       └── Hubs/                 # SignalR interfaces
│   └── Frontend/
│       ├── EvergineDemo.Frontend.Desktop/
│       │   ├── ViewModels/           # MVVM view models
│       │   ├── Views/                # Avalonia UI views
│       │   ├── Services/             # Frontend services
│       │   └── Program.cs            # Application entry point
│       ├── EvergineDemo.Frontend.Mobile/    # (Planned)
│       └── EvergineDemo.Frontend.Web/       # (Planned)
├── EvergineDemo.sln
└── README.md
```

## Key Features

### Real-Time Synchronization
- SignalR-based communication ensures all clients see the same simulation state
- 60Hz physics simulation on the server
- ~6Hz update broadcast to minimize bandwidth while maintaining smooth visuals

### Physics Simulation
- Gravity: -9.81 m/s²
- Angular velocity: π/5 rad/s (36° per second)
- Floor collision detection
- Smooth interpolation for visual updates

### Cross-Platform UI
- Avalonia UI provides native look and feel on all platforms
- Responsive layout adapts to different screen sizes
- Modern, dark-themed interface

### 3D Rendering
- Evergine engine for high-performance 3D rendering
- Support for STL model format
- OpenGL backend for broad compatibility

## Development

### Adding New Platforms

To add support for iOS, Android, or Web:

1. Create a new project in `src/Frontend/`
2. Add Evergine platform-specific packages:
   - iOS: `Evergine.iOS`
   - Android: `Evergine.Android`
   - Web: `Evergine.Web`, `Evergine.Targets.Web`
3. Reference the `EvergineDemo.Shared` project
4. Implement platform-specific initialization
5. Add to the solution file

### Extending the Simulation

The simulation can be extended by:
- Modifying `SimulationService.cs` for different physics behaviors
- Adding new model types in the `Shared/Models` directory
- Implementing additional SignalR hub methods for new interactions

## API Endpoints

### REST API
- `POST /api/model/upload` - Upload an STL file
- `GET /api/model/state` - Get current room state

### SignalR Hub
- `/simulationHub` - Real-time simulation updates
  - `ReceiveRoomState(RoomState)` - Complete state update
  - `ModelAdded(ModelState)` - New model notification
  - `ModelRemoved(string)` - Model removal notification
  - `ModelClicked(string)` - Send model click to server

## License

MIT License - See LICENSE file for details

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.