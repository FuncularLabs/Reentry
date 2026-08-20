using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;

namespace Reentry.App.Services;

/// <summary>
/// Subclass a WinUI HWND so WM_QUERYENDSESSION / WM_ENDSESSION write last-session.json.
/// The hook lives in App; Core only sees ISessionSnapshotter.
/// </summary>
public sealed class EndSessionHook : IDisposable
{
    private const uint WmQueryEndSession = 0x0011;
    private const uint WmEndSession = 0x0016;
    private const nuint SubclassId = 1;

    private readonly Action _onEndSession;
    private nint _hwnd;
    private SubclassProc? _proc;
    private bool _attached;

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate nint SubclassProc(
        nint hwnd,
        uint msg,
        nuint wParam,
        nint lParam,
        nuint idSubclass,
        nuint refData);

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
        _hwnd = hwnd;
        _proc = WndProc;
        _attached = SetWindowSubclass(_hwnd, _proc, SubclassId, 0);
    }

    private nint WndProc(nint hwnd, uint msg, nuint wParam, nint lParam, nuint id, nuint data)
    {
        if (msg is WmQueryEndSession or WmEndSession)
        {
            try { _onEndSession(); }
            catch { /* never block shutdown */ }
        }

        return DefSubclassProc(hwnd, msg, wParam, lParam);
    }

    private void Detach()
    {
        if (_attached && _proc is not null)
        {
            RemoveWindowSubclass(_hwnd, _proc, SubclassId);
            _attached = false;
        }
    }

    public void Dispose() => Detach();

    [DllImport("comctl32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowSubclass(
        nint hWnd,
        SubclassProc pfnSubclass,
        nuint uIdSubclass,
        nuint dwRefData);

    [DllImport("comctl32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RemoveWindowSubclass(
        nint hWnd,
        SubclassProc pfnSubclass,
        nuint uIdSubclass);

    [DllImport("comctl32.dll", ExactSpelling = true)]
    private static extern nint DefSubclassProc(
        nint hWnd,
        uint uMsg,
        nuint wParam,
        nint lParam);
}
