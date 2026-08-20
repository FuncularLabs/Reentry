using Reentry.Core.Abstractions;
using Reentry.Core.Models;

namespace Reentry.Core.Boot;

public sealed class BootClassifier
{
    public static readonly TimeSpan DefaultLookback = TimeSpan.FromHours(48);

    public const int User32ExpectedShutdown = 1074;
    public const int UnexpectedShutdown = 6008;
    public const int KernelPowerDirtyShutdown = 41;

    public BootKind Classify(IEventLogReader reader, DateTimeOffset now, TimeSpan? lookback = null)
    {
        var window = lookback ?? DefaultLookback;
        var events = reader.ReadRecent("System", window, now);

        EventLogRecord? latest = null;
        foreach (var record in events)
        {
            if (!IsRelevant(record))
                continue;
            if (latest is null || record.TimeCreated > latest.TimeCreated)
                latest = record;
        }

        if (latest is null)
            return BootKind.Ordinary;

        return IsUnexpected(latest) ? BootKind.Unexpected : BootKind.Expected;
    }

    public static bool IsRelevant(EventLogRecord record)
    {
        if (record.EventId == User32ExpectedShutdown)
            return true;
        if (record.EventId == UnexpectedShutdown)
            return true;
        if (record.EventId == KernelPowerDirtyShutdown && IsKernelPower(record.Provider))
            return true;
        return false;
    }

    public static bool IsUnexpected(EventLogRecord record)
        => record.EventId == UnexpectedShutdown
           || (record.EventId == KernelPowerDirtyShutdown && IsKernelPower(record.Provider));

    private static bool IsKernelPower(string provider)
        => provider.Contains("Kernel-Power", StringComparison.OrdinalIgnoreCase)
           || provider.Equals("Microsoft-Windows-Kernel-Power", StringComparison.OrdinalIgnoreCase);
}
