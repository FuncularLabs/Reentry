using Reentry.Core.Abstractions;
using Reentry.Core.Managed;
using Reentry.Core.Models;
using Reentry.Core.Settings;

namespace Reentry.Core.Tracking;

/// <summary>
/// Match last-session + inventory to live processes/windows.
/// Failed = exited with no window (or never appeared past the global cap).
/// Hung = restore (ARR / Explorer) still alive with no window past timeout.
/// Startup inventory that stays alive without a window is Interactive (tray / background).
/// Never kills anything. Global cap (~10 min) marks leftover Pending as Failed
/// and leftover restore Starting as Hung.
/// </summary>
public sealed class StartupTracker
{
    private readonly ReentrySettings _settings;
    private readonly Dictionary<string, Observation> _observations = new(StringComparer.Ordinal);
    private DateTimeOffset? _sessionStartUtc;

    public StartupTracker(ReentrySettings? settings = null)
    {
        _settings = settings ?? new ReentrySettings();
    }

    public DateTimeOffset? SessionStartUtc => _sessionStartUtc;

    public IReadOnlyList<TrackedApp> Tick(
        DateTimeOffset now,
        IProcessProbe probe,
        IReadOnlyList<StartupInventoryItem> inventory,
        SessionSnapshot? lastSession,
        IReadOnlyCollection<ManagedEntry>? managed = null)
    {
        _sessionStartUtc ??= now;

        var expected = ExpectationBuilder.Build(lastSession, inventory, managed ?? []);
        var processes = probe.GetProcesses();
        var windows = probe.GetVisibleWindows();
        var result = new List<TrackedApp>(expected.Count);

        foreach (var app in expected)
        {
            if (!_observations.TryGetValue(app.Id, out var obs))
            {
                obs = new Observation { FirstExpectedUtc = now };
                _observations[app.Id] = obs;
            }

            app.FirstExpectedUtc = obs.FirstExpectedUtc;

            var live = processes.FirstOrDefault(p => CommandText.SameExecutable(p.Executable, app.Executable));
            var window = windows.FirstOrDefault(w => w.IsVisible && CommandText.SameExecutable(w.Executable, app.Executable));

            if (live is not null)
            {
                obs.SawProcess = true;
                obs.ProcessFirstSeenUtc ??= now;
                obs.ProcessLastSeenUtc = now;
            }

            if (window is not null)
            {
                obs.SawWindow = true;
                obs.WindowFirstSeenUtc ??= now;
            }

            app.State = Decide(app, obs, live is not null, now);
            if (app.State.IsSettled())
            {
                obs.SettledUtc ??= now;
                app.Elapsed = obs.SettledUtc.Value - obs.FirstExpectedUtc;
            }
            else
            {
                app.Elapsed = now - obs.FirstExpectedUtc;
            }

            result.Add(app);
        }

        return TrackedAppSorter.Sort(result);
    }

    private AppState Decide(TrackedApp app, Observation obs, bool processAlive, DateTimeOffset now)
    {
        if (!app.IsEnabled)
            return AppState.Disabled;

        // Latch Interactive once a visible window appeared — that's "initialized".
        if (obs.SawWindow)
            return AppState.Interactive;

        var elapsed = now - obs.FirstExpectedUtc;
        var hungAfter = TimeSpan.FromSeconds(Math.Max(1, _settings.HungNoWindowSeconds));
        var cap = TimeSpan.FromMinutes(Math.Max(1, _settings.GlobalCapMinutes));

        if (processAlive)
        {
            // Tray / background startup apps (Dropbox, Everything, …) never show a
            // normal visible HWND. Alive is Interactive. Restore rows still need a window.
            if (!app.Source.ExpectsVisibleWindow())
                return AppState.Interactive;

            if (elapsed >= hungAfter || elapsed >= cap)
                return AppState.Hung;
            return AppState.Starting;
        }

        if (obs.SawProcess)
        {
            // Exited without ever showing a window. "Failed = exited quickly no window."
            return AppState.Failed;
        }

        if (elapsed >= cap)
            return AppState.Failed;

        return AppState.Pending;
    }

    private sealed class Observation
    {
        public DateTimeOffset FirstExpectedUtc { get; init; }
        public DateTimeOffset? ProcessFirstSeenUtc { get; set; }
        public DateTimeOffset? ProcessLastSeenUtc { get; set; }
        public DateTimeOffset? WindowFirstSeenUtc { get; set; }
        public DateTimeOffset? SettledUtc { get; set; }
        public bool SawProcess { get; set; }
        public bool SawWindow { get; set; }
    }
}
