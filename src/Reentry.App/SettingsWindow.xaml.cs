using Microsoft.UI.Xaml;
using Reentry.App.ViewModels;
using WinRT.Interop;

namespace Reentry.App;

public sealed partial class SettingsWindow : Window
{
    public SettingsWindow(SettingsViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        Title = "Reentry settings";
        SystemBackdrop = new Microsoft.UI.Xaml.Media.MicaBackdrop();
    }

    public SettingsViewModel ViewModel { get; }
    public nint Handle => WindowNative.GetWindowHandle(this);

    private void Toggle_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is Microsoft.UI.Xaml.FrameworkElement fe && fe.Tag is InventoryRow row)
            ViewModel.Toggle(row);
    }

    private void Remove_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is Microsoft.UI.Xaml.FrameworkElement fe && fe.Tag is InventoryRow row)
            ViewModel.RemoveManaged(row);
    }
}

