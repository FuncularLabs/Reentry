using Windows.Win32;
using Windows.Win32.System.Recovery;

namespace Reentry.App.Services;

public static class ApplicationRestart
{
    public static void Register(string commandLine = "/autostart")
    {
        try
        {
            PInvoke.RegisterApplicationRestart(commandLine, REGISTER_APPLICATION_RESTART_FLAGS.RESTART_NO_PATCH);
        }
        catch
        {
            // ARR is best-effort; the rest of the app still works.
        }
    }
}
