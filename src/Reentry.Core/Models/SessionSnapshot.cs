namespace Reentry.Core.Models;

public sealed class SessionSnapshot
{
    public DateTimeOffset CapturedUtc { get; set; }
    public string? SessionId { get; set; }
    public List<SnapshotProcess> Processes { get; set; } = [];
    public List<SnapshotWindow> Windows { get; set; } = [];
    public List<StartupInventoryItem> Inventory { get; set; } = [];
}

public sealed class SnapshotProcess
{
    public int ProcessId { get; set; }
    public required string Executable { get; set; }
    public string? CommandLine { get; set; }
    public bool IsArrRegistered { get; set; }
    public DateTimeOffset StartedUtc { get; set; }
}

public sealed class SnapshotWindow
{
    public long Handle { get; set; }
    public int ProcessId { get; set; }
    public required string Title { get; set; }
    public required string Executable { get; set; }
    public bool IsVisible { get; set; }
}
