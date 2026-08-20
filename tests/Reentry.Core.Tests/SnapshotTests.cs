using Reentry.Core.Models;
using Reentry.Core.Snapshot;
using Reentry.Core.Tests.Fakes;
using Reentry.Core.Tests.Support;

namespace Reentry.Core.Tests;

public class SnapshotTests
{
    [Fact]
    public void Store_RoundTripsLastSession()
    {
        using var dir = new TempDir();
        var store = new SessionSnapshotStore(dir.Path);

        var original = new SessionSnapshot
        {
            CapturedUtc = new DateTimeOffset(2026, 8, 20, 12, 0, 0, TimeSpan.Zero),
            SessionId = "test-session",
            Processes =
            [
                new SnapshotProcess
                {
                    ProcessId = 4242,
                    Executable = "C:\\Program Files\\Slack\\slack.exe",
                    CommandLine = "slack.exe",
                    IsArrRegistered = true,
                    StartedUtc = new DateTimeOffset(2026, 8, 20, 11, 59, 0, TimeSpan.Zero),
                },
            ],
            Windows =
            [
                new SnapshotWindow
                {
                    Handle = 99,
                    ProcessId = 4242,
                    Title = "Slack",
                    Executable = "C:\\Program Files\\Slack\\slack.exe",
                    IsVisible = true,
                },
            ],
            Inventory =
            [
                new StartupInventoryItem
                {
                    Name = "Slack",
                    Command = "C:\\Program Files\\Slack\\slack.exe",
                    Source = AppSource.Run,
                    ValueName = "Slack",
                    IsUserScope = true,
                },
            ],
        };

        store.Write(original);
        Assert.True(File.Exists(Path.Combine(dir.Path, "last-session.json")));

        var loaded = store.Read();
        Assert.NotNull(loaded);
        Assert.Equal("test-session", loaded!.SessionId);
        Assert.Equal(original.CapturedUtc, loaded.CapturedUtc);
        var process = Assert.Single(loaded.Processes);
        Assert.True(process.IsArrRegistered);
        Assert.Equal(4242, process.ProcessId);
        var window = Assert.Single(loaded.Windows);
        Assert.Equal("Slack", window.Title);
        var item = Assert.Single(loaded.Inventory);
        Assert.Equal(AppSource.Run, item.Source);
    }

    [Fact]
    public void Snapshotter_RecordsInventoryMatches_Windows_AndArr()
    {
        var probe = new FakeProcessProbe();
        probe.AddProcess(10, "C:\\Windows\\explorer.exe");
        probe.AddProcess(20, "C:\\Apps\\outlook.exe");
        probe.AddProcess(30, "C:\\Apps\\reentry.exe");
        probe.AddProcess(99, "C:\\Windows\\System32\\svchost.exe");
        probe.ArrPids.Add(30);
        probe.AddWindow(20, "C:\\Apps\\outlook.exe", "Inbox");

        var inventory = new List<StartupInventoryItem>
        {
            new()
            {
                Name = "Outlook",
                Command = "\"C:\\Apps\\outlook.exe\"",
                Source = AppSource.Run,
                IsUserScope = true,
            },
        };

        var now = new DateTimeOffset(2026, 8, 20, 17, 0, 0, TimeSpan.Zero);
        var snapshotter = new SessionSnapshotter(probe, () => inventory, () => now);
        var snap = snapshotter.Capture();

        Assert.Equal(now, snap.CapturedUtc);
        Assert.DoesNotContain(snap.Processes, p => p.Executable.Contains("svchost", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(snap.Processes, p => p.ProcessId == 20);
        Assert.Contains(snap.Processes, p => p.ProcessId == 30 && p.IsArrRegistered);
        Assert.DoesNotContain(snap.Processes, p => p.ProcessId == 10);
        var window = Assert.Single(snap.Windows);
        Assert.Equal("Inbox", window.Title);
        Assert.Single(snap.Inventory);
    }

    [Fact]
    public void Snapshotter_IncludesExplorerWhenWindowVisible()
    {
        var probe = new FakeProcessProbe();
        probe.AddProcess(10, "C:\\Windows\\explorer.exe");
        probe.AddWindow(10, "C:\\Windows\\explorer.exe", "Documents");

        var snapshotter = new SessionSnapshotter(probe, () => [], () => DateTimeOffset.UtcNow);
        var snap = snapshotter.Capture();
        Assert.Contains(snap.Processes, p => p.ProcessId == 10);
        Assert.Single(snap.Windows);
    }
}
