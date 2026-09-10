using Reentry.Core.Models;

namespace Reentry.Core.Timing;

/// <summary>
/// Live last / now / avg for the HUD. Current elapsed runs until the row or
/// section settles, then freezes. History is loaded once and excludes this session.
/// </summary>
public sealed class SessionTimingRecorder
{
    private readonly TimingStore _store;
    private readonly long _sessionId;
    private readonly DateTimeOffset _startedUtc;
    private readonly TimingHistory _history;
    private readonly object _gate = new();
    private readonly HashSet<string> _recordedApps = new(StringComparer.Ordinal);
    private readonly HashSet<string> _recordedSections = new(StringComparer.Ordinal);
    private readonly Dictionary<string, TimeSpan> _frozenSections = new(StringComparer.Ordinal);
    private IReadOnlyList<TrackedApp> _lastApps = [];

    public SessionTimingRecorder(TimingStore store, long sessionId, DateTimeOffset startedUtc)
    {
        _store = store;
        _sessionId = sessionId;
        _startedUtc = startedUtc;
        _history = store.LoadHistory(sessionId);
    }

    public long SessionId => _sessionId;
    public DateTimeOffset StartedUtc => _startedUtc;

    public SessionTimingView Observe(IReadOnlyList<TrackedApp> apps, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(apps);
        lock (_gate)
        {
            _lastApps = apps;

            foreach (var app in apps)
            {
                if (!app.State.IsSettled() || _recordedApps.Contains(app.Id))
                    continue;

                var settledUtc = _startedUtc + app.Elapsed;
                _store.RecordAppSettle(_sessionId, app.Id, app.Elapsed, app.State, settledUtc);
                _recordedApps.Add(app.Id);
            }

            var restore = apps.Where(a => a.Source.IsRestoreSource()).ToList();
            var startup = apps.Where(a => !a.Source.IsRestoreSource()).ToList();

            var restoreTriple = TripleForSection(TimingSections.Restore, restore, now);
            var startupTriple = TripleForSection(TimingSections.Startup, startup, now);

            var appTriples = new Dictionary<string, TimingTriple>(apps.Count, StringComparer.Ordinal);
            foreach (var app in apps)
            {
                appTriples[app.Id] = new TimingTriple(
                    Last: _history.LastApp(app.Id),
                    Current: app.Elapsed,
                    Average: _history.AverageApp(app.Id));
            }

            return new SessionTimingView
            {
                Restore = restoreTriple,
                Startup = startupTriple,
                Apps = appTriples,
            };
        }
    }

    /// <summary>
    /// Re-persist any settles already observed. ENDSESSION / timer hook so a crash
    /// still leaves the rows that had reached a terminal state.
    /// </summary>
    public SessionTimingView Flush(DateTimeOffset now)
        => Observe(_lastApps, now);

    private TimingTriple TripleForSection(string section, List<TrackedApp> items, DateTimeOffset now)
    {
        var current = CurrentForSection(section, items, now);
        if (items.Count > 0
            && items.TrueForAll(a => a.State.IsSettled())
            && !_recordedSections.Contains(section))
        {
            _store.RecordSectionSettle(_sessionId, section, current, now);
            _recordedSections.Add(section);
        }

        return new TimingTriple(
            Last: _history.LastSection(section),
            Current: current,
            Average: _history.AverageSection(section));
    }

    private TimeSpan CurrentForSection(string section, List<TrackedApp> items, DateTimeOffset now)
    {
        if (items.Count == 0)
            return TimeSpan.Zero;

        if (items.TrueForAll(a => a.State.IsSettled()))
        {
            if (!_frozenSections.TryGetValue(section, out var frozen))
            {
                frozen = Clamp(now - _startedUtc);
                _frozenSections[section] = frozen;
            }

            return frozen;
        }

        return Clamp(now - _startedUtc);
    }

    private static TimeSpan Clamp(TimeSpan value)
        => value < TimeSpan.Zero ? TimeSpan.Zero : value;
}
