using Avalonia.Controls;
using EvergineDemo.Frontend.Desktop.ViewModels;

namespace EvergineDemo.Frontend.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        // Set the storage provider in the ViewModel when the window is opened
        Opened += (s, e) =>
        {
            if (DataContext is MainWindowViewModel viewModel)
            {
                viewModel.SetStorageProvider(StorageProvider);
            }
        };
    }
}