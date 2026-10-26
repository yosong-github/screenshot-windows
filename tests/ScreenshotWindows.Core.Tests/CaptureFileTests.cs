using ScreenshotWindows.Core.Domain;

namespace ScreenshotWindows.Core.Tests;

public class CaptureFileTests
{
    [Fact]
    public void Names_UseASortableStamp_AndASuffixWhenNeeded()
    {
        var when = new DateTimeOffset(2026, 10, 25, 13, 33, 4, TimeSpan.Zero);
        Assert.Equal("screenshot-windows 2026-10-25 13.33.04.png", CaptureFileNames.Create(when));
        Assert.Equal("screenshot-windows 2026-10-25 13.33.04 2.png", CaptureFileNames.Create(when, 2));
    }

    [Fact]
    public void Folder_PrefersPictures_AndFallsBackToLocalAppData()
    {
        Assert.Equal(
            Path.Combine("Pictures", "screenshot-windows"),
            CaptureFolders.Resolve("Local", "Pictures"));
        Assert.Equal(
            Path.Combine("Local", "screenshot-windows", "Captures"),
            CaptureFolders.Resolve("Local", "  "));
    }
}
