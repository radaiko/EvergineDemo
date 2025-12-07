using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.AspNetCore.SignalR.Client;
using EvergineDemo.Shared.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace EvergineDemo.Frontend.CrossPlatform.ViewModels;

public partial class MainViewModel : ViewModelBase
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
    private readonly System.Net.Http.HttpClient _httpClient = new();

    public MainViewModel()
    {
        // Don't auto-connect, let user configure server URL first
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _httpClient?.Dispose();
        }
        base.Dispose(disposing);
    }

    [RelayCommand]
    private async Task LoadStlAsync()
    {
        try
        {
            StatusText = "File upload not yet implemented for web platform";
            // TODO: Implement file upload for browser platform
            // This requires different approach than desktop (HTML file input)
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
                Console.WriteLine($"[MainViewModel] Received room state update: {roomState.Models.Count} models");
                Models.Clear();
                foreach (var model in roomState.Models)
                {
                    Models.Add(model);
                }
            });

            _hubConnection.On<ModelState>("ModelAdded", (model) =>
            {
                Console.WriteLine($"[MainViewModel] Model added: {model.Id} - {model.FileName}");
                Models.Add(model);
                StatusText = $"New model added: {model.FileName}";
            });

            _hubConnection.On<string>("ModelRemoved", (modelId) =>
            {
                Console.WriteLine($"[MainViewModel] Model removed: {modelId}");
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
            Console.WriteLine($"[MainViewModel] Successfully connected to {cleanUrl}");
        }
        catch (Exception ex)
        {
            StatusText = $"Connection failed: {ex.Message}";
            IsConnected = false;
            Console.WriteLine($"[MainViewModel] Connection failed: {ex.Message}");
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
    /// Handle model click and send to backend
    /// </summary>
    public async Task HandleModelClickAsync(string modelId)
    {
        if (!IsConnected || _hubConnection == null)
        {
            StatusText = "Cannot click model: not connected to server";
            return;
        }

        try
        {
            await _hubConnection.InvokeAsync("ModelClicked", modelId);
            
            var model = Models.FirstOrDefault(m => m.Id == modelId);
            if (model != null)
            {
                StatusText = $"Clicked model: {model.FileName}";
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Error clicking model: {ex.Message}";
        }
    }
}
