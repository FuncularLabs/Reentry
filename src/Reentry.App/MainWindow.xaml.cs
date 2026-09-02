using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
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
        appWindow.Resize(new Windows.Graphics.SizeInt32(560, 760));
        if (appWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsAlwaysOnTop = true;
            presenter.IsMaximizable = false;
        }
    }

    public HudViewModel ViewModel { get; }

    public nint Handle => WindowNative.GetWindowHandle(this);

    private void HudList_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is DependencyObject root)
            ForceVisibleScrollbars(root);
    }

    private static void ForceVisibleScrollbars(DependencyObject root)
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is ScrollViewer viewer)
            {
                viewer.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
                viewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
            }

            if (child is ScrollBar bar)
                bar.IndicatorMode = ScrollingIndicatorMode.MouseIndicator;

            ForceVisibleScrollbars(child);
        }
    }
}
