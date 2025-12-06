using Avalonia.Controls;
using EvergineDemo.Frontend.Desktop.ViewModels;
using EvergineDemo.Frontend.Desktop.Services;
using EvergineDemo.Frontend.Desktop.Controls;

namespace EvergineDemo.Frontend.Desktop.Views;

public partial class MainWindow : Window
{
    private EvergineRenderingService? _renderingService;

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
                
                // Connect the rendering service to the Evergine control
                var evergineControl = this.FindControl<EvergineControl>("EvergineViewport");
                if (evergineControl != null)
                {
                    evergineControl.SetRenderingService(_renderingService);
                }
            }
        };

        // Clean up on close
        Closing += (s, e) =>
        {
            _renderingService?.Dispose();
            
            if (DataContext is MainWindowViewModel viewModel)
            {
                _ = viewModel.DisconnectAsync();
            }
        };
    }
}