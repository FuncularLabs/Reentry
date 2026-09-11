using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Reentry.App.ViewModels;
using WinRT.Interop;

namespace Reentry.App;

public sealed partial class MainWindow : Window
{
    public MainWindow(HudViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        Title = AppVersion.Moniker;

        try
        {
            SystemBackdrop = new Microsoft.UI.Xaml.Media.MicaBackdrop();
        }
        catch (Exception ex)
        {
            StartupLog.Write("MainWindow MicaBackdrop: " + ex);
        }

        try
        {
            var appWindow = WindowIcon.Apply(this);
            appWindow.Resize(new Windows.Graphics.SizeInt32(540, 760));
            if (appWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.IsAlwaysOnTop = true;
                presenter.IsMaximizable = false;
            }
        }
        catch (Exception ex)
        {
            StartupLog.Write("MainWindow AppWindow chrome: " + ex);
        }
    }

    public HudViewModel ViewModel { get; }

    public nint Handle => WindowNative.GetWindowHandle(this);

    internal FrameworkElement CaptureRoot => HudRoot;

    private void ProduceChecklist_Click(object sender, RoutedEventArgs e)
        => (Application.Current as App)?.ProduceChecklist();

    private void Settings_Click(object sender, RoutedEventArgs e)
        => (Application.Current as App)?.ShowSettings();
}