using Microsoft.UI.Xaml;

namespace Reentry.App;

public sealed partial class ConsentWindow : Window
{
    private readonly TaskCompletionSource<bool> _tcs = new();

    public ConsentWindow()
    {
        InitializeComponent();
        Title = "Reentry";
        SystemBackdrop = new Microsoft.UI.Xaml.Media.MicaBackdrop();
        Closed += (_, _) => _tcs.TrySetResult(false);
    }

    public Task<bool> ShowAsync()
    {
        Activate();
        return _tcs.Task;
    }

    private void Accept_Click(object sender, RoutedEventArgs e)
    {
        _tcs.TrySetResult(true);
        Close();
    }

    private void Decline_Click(object sender, RoutedEventArgs e)
    {
        _tcs.TrySetResult(false);
        Close();
    }
}
