using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Reentry.Core.Models;
using Windows.UI;

namespace Reentry.App.ViewModels;

public sealed partial class TrackedAppRow : ObservableObject
{
    private static readonly SolidColorBrush InteractiveBrush = Brush(16, 124, 16);
    private static readonly SolidColorBrush PendingBrush = Brush(201, 156, 0);
    private static readonly SolidColorBrush FailedBrush = Brush(155, 27, 90);
    private static readonly SolidColorBrush HungBrush = Brush(230, 126, 34);
    private static readonly SolidColorBrush DisabledBrush = Brush(107, 107, 107);
    private static readonly SolidColorBrush ChipOnDark = Brush(255, 255, 255);
    private static readonly SolidColorBrush ChipOnAmber = Brush(26, 18, 0);

    [ObservableProperty] private string _name = "";
    [ObservableProperty] private string _source = "";
    [ObservableProperty] private string _state = "";
    [ObservableProperty] private string _elapsed = "";
    [ObservableProperty] private bool _isManaged;
    [ObservableProperty] private string _stateGlyph = "●";
    [ObservableProperty] private Brush _stateBrush = DisabledBrush;
    [ObservableProperty] private Brush _chipForeground = ChipOnDark;

    public string Id { get; init; } = "";
    public Visibility ManagedVisibility => IsManaged ? Visibility.Visible : Visibility.Collapsed;

    public static TrackedAppRow From(TrackedApp app)
    {
        var row = new TrackedAppRow { Id = app.Id };
        row.Apply(app);
        return row;
    }

    public void Apply(TrackedApp app)
    {
        Name = app.Name;
        Source = app.Source.ToString();
        State = app.State.ToString();
        Elapsed = FormatElapsed(app.Elapsed);
        IsManaged = app.IsManaged;
        OnPropertyChanged(nameof(ManagedVisibility));
        StateGlyph = app.State switch
        {
            AppState.Interactive => "●",
            AppState.Starting => "◐",
            AppState.Pending => "○",
            AppState.Failed => "✖",
            AppState.Hung => "◐",
            AppState.Disabled => "–",
            _ => "●",
        };
        (StateBrush, ChipForeground) = app.State switch
        {
            AppState.Interactive => ((Brush)InteractiveBrush, ChipOnDark),
            AppState.Starting or AppState.Pending => (PendingBrush, ChipOnAmber),
            AppState.Failed => (FailedBrush, ChipOnDark),
            AppState.Hung => (HungBrush, ChipOnDark),
            _ => (DisabledBrush, ChipOnDark),
        };
    }

    private static SolidColorBrush Brush(byte r, byte g, byte b) =>
        new(Color.FromArgb(255, r, g, b));

    private static string FormatElapsed(TimeSpan elapsed)
    {
        if (elapsed.TotalHours >= 1)
            return $"{(int)elapsed.TotalHours:00}:{elapsed.Minutes:00}:{elapsed.Seconds:00}";
        return $"{elapsed.Minutes:00}:{elapsed.Seconds:00}";
    }
}
