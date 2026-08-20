using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using CommunityToolkit.Mvvm.Input;
using Reentry.Core.Abstractions;
using Reentry.Core.Inventory;
using Reentry.Core.Managed;
using Reentry.Core.Models;
using Reentry.Core.Settings;

namespace Reentry.App.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly SettingsStore _settings;
    private readonly StartupInventory _inventory;
    private readonly IRegistryWriter _writer;
    private readonly ManagedEntryStore _managed;
    private readonly IAutostartRegistration _autostart;
    private readonly string _exe;

    public SettingsViewModel(
        SettingsStore settings,
        StartupInventory inventory,
        IRegistryWriter writer,
        ManagedEntryStore managed,
        IAutostartRegistration autostart,
        string executablePath)
    {
        _settings = settings;
        _inventory = inventory;
        _writer = writer;
        _managed = managed;
        _autostart = autostart;
        _exe = executablePath;
        AutostartEnabled = settings.Current.AutostartEnabled;
        HungNoWindowSeconds = settings.Current.HungNoWindowSeconds;
        FailedExitSeconds = settings.Current.FailedExitSeconds;
        Refresh();
    }

    public ObservableCollection<InventoryRow> Items { get; } = [];

    [ObservableProperty] private bool _autostartEnabled;
    [ObservableProperty] private double _hungNoWindowSeconds;
    [ObservableProperty] private double _failedExitSeconds;
    [ObservableProperty] private string _newName = "";
    [ObservableProperty] private string _newCommand = "";
    [ObservableProperty] private string _status = "";

    [RelayCommand]
    public void Refresh()
    {
        Items.Clear();
        foreach (var item in _inventory.Collect()
                     .OrderByDescending(i => _managed.IsManaged(i))
                     .ThenBy(i => i.Name, StringComparer.OrdinalIgnoreCase))
        {
            Items.Add(new InventoryRow
            {
                Item = item,
                IsManaged = _managed.IsManaged(item),
            });
        }
    }

    [RelayCommand]
    public void Toggle(InventoryRow? row)
    {
        if (row is null)
            return;
        if (!row.Item.IsUserScope)
        {
            Status = "Machine-wide entries are listed read-only.";
            return;
        }

        var next = !row.Item.IsEnabled;
        new StartupToggle(_writer).SetEnabled(row.Item, next);
        row.Notify();
        Status = next ? $"Enabled {row.Item.Name}." : $"Disabled {row.Item.Name} (Run value kept).";
    }

    [RelayCommand]
    public void AddManaged()
    {
        if (string.IsNullOrWhiteSpace(NewName) || string.IsNullOrWhiteSpace(NewCommand))
        {
            Status = "Name and command are required.";
            return;
        }

        _managed.AddUserRun(_writer, NewName.Trim(), NewCommand.Trim());
        NewName = "";
        NewCommand = "";
        Refresh();
        Status = "Added a Reentry-managed user Run entry.";
    }

    [RelayCommand]
    public void RemoveManaged(InventoryRow? row)
    {
        if (row is null || !row.IsManaged)
            return;
        _managed.RemoveUserRun(_writer, row.Item.ValueName ?? row.Item.Name);
        Refresh();
        Status = $"Removed {row.Item.Name}.";
    }

    [RelayCommand]
    public void SaveAutostart()
    {
        _settings.Update(s =>
        {
            s.AutostartEnabled = AutostartEnabled;
            s.AutostartConsentGiven = true;
            s.HungNoWindowSeconds = (int)HungNoWindowSeconds;
            s.FailedExitSeconds = (int)FailedExitSeconds;
        });

        if (AutostartEnabled)
        {
            _autostart.Register(_exe, "/autostart");
            _autostart.SetEnabled(true);
        }
        else
        {
            _autostart.SetEnabled(false);
        }

        Status = "Saved.";
    }
}

public sealed partial class InventoryRow : ObservableObject
{
    public required StartupInventoryItem Item { get; init; }
    public bool IsManaged { get; init; }
    public string Badge => IsManaged ? "Reentry-managed" : "";
    public Visibility ManagedVisibility => IsManaged ? Visibility.Visible : Visibility.Collapsed;
    public string Scope => Item.IsUserScope ? "User" : "Machine";
    public string EnabledLabel => Item.IsEnabled ? "On" : "Off";
    public string Source => Item.Source.ToString();

    public void Notify()
    {
        OnPropertyChanged(nameof(EnabledLabel));
        OnPropertyChanged(nameof(Item));
    }
}
