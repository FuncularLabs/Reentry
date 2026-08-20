namespace Reentry.App.Services;

public sealed class SingleInstance : IDisposable
{
    private readonly Mutex _mutex;
    private readonly EventWaitHandle _activate;
    private readonly Thread _watch;
    private bool _owned;

    public SingleInstance(string mutexName)
    {
        _mutex = new Mutex(initiallyOwned: true, mutexName, out _owned);
        _activate = new EventWaitHandle(false, EventResetMode.AutoReset, mutexName + ".activate");
        _watch = new Thread(Watch) { IsBackground = true };
        if (_owned)
            _watch.Start();
    }

    public event EventHandler? Activated;

    public bool TryAcquire() => _owned;

    public void SignalOtherInstance()
    {
        try { _activate.Set(); }
        catch { /* ignore */ }
    }

    private void Watch()
    {
        while (true)
        {
            if (_activate.WaitOne())
                Activated?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Dispose()
    {
        if (_owned)
        {
            try { _mutex.ReleaseMutex(); } catch { /* ignore */ }
        }
        _mutex.Dispose();
        _activate.Dispose();
    }
}
