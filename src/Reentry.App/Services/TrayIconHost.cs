using H.NotifyIcon;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Reentry.App.Services;

public sealed class TrayIconHost : IDisposable
{
    private readonly Action _showHud;
    private readonly Action _produceChecklist;
    private readonly Action _showSettings;
    private readonly Action _exit;
    private TaskbarIcon? _icon;

    public TrayIconHost(Action showHud, Action produceChecklist, Action showSettings, Action exit)
    {
        _showHud = showHud;
        _produceChecklist = produceChecklist;
        _showSettings = showSettings;
        _exit = exit;
    }

    public void Show()
    {
        // Do not swallow create failures into a headless process — log them.
        try
        {
            var menu = new MenuFlyout();
            menu.Items.Add(Item("Show progress", _showHud));
            menu.Items.Add(Item("Produce Checklist\u2026", _produceChecklist));
            menu.Items.Add(Item("Settings", _showSettings));
            menu.Items.Add(new MenuFlyoutSeparator());
            menu.Items.Add(Item("Exit", _exit));

            _icon = new TaskbarIcon
            {
                ToolTipText = AppVersion.Moniker,
                ContextFlyout = menu,
                IconSource = BuildIconSource(),
            };
            _icon.ForceCreate();
        }
        catch (Exception ex)
        {
            Log(ex.ToString());
            throw;
        }
    }

    private static Microsoft.UI.Xaml.Media.ImageSource BuildIconSource()
    {
        var icoPath = WindowIcon.ResolvePath();
        if (icoPath is not null)
        {
            try
            {
                // BitmapImage accepts .ico URIs; System.Drawing.Icon assignment on
                // WinUI TaskbarIcon has left dogfood builds with no tray at all.
                return new BitmapImage(new Uri(icoPath, UriKind.Absolute));
            }
            catch (Exception ex)
            {
                Log(ex.ToString());
            }
        }

        return new GeneratedIconSource
        {
            Text = "R",
            BackgroundType = BackgroundType.Ellipse,
        };
    }

    private static MenuFlyoutItem Item(string text, Action action)
    {
        var item = new MenuFlyoutItem { Text = text };
        item.Click += (_, _) => action();
        return item;
    }

    private static void Log(string line)
    {
        try
        {
            var dir = Reentry.Core.ReentryPaths.GetDataDirectory();
            Directory.CreateDirectory(dir);
            File.AppendAllText(Path.Combine(dir, "tray.log"), DateTime.Now.ToString("o") + " " + line + Environment.NewLine);
        }
        catch { }
    }

    public void Dispose() => _icon?.Dispose();
}