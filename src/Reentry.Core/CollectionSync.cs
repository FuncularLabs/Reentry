using System.Collections.ObjectModel;

namespace Reentry.Core;

/// <summary>
/// In-place collection update so a 1 Hz HUD tick does not Clear() a bound
/// ObservableCollection (that rebuilds ListViews and resets subsection scroll).
/// </summary>
public static class CollectionSync
{
    public static void InPlace<TItem, TSource, TKey>(
        ObservableCollection<TItem> target,
        IReadOnlyList<TSource> source,
        Func<TItem, TKey> itemKey,
        Func<TSource, TKey> sourceKey,
        Action<TItem, TSource> apply,
        Func<TSource, TItem> create)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(itemKey);
        ArgumentNullException.ThrowIfNull(sourceKey);
        ArgumentNullException.ThrowIfNull(apply);
        ArgumentNullException.ThrowIfNull(create);

        var existing = new Dictionary<TKey, TItem>(target.Count);
        foreach (var item in target)
            existing[itemKey(item)] = item;

        for (var i = 0; i < source.Count; i++)
        {
            var src = source[i];
            var key = sourceKey(src);
            if (existing.TryGetValue(key, out var item))
            {
                apply(item, src);
                var current = target.IndexOf(item);
                if (current != i)
                    target.Move(current, i);
            }
            else
            {
                var created = create(src);
                existing[key] = created;
                target.Insert(i, created);
            }
        }

        while (target.Count > source.Count)
            target.RemoveAt(target.Count - 1);
    }
}
