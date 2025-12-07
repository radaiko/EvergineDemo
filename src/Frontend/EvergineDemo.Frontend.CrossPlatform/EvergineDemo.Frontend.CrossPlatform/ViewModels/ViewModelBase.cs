using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace EvergineDemo.Frontend.CrossPlatform.ViewModels;

public abstract class ViewModelBase : ObservableObject, IDisposable
{
    /// <summary>
    /// Dispose of resources
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Dispose pattern implementation
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        // Base implementation does nothing
        // Derived classes can override to clean up resources
    }
}
