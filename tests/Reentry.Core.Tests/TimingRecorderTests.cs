using Reentry.Core.Models;
using Reentry.Core.Tests.Support;
using Reentry.Core.Timing;

namespace Reentry.Core.Tests;

public class TimingRecorderTests
{
    private static readonly DateTimeOffset T0 = new(2026, 9, 2, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Observe_SectionCurrent_GrowsThenFreezesWhenAllSettled()
    {
        using var dir = new TempDir();
        using var store = new TimingStore(dir.Path);
        var sessionId = store.BeginSession(T0);
        var recorder = new SessionTimingRecorder(store, sessionId, T0);

        var dropbox = App("dropbox", AppSource.Run, AppState.Starting, TimeSpan.FromSeconds(3));
        var everything = App("everything", AppSource.StartupFolder, AppState.Pending, TimeSpan.FromSeconds(3));
        var live = recorder.Observe([dropbox, everything], T0.AddSeconds(3));
        Assert.Equal(TimeSpan.FromSeconds(3), live.Startup.Current);
        Assert.Null(live.Startup.Last);
        Assert.Null(live.Startup.Average);

        dropbox = App("dropbox", AppSource.Run, AppState.Interactive, TimeSpan.FromSeconds(8));
        everything = App("everything", AppSource.StartupFolder, AppState.Pending, TimeSpan.FromSeconds(8));
        var mid = recorder.Observe([dropbox, everything], T0.AddSeconds(8));
        Assert.Equal(TimeSpan.FromSeconds(8), mid.Startup.Current);
        Assert.Equal(TimeSpan.FromSeconds(8), mid.Apps["dropbox"].Current);

        everything = App("everything", AppSource.StartupFolder, AppState.Interactive, TimeSpan.FromSeconds(11));
        var settled = recorder.Observe([dropbox, everything], T0.AddSeconds(11));
        Assert.Equal(TimeSpan.FromSeconds(11), settled.Startup.Current);

        var later = recorder.Observe([dropbox, everything], T0.AddSeconds(40));
        Assert.Equal(TimeSpan.FromSeconds(11), later.Startup.Current);
        Assert.Equal(TimeSpan.FromSeconds(8), later.Apps["dropbox"].Current);
        Assert.Equal(TimeSpan.FromSeconds(11), later.Apps["everything"].Current);
    }

    [Fact]
    public void Observe_PersistsSettles_LastAndAvgOnNextBootExcludeCurrent()
    {
        using var dir = new TempDir();
        using var store = new TimingStore(dir.Path);

        var boot1 = store.BeginSession(T0);
        var rec1 = new SessionTimingRecorder(store, boot1, T0);
        rec1.Observe(
            [App("dropbox", AppSource.Run, AppState.Interactive, TimeSpan.FromSeconds(8))],
            T0.AddSeconds(8));
        rec1.Observe(
            [
                App("dropbox", AppSource.Run, AppState.Interactive, TimeSpan.FromSeconds(8)),
                App("outlook", AppSource.Arr, AppState.Interactive, TimeSpan.FromSeconds(12)),
            ],
            T0.AddSeconds(12));

        var boot2 = store.BeginSession(T0.AddHours(1));
        var rec2 = new SessionTimingRecorder(store, boot2, T0.AddHours(1));
        rec2.Observe(
            [App("dropbox", AppSource.Run, AppState.Interactive, TimeSpan.FromSeconds(16))],
            T0.AddHours(1).AddSeconds(16));
        rec2.Observe(
            [
                App("dropbox", AppSource.Run, AppState.Interactive, TimeSpan.FromSeconds(16)),
                App("outlook", AppSource.Arr, AppState.Interactive, TimeSpan.FromSeconds(20)),
            ],
            T0.AddHours(1).AddSeconds(20));

        var boot3 = store.BeginSession(T0.AddHours(2));
        var rec3 = new SessionTimingRecorder(store, boot3, T0.AddHours(2));
        var view = rec3.Observe(
            [
                App("dropbox", AppSource.Run, AppState.Starting, TimeSpan.FromSeconds(2)),
                App("outlook", AppSource.Arr, AppState.Starting, TimeSpan.FromSeconds(2)),
            ],
            T0.AddHours(2).AddSeconds(2));

        Assert.Equal(TimeSpan.FromSeconds(16), view.Startup.Last);
        Assert.Equal(TimeSpan.FromSeconds(12), view.Startup.Average);
        Assert.Equal(TimeSpan.FromSeconds(2), view.Startup.Current);
        Assert.Equal(TimeSpan.FromSeconds(20), view.Restore.Last);
        Assert.Equal(TimeSpan.FromSeconds(16), view.Restore.Average);
        Assert.Equal(TimeSpan.FromSeconds(16), view.Apps["dropbox"].Last);
        Assert.Equal(TimeSpan.FromSeconds(12), view.Apps["dropbox"].Average);
        Assert.Equal(TimeSpan.FromSeconds(2), view.Apps["dropbox"].Current);
    }

    [Fact]
    public void Observe_DoesNotOverwriteFrozenDuration()
    {
        using var dir = new TempDir();
        using var store = new TimingStore(dir.Path);
        var boot = store.BeginSession(T0);
        var recorder = new SessionTimingRecorder(store, boot, T0);
        recorder.Observe(
            [App("dropbox", AppSource.Run, AppState.Interactive, TimeSpan.FromSeconds(8))],
            T0.AddSeconds(8));
        recorder.Observe(
            [App("dropbox", AppSource.Run, AppState.Failed, TimeSpan.FromSeconds(99))],
            T0.AddSeconds(99));

        var next = store.BeginSession(T0.AddHours(1));
        var history = store.LoadHistory(next);
        Assert.Equal(TimeSpan.FromSeconds(8), history.LastApp("dropbox"));
        Assert.Equal(TimeSpan.FromSeconds(8), history.LastSection(TimingSections.Startup));
    }

    [Fact]
    public void Observe_EmptySection_DoesNotPersistZero()
    {
        using var dir = new TempDir();
        using var store = new TimingStore(dir.Path);
        var boot = store.BeginSession(T0);
        var recorder = new SessionTimingRecorder(store, boot, T0);
        recorder.Observe(
            [App("dropbox", AppSource.Run, AppState.Interactive, TimeSpan.FromSeconds(5))],
            T0.AddSeconds(5));

        var next = store.BeginSession(T0.AddHours(1));
        var history = store.LoadHistory(next);
        Assert.Null(history.LastSection(TimingSections.Restore));
        Assert.Equal(TimeSpan.FromSeconds(5), history.LastSection(TimingSections.Startup));
    }

    [Fact]
    public void Flush_ReplaysLastObservation()
    {
        using var dir = new TempDir();
        using var store = new TimingStore(dir.Path);
        var boot = store.BeginSession(T0);
        var recorder = new SessionTimingRecorder(store, boot, T0);
        recorder.Observe(
            [App("dropbox", AppSource.Run, AppState.Interactive, TimeSpan.FromSeconds(7))],
            T0.AddSeconds(7));
        recorder.Flush(T0.AddSeconds(30));

        var next = store.BeginSession(T0.AddHours(1));
        Assert.Equal(TimeSpan.FromSeconds(7), store.LoadHistory(next).LastApp("dropbox"));
    }

    private static TrackedApp App(string id, AppSource source, AppState state, TimeSpan elapsed)
        => new()
        {
            Id = id,
            Name = id,
            Executable = id + ".exe",
            Source = source,
            State = state,
            Elapsed = elapsed,
        };
}
