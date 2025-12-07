using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.AspNetCore.SignalR.Client;
using EvergineDemo.Shared.Models;
using EvergineDemo.Frontend.Desktop.Services;
using System;
using System.IO;
using System.Linq;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Avalonia.Platform.Storage;

namespace EvergineDemo.Frontend.Desktop.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _statusText = "Disconnected";

    [ObservableProperty]
    private bool _isConnected = false;

    [ObservableProperty]
    private string _serverUrl = "http://localhost:5000";

    [ObservableProperty]
    private ObservableCollection<ModelState> _models = new();

    private HubConnection? _hubConnection;
    private IStorageProvider? _storageProvider;
    private EvergineRenderingService? _renderingService;
    private ModelRenderingService? _modelRenderingService;
    private readonly System.Net.Http.HttpClient _httpClient = new();

    public MainWindowViewModel()
    {
        // Don't auto-connect anymore, let user configure server URL first
    }

    /// <summary>
    /// Set the rendering service for 3D visualization
    /// </summary>
    public void SetRenderingService(EvergineRenderingService renderingService)
    {
        _renderingService = renderingService;
    }

    /// <summary>
    /// Set the model rendering service for STL model rendering
    /// </summary>
    public void SetModelRenderingService(ModelRenderingService modelRenderingService)
    {
        _modelRenderingService = modelRenderingService;
    }

    /// <summary>
    /// Set the storage provider for file picker operations
    /// </summary>
    public void SetStorageProvider(IStorageProvider storageProvider)
    {
        _storageProvider = storageProvider;
    }

    [RelayCommand]
    private async Task LoadStlAsync()
    {
        try
        {
            if (_storageProvider == null)
            {
                StatusText = "Storage provider not initialized";
                return;
            }

            // Open file picker dialog
            var fileTypes = new FilePickerFileType[]
            {
                new("STL Files")
                {
                    Patterns = new[] { "*.stl", "*.STL" },
                    MimeTypes = new[] { "model/stl", "application/sla" }
                },
                new("All Files")
                {
                    Patterns = new[] { "*.*" }
                }
            };

            var options = new FilePickerOpenOptions
            {
                Title = "Select STL File",
                AllowMultiple = false,
                FileTypeFilter = fileTypes
            };

            var files = await _storageProvider.OpenFilePickerAsync(options);
            
            if (files == null || files.Count == 0)
            {
                StatusText = "No file selected";
                return;
            }

            var file = files[0];
            StatusText = $"Loading {file.Name}...";

            // Read file content
            await using var stream = await file.OpenReadAsync();
            
            // Check file size (limit to 100MB for safety)
            const long maxFileSize = 100 * 1024 * 1024; // 100 MB
            if (stream.Length > maxFileSize)
            {
                StatusText = $"File too large. Maximum size is 100MB.";
                return;
            }

            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            var fileBytes = memoryStream.ToArray();

            // Convert to base64
            var base64Content = Convert.ToBase64String(fileBytes);

            var request = new UploadStlRequest
            {
                FileName = file.Name,
                FileContent = base64Content
            };

            // Send to backend - use clean URL
            var cleanUrl = ServerUrl.TrimEnd('/');
            var response = await _httpClient.PostAsJsonAsync($"{cleanUrl}/api/model/upload", request);
            
            if (response.IsSuccessStatusCode)
            {
                StatusText = $"Successfully loaded {file.Name}";
            }
            else
            {
                StatusText = $"Failed to load STL: {response.ReasonPhrase}";
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ConnectToServerAsync()
    {
        try
        {
            // Validate server URL
            if (string.IsNullOrWhiteSpace(ServerUrl))
            {
                StatusText = "Please enter a server URL";
                return;
            }

            // Ensure URL doesn't end with a slash
            var cleanUrl = ServerUrl.TrimEnd('/');

            // Validate URL format
            if (!Uri.TryCreate(cleanUrl, UriKind.Absolute, out var uri) || 
                (uri.Scheme != "http" && uri.Scheme != "https"))
            {
                StatusText = "Please enter a valid HTTP or HTTPS URL";
                return;
            }

            StatusText = $"Connecting to {cleanUrl}...";

            _hubConnection = new HubConnectionBuilder()
                .WithUrl($"{cleanUrl}/simulationHub")
                .WithAutomaticReconnect()
                .Build();

            _hubConnection.On<RoomState>("ReceiveRoomState", (roomState) =>
            {
                Models.Clear();
                foreach (var model in roomState.Models)
                {
                    Models.Add(model);
                }
                
                // Update the 3D scene with model transformations
                _renderingService?.UpdateScene(roomState);
                _modelRenderingService?.UpdateModels(roomState);
            });

            _hubConnection.On<ModelState>("ModelAdded", async (model) =>
            {
                Models.Add(model);
                StatusText = $"New model added: {model.FileName}";
                
                // Fetch mesh data for the new model
                await FetchAndRenderModelAsync(model);
                
                // Update the 3D scene with current state
                var roomState = new RoomState { Models = Models.ToList() };
                _renderingService?.UpdateScene(roomState);
            });

            _hubConnection.On<string>("ModelRemoved", (modelId) =>
            {
                var model = Models.FirstOrDefault(m => m.Id == modelId);
                if (model != null)
                {
                    Models.Remove(model);
                    _modelRenderingService?.RemoveModel(modelId);
                }
            });

            _hubConnection.Reconnecting += error =>
            {
                StatusText = "Connection lost. Reconnecting...";
                IsConnected = false;
                return Task.CompletedTask;
            };

            _hubConnection.Reconnected += connectionId =>
            {
                StatusText = "Reconnected to server";
                IsConnected = true;
                return Task.CompletedTask;
            };

            _hubConnection.Closed += error =>
            {
                StatusText = "Disconnected from server";
                IsConnected = false;
                return Task.CompletedTask;
            };

            await _hubConnection.StartAsync();
            IsConnected = true;
            StatusText = "Connected to server";
        }
        catch (Exception ex)
        {
            StatusText = $"Connection failed: {ex.Message}";
            IsConnected = false;
        }
    }

    public async Task DisconnectAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.StopAsync();
            await _hubConnection.DisposeAsync();
            _hubConnection = null;
        }
    }

    /// <summary>
    /// Fetch mesh data for a model and render it
    /// </summary>
    private async Task FetchAndRenderModelAsync(ModelState model)
    {
        if (_modelRenderingService == null)
        {
            return;
        }

        try
        {
            var cleanUrl = ServerUrl.TrimEnd('/');
            var response = await _httpClient.GetAsync($"{cleanUrl}/api/model/{model.Id}/mesh");

            if (response.IsSuccessStatusCode)
            {
                var stlMesh = await response.Content.ReadFromJsonAsync<EvergineDemo.Shared.Models.Stl.StlMesh>();
                if (stlMesh != null)
                {
                    _modelRenderingService.AddOrUpdateModel(model, stlMesh);
                    Console.WriteLine($"Fetched and rendered mesh for model {model.Id}: {stlMesh.Triangles.Count} triangles");
                }
            }
            else
            {
                Console.WriteLine($"Failed to fetch mesh for model {model.Id}: {response.ReasonPhrase}");
                // Still add the model without mesh data (will create placeholder)
                _modelRenderingService.AddOrUpdateModel(model);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching mesh for model {model.Id}: {ex.Message}");
            // Add model without mesh data
            _modelRenderingService.AddOrUpdateModel(model);
        }
    }
}
