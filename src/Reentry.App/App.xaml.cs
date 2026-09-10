using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Reentry.App.Services;
using Reentry.App.ViewModels;
using Reentry.Core;
using Reentry.Core.Abstractions;
using Reentry.Core.Boot;
using Reentry.Core.Inventory;
using Reentry.Core.Managed;
using Reentry.Core.Models;
using Reentry.Core.Settings;
using Reentry.Core.Snapshot;
using Reentry.Core.Tracking;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

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
    private int _checklistBusy;

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

        // Manual launch (Explorer / shortcut) always shows the HUD — that is the app
        // surface. Prior code treated Ordinary interactive launches as Settings-only,
        // so closing Settings left a tray-only process with no visible window.
        // /autostart keeps showing the HUD (restore monitor at logon).
        var showHud = true;
        var forceSettings = HasFlag(argv, "/settings");

        if (showHud)
            ShowHud();

        if (forceSettings)
            ShowSettings();

        _tray = new TrayIconHost(
            showHud: ShowHud,
            produceChecklist: ProduceChecklist,
            showSettings: ShowSettings,
            exit: Exit);
        try { _tray.Show(); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }

        _endSession = new EndSessionHook(WriteSnapshot);
        if (_hud is not null)
            _endSession.Attach(_hud);
        else if (_settingsWindow is not null)
            _endSession.Attach(_settingsWindow);

        StartTimers();
        // SingleInstance watches on a background thread — marshal back to the UI queue
        // or Activate() silently does nothing / fails and a second launch looks dead.
        var ui = DispatcherQueue.GetForCurrentThread();
        _instance!.Activated += (_, _) =>
        {
            if (ui is null || !ui.TryEnqueue(() => ShowHud()))
                ShowHud();
        };
    }

    public void ShowHud()
    {
        if (_hud is null)
        {
            _hudVm = new HudViewModel(BootKind);
            _hud = new MainWindow(_hudVm);
            _hud.Closed += (_, _) => _hud = null;
            _endSession?.Attach(_hud);
        }

        RefreshHud();
        // Activate alone can leave WinUIDesktopWin32WindowClass created but invisible
        // (dogfood: process up, no tray, no caption). Show via AppWindow as well.
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(_hud);
        var id = Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = AppWindow.GetFromWindowId(id);
        if (!appWindow.IsVisible)
            appWindow.Show();
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

    public void ProduceChecklist() => _ = ProduceChecklistAsync();

    private async Task ProduceChecklistAsync()
    {
        if (Interlocked.CompareExchange(ref _checklistBusy, 1, 0) != 0)
            return;

        try
        {
            ShowHud();
            if (_hud is null || _hudVm is null)
                return;

            var hwnd = _hud.Handle;
            if (hwnd == 0)
                hwnd = _settingsWindow?.Handle ?? 0;
            if (hwnd == 0)
                return;

            var picker = new FileSavePicker();
            InitializeWithWindow.Initialize(picker, hwnd);
            picker.SuggestedFileName = ChecklistFormatter.SuggestedBaseName(DateTimeOffset.Now);
            picker.DefaultFileExtension = ".md";
            picker.FileTypeChoices.Add("Markdown", [".md"]);
            picker.FileTypeChoices.Add("Plain text", [".txt"]);
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

            var file = await picker.PickSaveFileAsync();
            if (file is null)
                return;
            if (_hud is null || _hudVm is null)
                return;

            RefreshHud();
            // Freeze the 1 Hz HUD tick so the PNG clone and the markdown
            // document read the same rows / elapsed. Restart in finally.
            _tickTimer?.Stop();
            if (_hud is null || _hudVm is null)
                return;

            var markdown = ChecklistFormatter.IsMarkdownPath(file.Name)
                           || ChecklistFormatter.IsMarkdownPath(file.Path);
            var doc = ChecklistExport.FromHud(_hudVm, DateTimeOffset.Now, imageFileName: null);
            if (markdown && !string.IsNullOrWhiteSpace(file.Path))
            {
                var pngPath = ChecklistFormatter.SiblingPngPath(file.Path);
                if (!string.IsNullOrEmpty(pngPath)
                    && await ChecklistCapture.TrySaveAsync(_hud, _hudVm, pngPath))
                {
                    doc = doc with { ImageFileName = Path.GetFileName(pngPath) };
                }
            }

            var body = markdown
                ? ChecklistFormatter.ToMarkdown(doc)
                : ChecklistFormatter.ToPlainText(doc);
            await FileIO.WriteTextAsync(file, body);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            await ShowChecklistErrorAsync(ex);
        }
        finally
        {
            if (_tickTimer is not null && !_tickTimer.IsEnabled)
                _tickTimer.Start();
            Interlocked.Exchange(ref _checklistBusy, 0);
        }
    }

    private async Task ShowChecklistErrorAsync(Exception ex)
    {
        try
        {
            var root = _hud?.Content?.XamlRoot ?? _settingsWindow?.Content?.XamlRoot;
            if (root is null)
                return;
            var dialog = new ContentDialog
            {
                Title = "Produce Checklist",
                Content = "Could not save the checklist.\n\n" + ex.Message,
                CloseButtonText = "OK",
                XamlRoot = root,
            };
            await dialog.ShowAsync();
        }
        catch (Exception inner)
        {
            System.Diagnostics.Debug.WriteLine(inner);
        }
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
