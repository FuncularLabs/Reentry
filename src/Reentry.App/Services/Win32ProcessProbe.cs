using System.Diagnostics;
using Reentry.Core.Abstractions;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Threading;

namespace Reentry.App.Services;

public sealed unsafe class Win32ProcessProbe : IProcessProbe
{
    public IReadOnlyList<LiveProcess> GetProcesses()
    {
        var list = new List<LiveProcess>();
        foreach (var process in Process.GetProcesses())
        {
            try
            {
                var exe = QueryImage(process.Id) ?? process.ProcessName;
                DateTimeOffset started;
                try { started = process.StartTime.ToUniversalTime(); }
                catch { started = DateTimeOffset.UtcNow; }

                list.Add(new LiveProcess(process.Id, exe, commandLine: null, started));
            }
            catch
            {
                // access-denied processes are skipped
            }
            finally
            {
                process.Dispose();
            }
        }

        return list;
    }

    public IReadOnlyList<LiveWindow> GetVisibleWindows()
    {
        var list = new List<LiveWindow>();
        PInvoke.EnumWindows((hwnd, _) =>
        {
            if (!PInvoke.IsWindowVisible(hwnd))
                return true;

            uint pid = 0;
            _ = PInvoke.GetWindowThreadProcessId(hwnd, &pid);
            var title = ReadTitle(hwnd);
            var exe = QueryImage((int)pid) ?? "";
            list.Add(new LiveWindow(hwnd.Value, (int)pid, title, exe, IsVisible: true));
            return true;
        }, 0);
        return list;
    }

    public bool IsArrRegistered(int processId)
        => processId == Environment.ProcessId;

    private static unsafe string? QueryImage(int pid)
    {
        HANDLE handle = PInvoke.OpenProcess(PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_LIMITED_INFORMATION, false, (uint)pid);
        if (handle.IsNull)
            return null;

        try
        {
            Span<char> buffer = stackalloc char[512];
            uint size = (uint)buffer.Length;
            fixed (char* p = buffer)
            {
                if (!PInvoke.QueryFullProcessImageName(handle, PROCESS_NAME_FORMAT.PROCESS_NAME_WIN32, p, &size))
                    return null;
            }

            return new string(buffer[..(int)size]);
        }
        finally
        {
            PInvoke.CloseHandle(handle);
        }
    }

    private static string ReadTitle(HWND hwnd)
    {
        var length = PInvoke.GetWindowTextLength(hwnd);
        if (length <= 0)
            return "";
        Span<char> buffer = stackalloc char[length + 1];
        var written = PInvoke.GetWindowText(hwnd, buffer);
        return written <= 0 ? "" : new string(buffer[..written]);
    }
}
