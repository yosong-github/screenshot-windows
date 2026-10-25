using Pegline.Core.Domain;

namespace Pegline.Core.Tests;

public class ClotheslineTests
{
    [Fact]
    public void Hang_AddsAShot_AndIgnoresTheSamePath()
    {
        var line = new Clothesline(() => 1.25);
        var first = line.Hang("/tmp/pegline/a.png", 800, 600);

        Assert.NotNull(first.Item);
        Assert.Equal(1.25, first.Item!.TiltDegrees);
        Assert.Empty(first.Evicted);
        Assert.Null(line.Hang("/tmp/pegline/A.PNG", 10, 10).Item);
        Assert.Equal(1, line.Count);
    }

    [Fact]
    public void Hang_EvictsOldestUntilTheLineFits()
    {
        var line = new Clothesline(() => 0) { MaxItems = 2 };
        line.Hang("/tmp/pegline/1.png", 1, 1);
        line.Hang("/tmp/pegline/2.png", 1, 1);
        var third = line.Hang("/tmp/pegline/3.png", 1, 1);

        Assert.Single(third.Evicted);
        Assert.EndsWith("1.png", third.Evicted[0].Path);
        Assert.Equal(2, line.Count);
        Assert.EndsWith("3.png", line.Items[^1].Path);
    }

    [Fact]
    public void TakeDown_RemovesOnlyThatShot()
    {
        var line = new Clothesline(() => 0);
        var hung = line.Hang("/tmp/pegline/a.png", 20, 10).Item!;

        Assert.Equal(hung.Id, line.TakeDown(hung.Id)!.Id);
        Assert.Null(line.TakeDown(hung.Id));
        Assert.Equal(0, line.Count);
    }

    [Fact]
    public void Restore_DropsOldestPastCapacity_WithoutRequiringTheFiles()
    {
        var line = new Clothesline(() => 0) { MaxItems = 2 };
        var extras = line.Restore(new[]
        {
            Shot("/tmp/pegline/old.png"),
            Shot("/tmp/pegline/mid.png"),
            Shot("/tmp/pegline/new.png"),
        });

        Assert.Single(extras);
        Assert.EndsWith("old.png", extras[0].Path);
        Assert.Equal(2, line.Count);
    }

    [Fact]
    public void EmptyPath_DoesNotHang()
    {
        var line = new Clothesline(() => 0);
        Assert.Null(line.Hang("  ", 1, 1).Item);
    }

    private static HangingItem Shot(string path) => new(Guid.NewGuid(), path, 8, 8, 0.5);
}
