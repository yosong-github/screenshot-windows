namespace ScreenshotWindows.Core.Domain;

public static class CaptureFolders
{
    public static string Resolve(string localAppData, string? pictures)
    {
        if (!string.IsNullOrWhiteSpace(pictures))
        {
            return Path.Combine(pictures, "screenshot-windows");
        }

        return Path.Combine(localAppData, "screenshot-windows", "Captures");
    }
}

public static class CaptureFileNames
{
    public static string Create(DateTimeOffset takenAt, int index = 1)
    {
        var stamp = takenAt.ToString("yyyy-MM-dd HH.mm.ss", System.Globalization.CultureInfo.InvariantCulture);
        var suffix = index <= 1 ? string.Empty : $" {index}";
        return $"screenshot-windows {stamp}{suffix}.png";
    }
}
