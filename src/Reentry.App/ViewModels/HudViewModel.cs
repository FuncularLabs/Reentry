using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Reentry.Core;
using Reentry.Core.Models;

namespace Reentry.App.ViewModels;

public sealed partial class HudViewModel : ObservableObject
{
    [ObservableProperty] private string _bootBanner = "";
    [ObservableProperty] private string _bootDetail = "";
    [ObservableProperty] private string _footerElapsed = "00:00";
    [ObservableProperty] private DateTimeOffset _sessionStartedUtc = DateTimeOffset.UtcNow;
    [ObservableProperty] private int _settledCount;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private double _settledFraction;
    [ObservableProperty] private string _settledSummary = "0 / 0 settled";

    public HudViewModel(BootKind bootKind)
    {
        (BootBanner, BootDetail) = bootKind switch
        {
            BootKind.Expected => ("Expected restart", "Windows recorded a planned shutdown (User32 1074)."),
            BootKind.Unexpected => ("Unexpected shutdown", "The last session did not end cleanly (6008 / Kernel-Power 41)."),
            _ => ("Ordinary logon", "No recent planned or dirty shutdown in the System log."),
        };
    }

    public ObservableCollection<TrackedAppRow> RestoreRows { get; } = [];
    public ObservableCollection<TrackedAppRow> StartupRows { get; } = [];

    public void ReplaceRows(IReadOnlyList<TrackedApp> apps)
    {
        var restore = apps.Where(a => a.Source is AppSource.Arr or AppSource.Explorer).ToList();
        var startup = apps.Where(a => a.Source is not AppSource.Arr and not AppSource.Explorer).ToList();
        Sync(RestoreRows, restore);
        Sync(StartupRows, startup);

        TotalCount = apps.Count;
        SettledCount = apps.Count(a => a.State.IsSettled());
        SettledFraction = TotalCount == 0 ? 0 : (double)SettledCount / TotalCount;
        SettledSummary = $"{SettledCount} / {TotalCount} settled";
        FooterElapsed = Format(DateTimeOffset.UtcNow - SessionStartedUtc);
    }

    private static void Sync(ObservableCollection<TrackedAppRow> target, List<TrackedApp> source)
    {
        CollectionSync.InPlace(
            target,
            source,
            itemKey: r => r.Id,
            sourceKey: a => a.Id,
            apply: (row, app) => row.Apply(app),
            create: TrackedAppRow.From);
    }

    private static string Format(TimeSpan elapsed)
    {
        if (elapsed.TotalHours >= 1)
            return $"{(int)elapsed.TotalHours:00}:{elapsed.Minutes:00}:{elapsed.Seconds:00}";
        return $"{elapsed.Minutes:00}:{elapsed.Seconds:00}";
    }
}
