using Reentry.Core.Abstractions;
using Reentry.Core.Models;

namespace Reentry.Core.Tests.Fakes;

public sealed class FakeEventLogReader : IEventLogReader
{
    public List<EventLogRecord> Events { get; } = [];

    public IReadOnlyList<EventLogRecord> ReadRecent(string logName, TimeSpan lookback, DateTimeOffset now)
        => Events
            .Where(e => e.LogName.Equals(logName, StringComparison.OrdinalIgnoreCase))
            .Where(e => e.TimeCreated >= now - lookback && e.TimeCreated <= now)
            .ToList();
}
