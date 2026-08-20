namespace Reentry.Core.Models;

public sealed class EventLogRecord
{
    public required string LogName { get; init; }
    public required string Provider { get; init; }
    public int EventId { get; init; }
    public DateTimeOffset TimeCreated { get; init; }
    public string? Message { get; init; }
}
