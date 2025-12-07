using System;
using Avalonia.Controls;
using EvergineDemo.Frontend.Desktop.ViewModels;
using EvergineDemo.Frontend.Desktop.Services;
using EvergineDemo.Frontend.Desktop.Controls;

namespace EvergineDemo.Frontend.Desktop.Views;

public partial class MainWindow : Window
{
    private EvergineRenderingService? _renderingService;
    private ModelRenderingService? _modelRenderingService;
    private RaycastService? _raycastService;

    public MainWindow()
    {
        InitializeComponent();
        
        // Set the storage provider in the ViewModel when the window is opened
        Opened += (s, e) =>
        {
            if (DataContext is MainWindowViewModel viewModel)
            {
                viewModel.SetStorageProvider(StorageProvider);
                
                // Initialize Evergine rendering service
                _renderingService = new EvergineRenderingService();
                viewModel.SetRenderingService(_renderingService);
                
                // Initialize model rendering service
                _modelRenderingService = new ModelRenderingService();
                viewModel.SetModelRenderingService(_modelRenderingService);
                
                // Initialize raycast service
                _raycastService = new RaycastService();
                
                // Connect the rendering services to the Evergine control
                var evergineControl = this.FindControl<EvergineControl>("EvergineViewport");
                if (evergineControl != null)
                {
                    evergineControl.SetRenderingService(_renderingService);
                    evergineControl.SetModelRenderingService(_modelRenderingService);
                    evergineControl.SetRaycastService(_raycastService);
                    
                    // Wire up model click event
                    evergineControl.ModelClicked += async (sender, args) =>
                    {
                        await viewModel.HandleModelClickAsync(args.ModelId);
                    };
                    
                    // Wire up model hover event for visual feedback
                    evergineControl.ModelHovered += (sender, args) =>
                    {
                        if (args.ModelId != null)
                        {
                            viewModel.StatusText = $"Hovering: {args.FileName}";
                        }
                        else
                        {
                            viewModel.StatusText = viewModel.IsConnected 
                                ? "Connected to server" 
                                : "Disconnected";
                        }
                    };
                }
            }
        };

        // Clean up on close
        Closing += async (s, e) =>
        {
            _renderingService?.Dispose();
            _modelRenderingService = null; // ModelRenderingService doesn't need disposal, just null reference
            
            if (DataContext is MainWindowViewModel viewModel)
            {
                try
                {
                    await viewModel.DisconnectAsync();
                    viewModel.Dispose(); // Dispose the ViewModel to clean up HttpClient
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error disconnecting: {ex.Message}");
                }
            }
        };
    }
}