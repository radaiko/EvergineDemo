using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.AspNetCore.SignalR.Client;
using EvergineDemo.Shared.Models;
using System;
using System.IO;
using System.Linq;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

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

    public MainWindowViewModel()
    {
        // Don't auto-connect anymore, let user configure server URL first
    }

    [RelayCommand]
    private async Task LoadStlAsync()
    {
        try
        {
            // For demo purposes, create a mock STL file upload
            // In a real implementation, use a file picker dialog
            StatusText = "Loading STL file...";

            var fileName = $"model_{DateTime.Now:HHmmss}.stl";
            var mockStlContent = "mock stl content"; // Replace with actual file picker
            var base64Content = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(mockStlContent));

            var request = new UploadStlRequest
            {
                FileName = fileName,
                FileContent = base64Content
            };

            // Send to backend - use clean URL
            var cleanUrl = ServerUrl.TrimEnd('/');
            using var httpClient = new System.Net.Http.HttpClient();
            var response = await httpClient.PostAsJsonAsync($"{cleanUrl}/api/model/upload", request);
            
            if (response.IsSuccessStatusCode)
            {
                StatusText = $"Successfully loaded {fileName}";
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
            });

            _hubConnection.On<ModelState>("ModelAdded", (model) =>
            {
                Models.Add(model);
                StatusText = $"New model added: {model.FileName}";
            });

            _hubConnection.On<string>("ModelRemoved", (modelId) =>
            {
                var model = Models.FirstOrDefault(m => m.Id == modelId);
                if (model != null)
                {
                    Models.Remove(model);
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
}
