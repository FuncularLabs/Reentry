using Reentry.Core.Abstractions;
using Reentry.Core.Models;

namespace Reentry.Core.Tests.Fakes;

public sealed class FakeRegistry : IRegistryReader, IRegistryWriter
{
    private readonly Dictionary<(RegistryHiveKind Hive, string Key), Dictionary<string, object>> _store
        = new();

    public bool HideLocalMachine { get; set; }

    public void SetString(RegistryHiveKind hive, string key, string name, string value)
        => Bucket(hive, key)[name] = value;

    public void SetBinary(RegistryHiveKind hive, string key, string name, byte[] value)
        => Bucket(hive, key)[name] = value;

    public IReadOnlyDictionary<string, string>? ReadStringValues(RegistryHiveKind hive, string keyPath)
    {
        if (hive == RegistryHiveKind.LocalMachine && HideLocalMachine)
            return null;

        if (!_store.TryGetValue((hive, keyPath), out var bucket))
            return null;

        return bucket
            .Where(kv => kv.Value is string)
            .ToDictionary(kv => kv.Key, kv => (string)kv.Value, StringComparer.OrdinalIgnoreCase);
    }

    public byte[]? ReadBinaryValue(RegistryHiveKind hive, string keyPath, string valueName)
    {
        if (hive == RegistryHiveKind.LocalMachine && HideLocalMachine)
            return null;

        if (!_store.TryGetValue((hive, keyPath), out var bucket))
            return null;

        return bucket.TryGetValue(valueName, out var value) ? value as byte[] : null;
    }

    public void SetStringValue(RegistryHiveKind hive, string keyPath, string valueName, string value)
        => SetString(hive, keyPath, valueName, value);

    public void DeleteValue(RegistryHiveKind hive, string keyPath, string valueName)
    {
        if (_store.TryGetValue((hive, keyPath), out var bucket))
            bucket.Remove(valueName);
    }

    public void SetBinaryValue(RegistryHiveKind hive, string keyPath, string valueName, byte[] data)
        => SetBinary(hive, keyPath, valueName, data);

    public bool HasString(RegistryHiveKind hive, string key, string name)
        => _store.TryGetValue((hive, key), out var bucket) && bucket.ContainsKey(name);

    private Dictionary<string, object> Bucket(RegistryHiveKind hive, string key)
    {
        var id = (hive, key);
        if (!_store.TryGetValue(id, out var bucket))
        {
            bucket = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            _store[id] = bucket;
        }
        return bucket;
    }
}
