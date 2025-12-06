# Quick Start Guide

Get the Evergine 3D Demo application running in 5 minutes!

## Prerequisites

You need:
- [.NET 10 SDK](https://dotnet.microsoft.com/download) installed
- A terminal/command prompt
- A code editor (optional)

Verify .NET 10 is installed:
```bash
dotnet --version
# Should show 10.0.x
```

## Step 1: Clone the Repository

```bash
git clone https://github.com/radaiko/EvergineDemo.git
cd EvergineDemo
```

## Step 2: Start the Backend Server

Open a terminal and run:

```bash
cd src/Backend/EvergineDemo.Backend
dotnet run
```

You should see:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
info: EvergineDemo.Backend.Services.SimulationService[0]
      Simulation service starting
```

**Leave this terminal running!** The backend server needs to stay active.

## Step 3: Start the Desktop Client

Open a **new terminal** (keep the backend running) and run:

```bash
cd src/Frontend/EvergineDemo.Frontend.Desktop
dotnet run
```

The Avalonia UI window should open automatically.

## Step 4: Test the Application

1. The client should show "Connected to server" in the status bar
2. Click the **"Load STL Model"** button to add a test model
3. The model will appear in the simulation and spin automatically
4. Click on the model in the 3D view to drop it to the floor (placeholder)
5. Open another client in a new terminal to see synchronized updates

## Troubleshooting

### "Connection Failed" Error

**Problem:** Client can't connect to the backend.

**Solution:**
1. Ensure the backend is running on http://localhost:5000
2. Check that no firewall is blocking port 5000
3. Try restarting the backend server

### Build Errors

**Problem:** Projects fail to build.

**Solution:**
```bash
# Clean and restore
dotnet clean
dotnet restore
dotnet build src/Backend/EvergineDemo.Backend/EvergineDemo.Backend.csproj
dotnet build src/Frontend/EvergineDemo.Frontend.Desktop/EvergineDemo.Frontend.Desktop.csproj
```

### Cross-Platform Projects Won't Build

**Problem:** Android/iOS/Web projects fail with "workload not installed" error.

**Solution:** These projects require additional workloads:
```bash
dotnet workload install android ios wasm-tools
```

Or, skip them and just build the desktop projects directly:
```bash
dotnet build src/Backend/EvergineDemo.Backend/EvergineDemo.Backend.csproj
dotnet build src/Frontend/EvergineDemo.Frontend.Desktop/EvergineDemo.Frontend.Desktop.csproj
```

### Port Already In Use

**Problem:** Backend fails to start with "Address already in use" error.

**Solution:**
1. Stop any other process using port 5000
2. Or, change the port in `src/Backend/EvergineDemo.Backend/Properties/launchSettings.json`

## What's Happening Behind the Scenes?

### Backend Server
- Runs a 60Hz physics simulation
- Manages the 3D room state
- Broadcasts updates to all connected clients via SignalR
- Handles STL file uploads via REST API

### Desktop Client
- Connects to the backend via SignalR
- Receives real-time updates of the room state
- Displays 3D viewport (placeholder for now)
- Sends STL upload requests to the backend

### Synchronized Simulation
- All clients see the same spinning models
- Models rotate at exactly 1 rotation per 10 seconds
- Clicking a model drops it with simulated gravity
- Models stop when they hit the floor

## Next Steps

1. **Explore the Code**
   - Backend: `src/Backend/EvergineDemo.Backend/Services/SimulationService.cs`
   - Frontend: `src/Frontend/EvergineDemo.Frontend.Desktop/ViewModels/MainWindowViewModel.cs`
   - Shared: `src/Shared/EvergineDemo.Shared/Models/`

2. **Read the Docs**
   - [README.md](README.md) - Full project overview
   - [DEVELOPMENT.md](DEVELOPMENT.md) - Development guide
   - [CROSS_PLATFORM.md](CROSS_PLATFORM.md) - Platform support details

3. **Try Multiple Clients**
   ```bash
   # Terminal 1: Backend
   cd src/Backend/EvergineDemo.Backend && dotnet run
   
   # Terminal 2: Client 1
   cd src/Frontend/EvergineDemo.Frontend.Desktop && dotnet run
   
   # Terminal 3: Client 2
   cd src/Frontend/EvergineDemo.Frontend.Desktop && dotnet run
   ```
   
   Both clients will see synchronized updates!

4. **Experiment**
   - Try loading multiple models
   - Watch them spin in sync across clients
   - Click models to drop them to the floor

## API Endpoints

Test the REST API directly:

```bash
# Get current room state
curl http://localhost:5000/api/model/state

# Upload a test model
curl -X POST http://localhost:5000/api/model/upload \
  -H "Content-Type: application/json" \
  -d '{"fileName":"test.stl","fileContent":"dGVzdCBjb250ZW50"}'
```

## What's Not Implemented Yet

- ✅ Backend simulation and API - **Complete**
- ✅ Desktop frontend with SignalR - **Complete**
- ✅ Real-time synchronization - **Complete**
- 🔨 Actual 3D rendering with Evergine - **In Progress**
- 🔨 STL file loader - **In Progress**
- 🔨 File picker dialog - **In Progress**
- 🔨 Click detection in 3D scene - **In Progress**
- 📋 iOS/Android/Web implementations - **Planned**

## Getting Help

- Review the [DEVELOPMENT.md](DEVELOPMENT.md) for detailed development info
- Check [Issues](https://github.com/radaiko/EvergineDemo/issues) for known problems
- Open a new issue if you find a bug

## Contributing

Contributions welcome! See [DEVELOPMENT.md](DEVELOPMENT.md) for code style and workflow.

Happy coding! 🚀
