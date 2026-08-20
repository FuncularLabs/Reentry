namespace Reentry.Core.Abstractions;

public sealed record LiveProcess(
    int ProcessId,
    string Executable,
    string? CommandLine,
    DateTimeOffset StartedUtc);

public sealed record LiveWindow(
    long Handle,
    int ProcessId,
    string Title,
    string Executable,
    bool IsVisible);

public interface IProcessProbe
{
    IReadOnlyList<LiveProcess> GetProcesses();
    IReadOnlyList<LiveWindow> GetVisibleWindows();

    /// <summary>
    /// Windows has no public "list every ARR registrant" API. App reports what it knows
    /// (Reentry itself after RegisterApplicationRestart, plus any future probe).
    /// </summary>
    bool IsArrRegistered(int processId);
}
