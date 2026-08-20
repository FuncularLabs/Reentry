namespace Reentry.Core.Settings;

public sealed class SettingsStore
{
    private readonly string _path;

    public SettingsStore(string? dataDirectory = null)
    {
        var dir = dataDirectory ?? ReentryPaths.GetDataDirectory();
        Directory.CreateDirectory(dir);
        _path = Path.Combine(dir, ReentryPaths.SettingsFileName);
        Current = JsonUtil.Read<ReentrySettings>(_path) ?? new ReentrySettings();
    }

    public ReentrySettings Current { get; private set; }

    public string PathOnDisk => _path;

    public void Save() => JsonUtil.WriteAtomic(_path, Current);

    public void Update(Action<ReentrySettings> mutate)
    {
        mutate(Current);
        Save();
    }

    public void Reload()
        => Current = JsonUtil.Read<ReentrySettings>(_path) ?? new ReentrySettings();
}
