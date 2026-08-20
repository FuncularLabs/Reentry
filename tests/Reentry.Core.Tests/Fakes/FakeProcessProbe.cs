using Reentry.Core.Abstractions;

namespace Reentry.Core.Tests.Fakes;

public sealed class FakeProcessProbe : IProcessProbe
{
    public List<LiveProcess> Processes { get; } = [];
    public List<LiveWindow> Windows { get; } = [];
    public HashSet<int> ArrPids { get; } = [];

    public IReadOnlyList<LiveProcess> GetProcesses() => Processes;
    public IReadOnlyList<LiveWindow> GetVisibleWindows() => Windows;
    public bool IsArrRegistered(int processId) => ArrPids.Contains(processId);

    public void AddProcess(int pid, string exe, string? commandLine = null, DateTimeOffset? started = null)
        => Processes.Add(new LiveProcess(pid, exe, commandLine, started ?? DateTimeOffset.UtcNow));

    public void AddWindow(int pid, string exe, string title, long handle = 1)
        => Windows.Add(new LiveWindow(handle, pid, title, exe, IsVisible: true));

    public void ClearLive()
    {
        Processes.Clear();
        Windows.Clear();
    }
}
