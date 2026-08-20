using System.Diagnostics.Eventing.Reader;
using Reentry.Core.Abstractions;
using Reentry.Core.Models;

namespace Reentry.App.Services;

public sealed class Win32EventLogReader : IEventLogReader
{
    public IReadOnlyList<EventLogRecord> ReadRecent(string logName, TimeSpan lookback, DateTimeOffset now)
    {
        var since = now - lookback;
        var results = new List<EventLogRecord>();
        var query = new EventLogQuery(logName, PathType.LogName)
        {
            ReverseDirection = true,
        };

        try
        {
            using var reader = new EventLogReader(query);
            for (var i = 0; i < 400; i++)
            {
                using var ev = reader.ReadEvent();
                if (ev is null)
                    break;
                if (ev.TimeCreated is null)
                    continue;
                var created = new DateTimeOffset(DateTime.SpecifyKind(ev.TimeCreated.Value, DateTimeKind.Utc));
                if (created < since)
                    break;

                results.Add(new EventLogRecord
                {
                    LogName = logName,
                    Provider = ev.ProviderName ?? "",
                    EventId = ev.Id,
                    TimeCreated = created,
                    Message = SafeFormat(ev),
                });
            }
        }
        catch (EventLogNotFoundException)
        {
            return [];
        }
        catch (UnauthorizedAccessException)
        {
            return [];
        }

        return results;
    }

    private static string? SafeFormat(EventRecord ev)
    {
        try { return ev.FormatDescription(); }
        catch { return null; }
    }
}
