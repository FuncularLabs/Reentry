using System.Drawing;
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
    private Icon? _ownedIcon;

    public TrayIconHost(Action showHud, Action produceChecklist, Action showSettings, Action exit)
    {
        _showHud = showHud;
        _produceChecklist = produceChecklist;
        _showSettings = showSettings;
        _exit = exit;
    }

    public void Show()
    {
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
            };

            // Same circle-R ICO as the titlebar — GeneratedIconSource text "R"
            // was microscopic in the overflow tray.
            var icoPath = WindowIcon.ResolvePath();
            if (icoPath is not null && TrySetIconFromFile(_icon, icoPath))
            {
                // ok
            }
            else
            {
                _icon.IconSource = new GeneratedIconSource
                {
                    Text = "R",
                    BackgroundType = BackgroundType.Ellipse,
                };
            }

            _icon.ForceCreate();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
        }
    }

    private bool TrySetIconFromFile(TaskbarIcon icon, string icoPath)
    {
        try
        {
            _ownedIcon = new Icon(icoPath);
            icon.Icon = _ownedIcon;
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
        }

        try
        {
            icon.IconSource = new BitmapImage(new Uri(icoPath, UriKind.Absolute));
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            return false;
        }
    }

    private static MenuFlyoutItem Item(string text, Action action)
    {
        var item = new MenuFlyoutItem { Text = text };
        item.Click += (_, _) => action();
        return item;
    }

    public void Dispose()
    {
        _icon?.Dispose();
        _ownedIcon?.Dispose();
    }
}