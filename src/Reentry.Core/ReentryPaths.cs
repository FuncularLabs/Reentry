namespace Reentry.Core;

/// <summary>
/// On-disk locations. Default store is %LOCALAPPDATA%\Reentry; override with REENTRY_DATA_DIR.
/// </summary>
public static class ReentryPaths
{
    public const string DataDirectoryEnvironmentVariable = "REENTRY_DATA_DIR";
    public const string SettingsFileName = "settings.json";
    public const string LastSessionFileName = "last-session.json";
    public const string ManagedEntriesFileName = "managed-entries.json";

    public static string GetDataDirectory()
    {
        var overrideDir = Environment.GetEnvironmentVariable(DataDirectoryEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(overrideDir))
            return Path.GetFullPath(overrideDir);

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Reentry");
    }

    public static string SettingsPath => Path.Combine(GetDataDirectory(), SettingsFileName);
    public static string LastSessionPath => Path.Combine(GetDataDirectory(), LastSessionFileName);
    public static string ManagedEntriesPath => Path.Combine(GetDataDirectory(), ManagedEntriesFileName);

    public static void EnsureDataDirectory()
        => Directory.CreateDirectory(GetDataDirectory());
}
