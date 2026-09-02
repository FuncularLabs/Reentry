using Microsoft.Data.Sqlite;
using Reentry.Core.Models;

namespace Reentry.Core.Timing;

/// <summary>
/// Local SQLite capture of per-boot section and per-app settle durations.
/// Last / average exclude the in-progress session. Nothing is uploaded.
/// </summary>
public sealed class TimingStore : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly object _gate = new();
    private bool _disposed;

    public TimingStore(string? dataDirectory = null)
    {
        var dir = dataDirectory ?? ReentryPaths.GetDataDirectory();
        Directory.CreateDirectory(dir);
        PathOnDisk = Path.Combine(dir, ReentryPaths.TimingsFileName);

        // Pooling=False so tests can delete the temp dir after Dispose.
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = PathOnDisk,
            Pooling = false,
        };
        _connection = new SqliteConnection(builder.ConnectionString);
        _connection.Open();
        Initialize();
    }

    public string PathOnDisk { get; }

    public long BeginSession(DateTimeOffset startedUtc)
    {
        lock (_gate)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = """
                INSERT INTO sessions (started_utc) VALUES ($started);
                SELECT last_insert_rowid();
                """;
            cmd.Parameters.AddWithValue("$started", ToUtcText(startedUtc));
            return (long)cmd.ExecuteScalar()!;
        }
    }

    public void RecordAppSettle(
        long sessionId,
        string appId,
        TimeSpan duration,
        AppState finalState,
        DateTimeOffset settledUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(appId);
        lock (_gate)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = """
                INSERT OR IGNORE INTO app_timings
                    (session_id, app_id, duration_ms, final_state, settled_utc)
                VALUES
                    ($session, $app, $ms, $state, $settled);
                """;
            cmd.Parameters.AddWithValue("$session", sessionId);
            cmd.Parameters.AddWithValue("$app", appId);
            cmd.Parameters.AddWithValue("$ms", ToMilliseconds(duration));
            cmd.Parameters.AddWithValue("$state", finalState.ToString());
            cmd.Parameters.AddWithValue("$settled", ToUtcText(settledUtc));
            cmd.ExecuteNonQuery();
        }
    }

    public void RecordSectionSettle(
        long sessionId,
        string section,
        TimeSpan duration,
        DateTimeOffset settledUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(section);
        lock (_gate)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = """
                INSERT OR IGNORE INTO section_timings
                    (session_id, section, duration_ms, settled_utc)
                VALUES
                    ($session, $section, $ms, $settled);
                """;
            cmd.Parameters.AddWithValue("$session", sessionId);
            cmd.Parameters.AddWithValue("$section", section);
            cmd.Parameters.AddWithValue("$ms", ToMilliseconds(duration));
            cmd.Parameters.AddWithValue("$settled", ToUtcText(settledUtc));
            cmd.ExecuteNonQuery();
        }
    }

    public TimingHistory LoadHistory(long excludeSessionId)
    {
        lock (_gate)
        {
            var sectionMs = new Dictionary<string, List<(long SessionId, long Ms)>>(StringComparer.Ordinal);
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = """
                    SELECT session_id, section, duration_ms
                    FROM section_timings
                    WHERE session_id <> $exclude
                    ORDER BY session_id;
                    """;
                cmd.Parameters.AddWithValue("$exclude", excludeSessionId);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var section = reader.GetString(1);
                    if (!sectionMs.TryGetValue(section, out var list))
                    {
                        list = [];
                        sectionMs[section] = list;
                    }

                    list.Add((reader.GetInt64(0), reader.GetInt64(2)));
                }
            }

            var appMs = new Dictionary<string, List<(long SessionId, long Ms)>>(StringComparer.Ordinal);
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = """
                    SELECT session_id, app_id, duration_ms
                    FROM app_timings
                    WHERE session_id <> $exclude
                    ORDER BY session_id;
                    """;
                cmd.Parameters.AddWithValue("$exclude", excludeSessionId);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var appId = reader.GetString(1);
                    if (!appMs.TryGetValue(appId, out var list))
                    {
                        list = [];
                        appMs[appId] = list;
                    }

                    list.Add((reader.GetInt64(0), reader.GetInt64(2)));
                }
            }

            return new TimingHistory(Summarize(sectionMs), Summarize(appMs));
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _connection.Dispose();
    }

    private void Initialize()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS sessions (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                started_utc TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS section_timings (
                session_id INTEGER NOT NULL,
                section TEXT NOT NULL,
                duration_ms INTEGER NOT NULL,
                settled_utc TEXT NOT NULL,
                PRIMARY KEY (session_id, section)
            );
            CREATE TABLE IF NOT EXISTS app_timings (
                session_id INTEGER NOT NULL,
                app_id TEXT NOT NULL,
                duration_ms INTEGER NOT NULL,
                final_state TEXT NOT NULL,
                settled_utc TEXT NOT NULL,
                PRIMARY KEY (session_id, app_id)
            );
            """;
        cmd.ExecuteNonQuery();
    }

    private static Dictionary<string, (TimeSpan? Last, TimeSpan? Average)> Summarize(
        Dictionary<string, List<(long SessionId, long Ms)>> grouped)
    {
        var result = new Dictionary<string, (TimeSpan? Last, TimeSpan? Average)>(StringComparer.Ordinal);
        foreach (var (key, samples) in grouped)
        {
            if (samples.Count == 0)
                continue;

            var last = TimeSpan.FromMilliseconds(samples[^1].Ms);
            var avg = TimeSpan.FromMilliseconds(samples.Average(s => (double)s.Ms));
            result[key] = (last, avg);
        }

        return result;
    }

    private static long ToMilliseconds(TimeSpan duration)
        => Math.Max(0, (long)Math.Round(duration.TotalMilliseconds));

    private static string ToUtcText(DateTimeOffset value)
        => value.ToUniversalTime().ToString("O");
}

public sealed class TimingHistory
{
    private readonly Dictionary<string, (TimeSpan? Last, TimeSpan? Average)> _sections;
    private readonly Dictionary<string, (TimeSpan? Last, TimeSpan? Average)> _apps;

    public TimingHistory(
        Dictionary<string, (TimeSpan? Last, TimeSpan? Average)> sections,
        Dictionary<string, (TimeSpan? Last, TimeSpan? Average)> apps)
    {
        _sections = sections;
        _apps = apps;
    }

    public TimeSpan? LastSection(string section) => Lookup(_sections, section).Last;
    public TimeSpan? AverageSection(string section) => Lookup(_sections, section).Average;
    public TimeSpan? LastApp(string appId) => Lookup(_apps, appId).Last;
    public TimeSpan? AverageApp(string appId) => Lookup(_apps, appId).Average;

    private static (TimeSpan? Last, TimeSpan? Average) Lookup(
        Dictionary<string, (TimeSpan? Last, TimeSpan? Average)> map,
        string key)
        => map.TryGetValue(key, out var pair) ? pair : (null, null);
}
