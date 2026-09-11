using Reentry.Core;

namespace Reentry.App;

internal static class StartupLog
{
    internal static void Write(string message)
    {
        try
        {
            ReentryPaths.EnsureDataDirectory();
            var path = Path.Combine(ReentryPaths.GetDataDirectory(), "launch.log");
            File.AppendAllText(path, DateTime.Now.ToString("o") + " " + message + Environment.NewLine);
        }
        catch { }
    }

    internal static void Write(Exception ex) => Write(ex.ToString());
}