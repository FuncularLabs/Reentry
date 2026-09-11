namespace Reentry.Core.Models;

public sealed record ChecklistDocument
{
    public required DateTimeOffset ExportedAt { get; init; }
    public required string SessionType { get; init; }
    public string? SystemLogNote { get; init; }
    public required int SettledCount { get; init; }
    public required int TotalCount { get; init; }
    public required string SessionElapsed { get; init; }
    public required IReadOnlyList<ChecklistRow> RestoreRows { get; init; }
    public required IReadOnlyList<ChecklistRow> StartupRows { get; init; }
    public required string ReentryVersion { get; init; }
    public required string OsBuild { get; init; }
    public string? MachineName { get; init; }
    public string? ImageFileName { get; init; }
}

public sealed record ChecklistRow
{
    public required string Name { get; init; }
    public required string Source { get; init; }
    public required AppState State { get; init; }
}
