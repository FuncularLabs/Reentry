using Microsoft.UI.Xaml;
using Reentry.App.Services;
using Reentry.Core.Abstractions;
using Reentry.App.ViewModels;
using Reentry.Core;
using Reentry.Core.Boot;
using Reentry.Core.Inventory;
using Reentry.Core.Managed;
using Reentry.Core.Models;
using Reentry.Core.Settings;
using Reentry.Core.Snapshot;
using Reentry.Core.Tracking;

namespace Reentry.App;

public partial class App : Application
{
    public const string MutexName = @"Local\FuncularLabs.Reentry.1";

    private SingleInstance? _instance;
    private SettingsStore? _settings;
    private ManagedEntryStore? _managed;
    private SessionSnapshotStore? _snapshots;
    private ISessionSnapshotter? _snapshotter;
    private StartupInventory? _inventory;
    private StartupTracker? _tracker;
    private Win32ProcessProbe? _probe;
    private Win32Registry? _registry;
    private AutostartRegistration? _autostart;
    private EndSessionHook? _endSession;
    private TrayIconHost? _tray;
    private DispatcherTimer? _snapshotTimer;
    private DispatcherTimer? _tickTimer;
    private MainWindow? _hud;
    private SettingsWindow? _settingsWindow;
    private HudViewModel? _hudVm;

    public App()
    {
        InitializeComponent();
        UnhandledException += (_, e) =>
        {
            e.Handled = true;
            System.Diagnostics.Debug.WriteLine(e.Message);
        };
    }

    public BootKind BootKind { get; private set; } = BootKind.Ordinary;

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var argv = Environment.GetCommandLineArgs().Skip(1).ToArray();
        if (HasFlag(argv, "/uninstall") || HasFlag(argv, "/cleanup"))
        {
            RunCleanup();
            Exit();
            return;
        }

        _instance = new SingleInstance(MutexName);
        if (!_instance.TryAcquire())
        {
            _instance.SignalOtherInstance();
            Exit();
            return;
        }

        ReentryPaths.EnsureDataDirectory();
        _settings = new SettingsStore();
        _managed = new ManagedEntryStore();
        _snapshots = new SessionSnapshotStore();
        _registry = new Win32Registry();
        _probe = new Win32ProcessProbe();
        _inventory = new StartupInventory(_registry, new Win32FileSystemProbe());
        _snapshotter = new SessionSnapshotter(_probe, () => _inventory.Collect());
        _tracker = new StartupTracker(_settings.Current);
        _autostart = new AutostartRegistration(_registry);

        ApplicationRestart.Register("/autostart");

        _ = LaunchAsync(argv);
    }

    private async Task LaunchAsync(string[] argv)
    {
        BootKind = new BootClassifier().Classify(new Win32EventLogReader(), DateTimeOffset.UtcNow);

        if (!_settings!.Current.AutostartConsentGiven)
        {
            var consent = new ConsentWindow();
            var accepted = await consent.ShowAsync();
            _settings.Update(s =>
            {
                s.AutostartConsentGiven = true;
                s.AutostartEnabled = accepted;
            });
            if (accepted)
                RegisterAutostart();
        }
        else if (_settings.Current.AutostartEnabled)
        {
            RegisterAutostart();
        }

        var showHud = HasFlag(argv, "/autostart") || BootKind == BootKind.Unexpected;
        var forceSettings = HasFlag(argv, "/settings") || !showHud;

        if (showHud)
            ShowHud();

        if (forceSettings)
            ShowSettings();

        _tray = new TrayIconHost(
            showHud: ShowHud,
            showSettings: ShowSettings,
            exit: Exit);
        _tray.Show();

        _endSession = new EndSessionHook(WriteSnapshot);
        if (_hud is not null)
            _endSession.Attach(_hud);
        else if (_settingsWindow is not null)
            _endSession.Attach(_settingsWindow);

        StartTimers();
        _instance!.Activated += (_, _) => ShowSettings();
    }

    public void ShowHud()
    {
        if (_hud is null)
        {
            _hudVm = new HudViewModel(BootKind);
            _hud = new MainWindow(_hudVm);
            _endSession?.Attach(_hud);
        }

        RefreshHud();
        _hud.Activate();
    }

    public void ShowSettings()
    {
        if (_settingsWindow is null)
        {
            var vm = new SettingsViewModel(
                _settings!,
                _inventory!,
                _registry!,
                _managed!,
                _autostart!,
                executablePath: Environment.ProcessPath ?? "Reentry.exe");
            _settingsWindow = new SettingsWindow(vm);
            _settingsWindow.Closed += (_, _) => _settingsWindow = null;
            _endSession?.Attach(_settingsWindow);
        }

        _settingsWindow.Activate();
    }

    private void StartTimers()
    {
        var snapshotSeconds = Math.Max(10, _settings!.Current.SnapshotIntervalSeconds);
        _snapshotTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(snapshotSeconds) };
        _snapshotTimer.Tick += (_, _) => WriteSnapshot();
        _snapshotTimer.Start();

        _tickTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _tickTimer.Tick += (_, _) => RefreshHud();
        _tickTimer.Start();

        WriteSnapshot();
        RefreshHud();
    }

    private void RefreshHud()
    {
        if (_hudVm is null || _tracker is null || _inventory is null || _probe is null)
            return;

        var rows = _tracker.Tick(
            DateTimeOffset.UtcNow,
            _probe,
            _inventory.Collect(),
            _snapshots!.Read(),
            _managed!.All);
        _hudVm.ReplaceRows(rows);
    }

    private void WriteSnapshot()
    {
        try
        {
            if (_snapshotter is null || _snapshots is null)
                return;
            _snapshots.Write(_snapshotter.Capture());
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
        }
    }

    private void RegisterAutostart()
    {
        var exe = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(exe))
            return;
        _autostart!.Register(exe, "/autostart");
        _autostart.SetEnabled(_settings!.Current.AutostartEnabled);
    }

    private void RunCleanup()
    {
        try
        {
            var registry = new Win32Registry();
            new AutostartRegistration(registry).Cleanup();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
        }
    }

    private static bool HasFlag(IEnumerable<string> argv, string flag)
        => argv.Any(a => string.Equals(a, flag, StringComparison.OrdinalIgnoreCase));
}
