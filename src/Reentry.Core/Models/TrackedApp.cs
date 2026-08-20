namespace Reentry.Core.Models;

public sealed class TrackedApp
{
    public required string Id { get; init; }
    public required string Name { get; set; }
    public required string Executable { get; set; }
    public string? CommandLine { get; set; }
    public AppSource Source { get; set; }
    public AppState State { get; set; } = AppState.Pending;
    public TimeSpan Elapsed { get; set; }
    public bool IsManaged { get; set; }
    public bool IsEnabled { get; set; } = true;
    public string? Location { get; set; }
    public string? ValueName { get; set; }
    public bool IsUserScope { get; set; } = true;
    public DateTimeOffset FirstExpectedUtc { get; set; }
}
