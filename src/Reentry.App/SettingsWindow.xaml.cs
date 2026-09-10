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
        Title = AppVersion.Moniker + " settings";
        SystemBackdrop = new Microsoft.UI.Xaml.Media.MicaBackdrop();
        WindowIcon.Apply(this);
    }

    public SettingsViewModel ViewModel { get; }
    public nint Handle => WindowNative.GetWindowHandle(this);

    private void ProduceChecklist_Click(object sender, RoutedEventArgs e)
        => (Application.Current as App)?.ProduceChecklist();

    private void Toggle_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is InventoryRow row)
            ViewModel.Toggle(row);
    }

    private void Remove_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is InventoryRow row)
            ViewModel.RemoveManaged(row);
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SaveAutostart();
        (Application.Current as App)?.CloseSettingsAndShowHud();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
        => (Application.Current as App)?.CloseSettingsAndShowHud();
}