using Reentry.Core.Models;

namespace Reentry.Core.Timing;

public static class TimingSections
{
    public const string Restore = "restore";
    public const string Startup = "startup";

    public static string For(TrackedApp app)
        => app.Source.IsRestoreSource() ? Restore : Startup;
}

public readonly record struct TimingTriple(TimeSpan? Last, TimeSpan Current, TimeSpan? Average);

public sealed class SessionTimingView
{
    public required TimingTriple Restore { get; init; }
    public required TimingTriple Startup { get; init; }
    public required IReadOnlyDictionary<string, TimingTriple> Apps { get; init; }

    public TimingTriple ForApp(string appId, TimeSpan currentElapsed)
        => Apps.TryGetValue(appId, out var triple)
            ? triple
            : new TimingTriple(Last: null, Current: currentElapsed, Average: null);
}
