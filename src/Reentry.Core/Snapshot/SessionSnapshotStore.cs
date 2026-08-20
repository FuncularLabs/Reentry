using Reentry.Core.Models;

namespace Reentry.Core.Snapshot;

public sealed class SessionSnapshotStore
{
    private readonly string _path;

    public SessionSnapshotStore(string? dataDirectory = null)
    {
        var dir = dataDirectory ?? ReentryPaths.GetDataDirectory();
        Directory.CreateDirectory(dir);
        _path = Path.Combine(dir, ReentryPaths.LastSessionFileName);
    }

    public string PathOnDisk => _path;

    public void Write(SessionSnapshot snapshot)
        => JsonUtil.WriteAtomic(_path, snapshot);

    public SessionSnapshot? Read()
        => JsonUtil.Read<SessionSnapshot>(_path);
}
