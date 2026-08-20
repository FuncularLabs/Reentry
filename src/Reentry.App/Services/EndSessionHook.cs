using Microsoft.UI.Xaml;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Controls;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Reentry.App.Services;

/// <summary>
/// Subclass a WinUI HWND so WM_QUERYENDSESSION / WM_ENDSESSION write last-session.json.
/// The hook lives in App; Core only sees ISessionSnapshotter.
/// </summary>
public sealed class EndSessionHook : IDisposable
{
    private const uint WmQueryEndSession = 0x0011;
    private const uint WmEndSession = 0x0016;
    private readonly Action _onEndSession;
    private HWND _hwnd;
    private SUBCLASSPROC? _proc;
    private bool _attached;

    public EndSessionHook(Action onEndSession)
    {
        _onEndSession = onEndSession;
    }

    public void Attach(Window window)
    {
        var handle = window switch
        {
            MainWindow hud => hud.Handle,
            SettingsWindow settings => settings.Handle,
            _ => WinRT.Interop.WindowNative.GetWindowHandle(window),
        };
        Attach(handle);
    }

    public void Attach(nint hwnd)
    {
        Detach();
        _hwnd = new HWND(hwnd);
        _proc = WndProc;
        _attached = PInvoke.SetWindowSubclass(_hwnd, _proc, 1, 0);
    }

    private LRESULT WndProc(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam, nuint id, nuint data)
    {
        if (msg is WmQueryEndSession or WmEndSession)
        {
            try { _onEndSession(); }
            catch { /* never block shutdown */ }
        }

        return PInvoke.DefSubclassProc(hwnd, msg, wParam, lParam);
    }

    private void Detach()
    {
        if (_attached && _proc is not null)
        {
            PInvoke.RemoveWindowSubclass(_hwnd, _proc, 1);
            _attached = false;
        }
    }

    public void Dispose() => Detach();
}
