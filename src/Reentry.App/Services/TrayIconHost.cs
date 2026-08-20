using H.NotifyIcon;
using Microsoft.UI.Xaml.Controls;

namespace Reentry.App.Services;

public sealed class TrayIconHost : IDisposable
{
    private readonly Action _showHud;
    private readonly Action _showSettings;
    private readonly Action _exit;
    private TaskbarIcon? _icon;

    public TrayIconHost(Action showHud, Action showSettings, Action exit)
    {
        _showHud = showHud;
        _showSettings = showSettings;
        _exit = exit;
    }

    public void Show()
    {
        try
        {
            var menu = new MenuFlyout();
            menu.Items.Add(Item("Show progress", _showHud));
            menu.Items.Add(Item("Settings", _showSettings));
            menu.Items.Add(new MenuFlyoutSeparator());
            menu.Items.Add(Item("Exit", _exit));

            _icon = new TaskbarIcon
            {
                ToolTipText = "Reentry",
                ContextFlyout = menu,
                IconSource = new GeneratedIconSource { Text = "R" },
            };
            _icon.ForceCreate();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
        }
    }

    private static MenuFlyoutItem Item(string text, Action action)
    {
        var item = new MenuFlyoutItem { Text = text };
        item.Click += (_, _) => action();
        return item;
    }

    public void Dispose() => _icon?.Dispose();
}
