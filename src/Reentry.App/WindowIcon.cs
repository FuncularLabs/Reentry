using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinRT.Interop;

namespace Reentry.App;

/// <summary>
/// WinUI's <see cref="Window"/> does not pick up <c>ApplicationIcon</c> for the
/// caption. Every window calls <see cref="AppWindow.SetIcon"/> with the same ICO
/// so the HUD, settings, and consent title bars match the exe/taskbar glyph.
/// </summary>
internal static class WindowIcon
{
    internal const string FileName = "reentry.ico";

    public static AppWindow Apply(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);
        var hwnd = WindowNative.GetWindowHandle(window);
        var id = Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = AppWindow.GetFromWindowId(id);
        var path = ResolvePath();
        if (path is not null)
            appWindow.SetIcon(path);
        return appWindow;
    }

    internal static string? ResolvePath()
    {
        var bases = new[]
        {
            AppContext.BaseDirectory,
            Path.GetDirectoryName(Environment.ProcessPath) ?? "",
        };

        foreach (var root in bases)
        {
            if (string.IsNullOrWhiteSpace(root))
                continue;
            foreach (var relative in new[] { Path.Combine("Assets", FileName), FileName })
            {
                var candidate = Path.GetFullPath(Path.Combine(root, relative));
                if (File.Exists(candidate))
                    return candidate;
            }
        }

        return null;
    }
}
