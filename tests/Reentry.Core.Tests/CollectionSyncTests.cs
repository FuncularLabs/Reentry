using System.Collections.ObjectModel;
using Reentry.Core;

namespace Reentry.Core.Tests;

public class CollectionSyncTests
{
    private sealed class Row
    {
        public required string Id { get; init; }
        public string Name { get; set; } = "";
        public int Applies { get; set; }
    }

    private readonly record struct Source(string Id, string Name);

    [Fact]
    public void InPlace_EmptyTarget_InsertsInOrder()
    {
        var target = new ObservableCollection<Row>();
        Sync(target, [new("b", "B"), new("a", "A")]);
        Assert.Equal(["b", "a"], target.Select(r => r.Id).ToArray());
        Assert.Equal(["B", "A"], target.Select(r => r.Name).ToArray());
    }

    [Fact]
    public void InPlace_SameIds_AppliesExistingInstances_DoesNotReplace()
    {
        var target = new ObservableCollection<Row>
        {
            new() { Id = "steam", Name = "Steam" },
            new() { Id = "dropbox", Name = "Dropbox" },
        };
        var steam = target[0];
        var dropbox = target[1];

        Sync(target, [new("steam", "Steam (running)"), new("dropbox", "Dropbox")]);

        Assert.Equal(2, target.Count);
        Assert.Same(steam, target[0]);
        Assert.Same(dropbox, target[1]);
        Assert.Equal("Steam (running)", steam.Name);
        Assert.Equal(1, steam.Applies);
        Assert.Equal(1, dropbox.Applies);
    }

    [Fact]
    public void InPlace_RemovesMissing_InsertsNew_PreservesOrder()
    {
        var target = new ObservableCollection<Row>
        {
            new() { Id = "gone", Name = "Gone" },
            new() { Id = "keep", Name = "Keep" },
        };
        var keep = target[1];

        Sync(target, [new("keep", "Keep 2"), new("new", "New")]);

        Assert.Equal(["keep", "new"], target.Select(r => r.Id).ToArray());
        Assert.Same(keep, target[0]);
        Assert.Equal("Keep 2", keep.Name);
        Assert.Equal("New", target[1].Name);
    }

    [Fact]
    public void InPlace_ReordersExistingWithoutClear()
    {
        var target = new ObservableCollection<Row>
        {
            new() { Id = "a", Name = "A" },
            new() { Id = "b", Name = "B" },
            new() { Id = "c", Name = "C" },
        };
        var a = target[0];
        var b = target[1];
        var c = target[2];

        var changes = 0;
        target.CollectionChanged += (_, _) => changes++;

        Sync(target, [new("c", "C"), new("a", "A"), new("b", "B")]);

        Assert.Equal(["c", "a", "b"], target.Select(r => r.Id).ToArray());
        Assert.Same(c, target[0]);
        Assert.Same(a, target[1]);
        Assert.Same(b, target[2]);
        Assert.True(changes > 0);
    }

    [Fact]
    public void InPlace_IdenticalOrder_DoesNotRaiseCollectionChanged()
    {
        var target = new ObservableCollection<Row>
        {
            new() { Id = "a", Name = "A" },
            new() { Id = "b", Name = "B" },
        };

        var changes = 0;
        target.CollectionChanged += (_, _) => changes++;

        Sync(target, [new("a", "A2"), new("b", "B2")]);

        Assert.Equal(0, changes);
        Assert.Equal("A2", target[0].Name);
        Assert.Equal("B2", target[1].Name);
    }

    [Fact]
    public void InPlace_ClearsByRemovingTail_WhenSourceEmpty()
    {
        var target = new ObservableCollection<Row>
        {
            new() { Id = "a", Name = "A" },
            new() { Id = "b", Name = "B" },
        };

        var reset = 0;
        target.CollectionChanged += (_, e) =>
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
                reset++;
        };

        Sync(target, []);

        Assert.Empty(target);
        Assert.Equal(0, reset);
    }

    private static void Sync(ObservableCollection<Row> target, IReadOnlyList<Source> source)
    {
        CollectionSync.InPlace(
            target,
            source,
            itemKey: r => r.Id,
            sourceKey: s => s.Id,
            apply: (row, src) =>
            {
                row.Name = src.Name;
                row.Applies++;
            },
            create: s => new Row { Id = s.Id, Name = s.Name });
    }
}
