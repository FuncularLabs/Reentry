using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Reentry.Core;
using Reentry.Core.Models;
using Reentry.Core.Timing;

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
    [ObservableProperty] private string _restoreTiming = "now 00:00";
    [ObservableProperty] private string _startupTiming = "now 00:00";

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

    public void ReplaceRows(IReadOnlyList<TrackedApp> apps, SessionTimingView? timings = null)
    {
        var restore = apps.Where(a => a.Source.IsRestoreSource()).ToList();
        var startup = apps.Where(a => !a.Source.IsRestoreSource()).ToList();
        Sync(RestoreRows, restore, timings);
        Sync(StartupRows, startup, timings);

        TotalCount = apps.Count;
        SettledCount = apps.Count(a => a.State.IsSettled());
        SettledFraction = TotalCount == 0 ? 0 : (double)SettledCount / TotalCount;
        SettledSummary = $"{SettledCount} / {TotalCount} settled";
        FooterElapsed = DurationFormat.Clock(DateTimeOffset.UtcNow - SessionStartedUtc);
        RestoreTiming = DurationFormat.Section(timings?.Restore ?? SectionFallback(restore));
        StartupTiming = DurationFormat.Section(timings?.Startup ?? SectionFallback(startup));
    }

    private static void Sync(
        ObservableCollection<TrackedAppRow> target,
        List<TrackedApp> source,
        SessionTimingView? timings)
    {
        CollectionSync.InPlace(
            target,
            source,
            itemKey: r => r.Id,
            sourceKey: a => a.Id,
            apply: (row, app) => row.Apply(app, timings?.ForApp(app.Id, app.Elapsed)),
            create: app => TrackedAppRow.From(app, timings?.ForApp(app.Id, app.Elapsed)));
    }

    private static TimingTriple SectionFallback(List<TrackedApp> apps)
    {
        if (apps.Count == 0)
            return new TimingTriple(null, TimeSpan.Zero, null);
        return new TimingTriple(null, apps.Max(a => a.Elapsed), null);
    }
}
