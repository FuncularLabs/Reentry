using Reentry.Core.Models;

namespace Reentry.Core.Abstractions;

public interface IRegistryReader
{
    /// <summary>
    /// String values under <paramref name="keyPath"/>.
    /// Return null when the key is missing or the hive is unreadable (HKLM is best-effort).
    /// </summary>
    IReadOnlyDictionary<string, string>? ReadStringValues(RegistryHiveKind hive, string keyPath);

    /// <summary>Binary value, or null if missing / unreadable.</summary>
    byte[]? ReadBinaryValue(RegistryHiveKind hive, string keyPath, string valueName);
}

public interface IRegistryWriter
{
    void SetStringValue(RegistryHiveKind hive, string keyPath, string valueName, string value);
    void DeleteValue(RegistryHiveKind hive, string keyPath, string valueName);
    void SetBinaryValue(RegistryHiveKind hive, string keyPath, string valueName, byte[] data);
}
