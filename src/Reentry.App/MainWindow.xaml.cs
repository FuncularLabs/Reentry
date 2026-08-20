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
        Title = "Reentry";
        SystemBackdrop = new Microsoft.UI.Xaml.Media.MicaBackdrop();

        var appWindow = WindowIcon.Apply(this);
        appWindow.Resize(new Windows.Graphics.SizeInt32(540, 760));
        if (appWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsAlwaysOnTop = true;
            presenter.IsMaximizable = false;
        }
    }

    public HudViewModel ViewModel { get; }

    public nint Handle => WindowNative.GetWindowHandle(this);
}
