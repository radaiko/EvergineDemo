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
                
                // Connect the rendering services to the Evergine control
                var evergineControl = this.FindControl<EvergineControl>("EvergineViewport");
                if (evergineControl != null)
                {
                    evergineControl.SetRenderingService(_renderingService);
                    evergineControl.SetModelRenderingService(_modelRenderingService);
                }
            }
        };

        // Clean up on close
        Closing += async (s, e) =>
        {
            _renderingService?.Dispose();
            
            if (DataContext is MainWindowViewModel viewModel)
            {
                try
                {
                    await viewModel.DisconnectAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error disconnecting: {ex.Message}");
                }
            }
        };
    }
}