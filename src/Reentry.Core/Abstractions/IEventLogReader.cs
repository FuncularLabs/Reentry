using Reentry.Core.Models;

namespace Reentry.Core.Abstractions;

public interface IEventLogReader
{
    IReadOnlyList<EventLogRecord> ReadRecent(string logName, TimeSpan lookback, DateTimeOffset now);
}
