using Reentry.Core;
using Reentry.Core.Settings;
using Reentry.Core.Tests.Support;

namespace Reentry.Core.Tests;

public class SettingsPathsTests
{
    [Fact]
    public void DataDirectory_UsesLocalAppData_ByDefault()
    {
        var previous = Environment.GetEnvironmentVariable(ReentryPaths.DataDirectoryEnvironmentVariable);
        try
        {
            Environment.SetEnvironmentVariable(ReentryPaths.DataDirectoryEnvironmentVariable, null);
            var dir = ReentryPaths.GetDataDirectory();
            Assert.EndsWith("Reentry", dir);
            Assert.Contains(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                dir);
        }
        finally
        {
            Environment.SetEnvironmentVariable(ReentryPaths.DataDirectoryEnvironmentVariable, previous);
        }
    }

    [Fact]
    public void DataDirectory_HonorsOverride()
    {
        using var tmp = new TempDir();
        var previous = Environment.GetEnvironmentVariable(ReentryPaths.DataDirectoryEnvironmentVariable);
        try
        {
            Environment.SetEnvironmentVariable(ReentryPaths.DataDirectoryEnvironmentVariable, tmp.Path);
            Assert.Equal(Path.GetFullPath(tmp.Path), ReentryPaths.GetDataDirectory());
            Assert.Equal(Path.Combine(Path.GetFullPath(tmp.Path), "settings.json"), ReentryPaths.SettingsPath);
            Assert.Equal(Path.Combine(Path.GetFullPath(tmp.Path), "last-session.json"), ReentryPaths.LastSessionPath);
        }
        finally
        {
            Environment.SetEnvironmentVariable(ReentryPaths.DataDirectoryEnvironmentVariable, previous);
        }
    }

    [Fact]
    public void Settings_DefaultsAreUsableOutOfTheBox()
    {
        using var dir = new TempDir();
        var store = new SettingsStore(dir.Path);

        Assert.False(store.Current.AutostartConsentGiven);
        Assert.False(store.Current.AutostartEnabled);
        Assert.Equal(15, store.Current.FailedExitSeconds);
        Assert.Equal(90, store.Current.HungNoWindowSeconds);
        Assert.Equal(10, store.Current.GlobalCapMinutes);
        Assert.Equal(30, store.Current.SnapshotIntervalSeconds);
    }

    [Fact]
    public void Settings_PersistAcrossInstances()
    {
        using var dir = new TempDir();

        var first = new SettingsStore(dir.Path);
        first.Update(s =>
        {
            s.AutostartConsentGiven = true;
            s.AutostartEnabled = true;
            s.HungNoWindowSeconds = 120;
        });

        var second = new SettingsStore(dir.Path);
        Assert.True(second.Current.AutostartConsentGiven);
        Assert.True(second.Current.AutostartEnabled);
        Assert.Equal(120, second.Current.HungNoWindowSeconds);
    }
}
