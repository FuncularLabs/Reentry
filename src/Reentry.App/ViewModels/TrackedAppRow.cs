using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Reentry.Core.Models;

namespace Reentry.App.ViewModels;

public sealed partial class TrackedAppRow : ObservableObject
{
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private string _source = "";
    [ObservableProperty] private string _state = "";
    [ObservableProperty] private string _elapsed = "";
    [ObservableProperty] private bool _isManaged;
    [ObservableProperty] private string _stateGlyph = "●";

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
    }

    private static string FormatElapsed(TimeSpan elapsed)
    {
        if (elapsed.TotalHours >= 1)
            return $"{(int)elapsed.TotalHours:00}:{elapsed.Minutes:00}:{elapsed.Seconds:00}";
        return $"{elapsed.Minutes:00}:{elapsed.Seconds:00}";
    }
}
