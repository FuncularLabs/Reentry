using Reentry.Core.Abstractions;
using Reentry.Core.Models;

namespace Reentry.Core.Snapshot;

/// <summary>
/// Probe-backed snapshotter. Filters to inventory matches, visible windows, and ARR pids.
/// Does not parse Outlook/Chrome/Explorer private session files — we only record what we see.
/// </summary>
public sealed class SessionSnapshotter : ISessionSnapshotter
{
    private readonly IProcessProbe _probe;
    private readonly Func<IReadOnlyList<StartupInventoryItem>> _inventory;
    private readonly Func<DateTimeOffset> _utcNow;

    public SessionSnapshotter(
        IProcessProbe probe,
        Func<IReadOnlyList<StartupInventoryItem>> inventory,
        Func<DateTimeOffset>? utcNow = null)
    {
        _probe = probe;
        _inventory = inventory;
        _utcNow = utcNow ?? (() => DateTimeOffset.UtcNow);
    }

    public SessionSnapshot Capture()
    {
        var now = _utcNow();
        var inventory = _inventory().ToList();
        var processes = _probe.GetProcesses();
        var windows = _probe.GetVisibleWindows();

        var interesting = new Dictionary<int, SnapshotProcess>();

        foreach (var process in processes)
        {
            var inInventory = inventory.Any(i => CommandText.SameExecutable(i.Command, process.Executable));
            var arr = _probe.IsArrRegistered(process.ProcessId);
            if (!inInventory && !arr)
                continue;

            interesting[process.ProcessId] = ToSnapshot(process, arr);
        }

        var snapshotWindows = new List<SnapshotWindow>();
        foreach (var window in windows.Where(w => w.IsVisible))
        {
            snapshotWindows.Add(new SnapshotWindow
            {
                Handle = window.Handle,
                ProcessId = window.ProcessId,
                Title = window.Title,
                Executable = window.Executable,
                IsVisible = true,
            });

            if (!interesting.ContainsKey(window.ProcessId))
            {
                var live = processes.FirstOrDefault(p => p.ProcessId == window.ProcessId);
                interesting[window.ProcessId] = live is null
                    ? new SnapshotProcess
                    {
                        ProcessId = window.ProcessId,
                        Executable = window.Executable,
                        IsArrRegistered = _probe.IsArrRegistered(window.ProcessId),
                        StartedUtc = now,
                    }
                    : ToSnapshot(live, _probe.IsArrRegistered(live.ProcessId));
            }
        }

        return new SessionSnapshot
        {
            CapturedUtc = now,
            SessionId = now.ToUnixTimeSeconds().ToString(),
            Processes = interesting.Values.ToList(),
            Windows = snapshotWindows,
            Inventory = inventory,
        };
    }

    private static SnapshotProcess ToSnapshot(LiveProcess process, bool arr) => new()
    {
        ProcessId = process.ProcessId,
        Executable = process.Executable,
        CommandLine = process.CommandLine,
        IsArrRegistered = arr,
        StartedUtc = process.StartedUtc,
    };
}
