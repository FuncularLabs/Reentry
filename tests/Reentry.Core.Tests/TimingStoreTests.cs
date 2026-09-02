using Reentry.Core.Models;
using Reentry.Core.Tests.Support;
using Reentry.Core.Timing;

namespace Reentry.Core.Tests;

public class TimingStoreTests
{
    private static readonly DateTimeOffset T0 = new(2026, 9, 2, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void LastAndAverage_ExcludeInProgressSession()
    {
        using var dir = new TempDir();
        using var store = new TimingStore(dir.Path);

        var boot1 = store.BeginSession(T0);
        store.RecordSectionSettle(boot1, TimingSections.Startup, TimeSpan.FromSeconds(10), T0.AddSeconds(10));
        store.RecordAppSettle(boot1, "dropbox", TimeSpan.FromSeconds(8), AppState.Interactive, T0.AddSeconds(8));

        var boot2 = store.BeginSession(T0.AddHours(1));
        store.RecordSectionSettle(boot2, TimingSections.Startup, TimeSpan.FromSeconds(20), T0.AddHours(1).AddSeconds(20));
        store.RecordAppSettle(boot2, "dropbox", TimeSpan.FromSeconds(16), AppState.Interactive, T0.AddHours(1).AddSeconds(16));

        var boot3 = store.BeginSession(T0.AddHours(2));
        store.RecordSectionSettle(boot3, TimingSections.Startup, TimeSpan.FromSeconds(99), T0.AddHours(2).AddSeconds(99));
        store.RecordAppSettle(boot3, "dropbox", TimeSpan.FromSeconds(99), AppState.Interactive, T0.AddHours(2).AddSeconds(99));

        var history = store.LoadHistory(excludeSessionId: boot3);
        Assert.Equal(TimeSpan.FromSeconds(20), history.LastSection(TimingSections.Startup));
        Assert.Equal(TimeSpan.FromSeconds(15), history.AverageSection(TimingSections.Startup));
        Assert.Equal(TimeSpan.FromSeconds(16), history.LastApp("dropbox"));
        Assert.Equal(TimeSpan.FromSeconds(12), history.AverageApp("dropbox"));
        Assert.Null(history.LastSection(TimingSections.Restore));
        Assert.Null(history.LastApp("missing"));
    }

    [Fact]
    public void RecordSettle_FirstWriteWins()
    {
        using var dir = new TempDir();
        using var store = new TimingStore(dir.Path);
        var boot = store.BeginSession(T0);
        store.RecordAppSettle(boot, "dropbox", TimeSpan.FromSeconds(8), AppState.Interactive, T0.AddSeconds(8));
        store.RecordAppSettle(boot, "dropbox", TimeSpan.FromSeconds(99), AppState.Failed, T0.AddSeconds(99));
        store.RecordSectionSettle(boot, TimingSections.Startup, TimeSpan.FromSeconds(10), T0.AddSeconds(10));
        store.RecordSectionSettle(boot, TimingSections.Startup, TimeSpan.FromSeconds(99), T0.AddSeconds(99));

        var later = store.BeginSession(T0.AddHours(1));
        var history = store.LoadHistory(later);
        Assert.Equal(TimeSpan.FromSeconds(8), history.LastApp("dropbox"));
        Assert.Equal(TimeSpan.FromSeconds(10), history.LastSection(TimingSections.Startup));
    }

    [Fact]
    public void History_SurvivesReopen()
    {
        using var dir = new TempDir();
        long boot1;
        using (var store = new TimingStore(dir.Path))
        {
            boot1 = store.BeginSession(T0);
            store.RecordSectionSettle(boot1, TimingSections.Restore, TimeSpan.FromSeconds(12), T0.AddSeconds(12));
        }

        using var reopened = new TimingStore(dir.Path);
        var boot2 = reopened.BeginSession(T0.AddHours(1));
        var history = reopened.LoadHistory(boot2);
        Assert.Equal(TimeSpan.FromSeconds(12), history.LastSection(TimingSections.Restore));
        Assert.Equal(TimeSpan.FromSeconds(12), history.AverageSection(TimingSections.Restore));
        Assert.True(File.Exists(Path.Combine(dir.Path, "timings.sqlite")));
    }

    [Fact]
    public void NoHistory_ReturnsNullLastAndAverage()
    {
        using var dir = new TempDir();
        using var store = new TimingStore(dir.Path);
        var boot = store.BeginSession(T0);
        var history = store.LoadHistory(boot);
        Assert.Null(history.LastSection(TimingSections.Startup));
        Assert.Null(history.AverageSection(TimingSections.Startup));
        Assert.Null(history.LastApp("anything"));
        Assert.Null(history.AverageApp("anything"));
    }
}
