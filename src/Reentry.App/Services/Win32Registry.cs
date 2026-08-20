using Microsoft.Win32;
using Reentry.Core.Abstractions;
using Reentry.Core.Models;

namespace Reentry.App.Services;

public sealed class Win32Registry : IRegistryReader, IRegistryWriter
{
    public IReadOnlyDictionary<string, string>? ReadStringValues(RegistryHiveKind hive, string keyPath)
    {
        try
        {
            using var key = Open(hive, keyPath, writable: false);
            if (key is null)
                return null;

            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var name in key.GetValueNames())
            {
                if (key.GetValue(name) is string text)
                    result[name] = text;
            }

            return result;
        }
        catch (System.Security.SecurityException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    public byte[]? ReadBinaryValue(RegistryHiveKind hive, string keyPath, string valueName)
    {
        try
        {
            using var key = Open(hive, keyPath, writable: false);
            return key?.GetValue(valueName) as byte[];
        }
        catch (System.Security.SecurityException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    public void SetStringValue(RegistryHiveKind hive, string keyPath, string valueName, string value)
    {
        using var key = Ensure(hive, keyPath);
        key.SetValue(valueName, value, RegistryValueKind.String);
    }

    public void DeleteValue(RegistryHiveKind hive, string keyPath, string valueName)
    {
        using var key = Open(hive, keyPath, writable: true);
        if (key is null)
            return;
        try
        {
            key.DeleteValue(valueName, throwOnMissingValue: false);
        }
        catch (ArgumentException)
        {
            // missing is fine
        }
    }

    public void SetBinaryValue(RegistryHiveKind hive, string keyPath, string valueName, byte[] data)
    {
        using var key = Ensure(hive, keyPath);
        key.SetValue(valueName, data, RegistryValueKind.Binary);
    }

    private static RegistryKey? Open(RegistryHiveKind hive, string keyPath, bool writable)
    {
        var root = hive == RegistryHiveKind.CurrentUser ? Registry.CurrentUser : Registry.LocalMachine;
        return root.OpenSubKey(keyPath, writable);
    }

    private static RegistryKey Ensure(RegistryHiveKind hive, string keyPath)
    {
        var root = hive == RegistryHiveKind.CurrentUser ? Registry.CurrentUser : Registry.LocalMachine;
        return root.CreateSubKey(keyPath, writable: true)
               ?? throw new InvalidOperationException($"Could not open {hive}\\{keyPath}.");
    }
}
