using Pegline.Core.Domain;

namespace Pegline.Core.Tests;

public class CaptureFileTests
{
    [Fact]
    public void Names_UseASortableStamp_AndASuffixWhenNeeded()
    {
        var when = new DateTimeOffset(2026, 10, 25, 13, 33, 4, TimeSpan.Zero);
        Assert.Equal("Pegline 2026-10-25 13.33.04.png", CaptureFileNames.Create(when));
        Assert.Equal("Pegline 2026-10-25 13.33.04 2.png", CaptureFileNames.Create(when, 2));
    }

    [Fact]
    public void Folder_PrefersPictures_AndFallsBackToLocalAppData()
    {
        Assert.Equal(
            Path.Combine("Pictures", "Pegline"),
            CaptureFolders.Resolve("Local", "Pictures"));
        Assert.Equal(
            Path.Combine("Local", "Pegline", "Captures"),
            CaptureFolders.Resolve("Local", "  "));
    }
}
