using Reentry.Core.Models;
using Reentry.Core.Settings;
using Reentry.Core.Tests.Fakes;
using Reentry.Core.Tracking;

namespace Reentry.Core.Tests;

public class TrackerTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 20, 8, 0, 0, TimeSpan.Zero);

    private static StartupInventoryItem Steam() => new()
    {
        Name = "Steam",
        Command = "C:\\Program Files\\Steam\\steam.exe -silent",
        Source = AppSource.Run,
        ValueName = "Steam",
        IsUserScope = true,
        IsEnabled = true,
    };

    [Fact]
    public void Tick_ExpectedAppWithNoProcess_IsPending()
    {
        var tracker = new StartupTracker();
        var rows = tracker.Tick(T0, new FakeProcessProbe(), [Steam()], lastSession: null);
        var steam = Assert.Single(rows);
        Assert.Equal(AppState.Pending, steam.State);
        Assert.Equal(TimeSpan.Zero, steam.Elapsed);
    }

    [Fact]
    public void Tick_ProcessAliveNoWindow_IsStarting()
    {
        var probe = new FakeProcessProbe();
        probe.AddProcess(44, "C:\\Program Files\\Steam\\steam.exe");
        var tracker = new StartupTracker();
        var rows = tracker.Tick(T0.AddSeconds(5), probe, [Steam()], lastSession: null);
        Assert.Equal(AppState.Starting, Assert.Single(rows).State);
    }

    [Fact]
    public void Tick_VisibleWindow_IsInteractive_AndLatches()
    {
        var probe = new FakeProcessProbe();
        probe.AddProcess(44, "steam.exe");
        probe.AddWindow(44, "steam.exe", "Steam");
        var tracker = new StartupTracker();
        Assert.Equal(AppState.Interactive, Assert.Single(tracker.Tick(T0.AddSeconds(8), probe, [Steam()], null)).State);

        probe.ClearLive();
        Assert.Equal(AppState.Interactive, Assert.Single(tracker.Tick(T0.AddSeconds(20), probe, [Steam()], null)).State);
    }

    [Fact]
    public void Tick_ExitedQuicklyWithNoWindow_IsFailed()
    {
        var probe = new FakeProcessProbe();
        probe.AddProcess(44, "steam.exe");
        var tracker = new StartupTracker();
        Assert.Equal(AppState.Starting, Assert.Single(tracker.Tick(T0.AddSeconds(2), probe, [Steam()], null)).State);

        probe.ClearLive();
        Assert.Equal(AppState.Failed, Assert.Single(tracker.Tick(T0.AddSeconds(6), probe, [Steam()], null)).State);
    }

    [Fact]
    public void Tick_AliveNoWindowPastHungTimeout_IsHung()
    {
        var probe = new FakeProcessProbe();
        probe.AddProcess(44, "steam.exe");
        var settings = new ReentrySettings { HungNoWindowSeconds = 90, GlobalCapMinutes = 10 };
        var tracker = new StartupTracker(settings);
        tracker.Tick(T0, probe, [Steam()], null);
        var hung = Assert.Single(tracker.Tick(T0.AddSeconds(91), probe, [Steam()], null));
        Assert.Equal(AppState.Hung, hung.State);
        Assert.True(hung.Elapsed >= TimeSpan.FromSeconds(90));
    }

    [Fact]
    public void Tick_DisabledInventory_IsDisabled()
    {
        var item = Steam();
        item.IsEnabled = false;
        var probe = new FakeProcessProbe();
        probe.AddProcess(44, "steam.exe");
        var rows = new StartupTracker().Tick(T0, probe, [item], null);
        Assert.Equal(AppState.Disabled, Assert.Single(rows).State);
    }

    [Fact]
    public void Tick_NeverAppearedPastGlobalCap_IsFailed()
    {
        var settings = new ReentrySettings { GlobalCapMinutes = 10 };
        var tracker = new StartupTracker(settings);
        tracker.Tick(T0, new FakeProcessProbe(), [Steam()], null);
        var late = Assert.Single(tracker.Tick(T0.AddMinutes(10), new FakeProcessProbe(), [Steam()], null));
        Assert.Equal(AppState.Failed, late.State);
    }

    [Fact]
    public void Tick_AlivePastGlobalCapNoWindow_IsHung()
    {
        var probe = new FakeProcessProbe();
        probe.AddProcess(44, "steam.exe");
        var settings = new ReentrySettings { HungNoWindowSeconds = 90, GlobalCapMinutes = 10 };
        var tracker = new StartupTracker(settings);
        tracker.Tick(T0, probe, [Steam()], null);
        Assert.Equal(AppState.Hung, Assert.Single(tracker.Tick(T0.AddMinutes(10), probe, [Steam()], null)).State);
    }

    [Fact]
    public void Tick_ArrSnapshotProcess_BecomesExpectedRow()
    {
        var last = new SessionSnapshot
        {
            Processes =
            [
                new SnapshotProcess
                {
                    ProcessId = 7,
                    Executable = "C:\\Program Files\\Microsoft Office\\OUTLOOK.EXE",
                    IsArrRegistered = true,
                },
            ],
        };

        var rows = new StartupTracker().Tick(T0, new FakeProcessProbe(), [], last);
        var outlook = Assert.Single(rows);
        Assert.Equal(AppSource.Arr, outlook.Source);
        Assert.Equal(AppState.Pending, outlook.State);
        Assert.Equal("OUTLOOK", outlook.Name, ignoreCase: true);
    }

    [Fact]
    public void Tick_ExplorerWindowFromSnapshot_IsIncludedWhenNotInInventory()
    {
        var last = new SessionSnapshot
        {
            Windows =
            [
                new SnapshotWindow
                {
                    Handle = 1,
                    ProcessId = 8,
                    Title = "Documents",
                    Executable = "C:\\Windows\\explorer.exe",
                    IsVisible = true,
                },
            ],
        };

        var rows = new StartupTracker().Tick(T0, new FakeProcessProbe(), [], last);
        var explorer = Assert.Single(rows);
        Assert.Equal(AppSource.Explorer, explorer.Source);
        Assert.Equal("Documents", explorer.Name);
    }

    [Fact]
    public void Tracker_HasNoKillApi()
    {
        Assert.Null(typeof(StartupTracker).GetMethod("Kill"));
        Assert.Null(typeof(StartupTracker).GetMethod("Terminate"));
    }
}
