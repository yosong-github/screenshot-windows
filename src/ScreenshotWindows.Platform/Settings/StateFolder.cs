namespace ScreenshotWindows.Platform.Settings;

/// <summary>
/// %LOCALAPPDATA%\screenshot-windows. On first launch, copy the previous folder when the new one is absent.
/// </summary>
public static class StateFolder
{
    public const string Name = "screenshot-windows";

    public static void CopyLegacyIfMissing(string? localAppData = null)
    {
        localAppData ??= Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrEmpty(localAppData))
        {
            return;
        }

        var destination = Path.Combine(localAppData, Name);
        if (Directory.Exists(destination))
        {
            return;
        }

        var source = Path.Combine(localAppData, "Pegline");
        if (!Directory.Exists(source))
        {
            return;
        }

        var staging = destination + ".incoming";
        try
        {
            if (Directory.Exists(staging))
            {
                Directory.Delete(staging, recursive: true);
            }

            CopyTree(source, staging);
            Directory.Move(staging, destination);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            try
            {
                if (Directory.Exists(staging))
                {
                    Directory.Delete(staging, recursive: true);
                }
            }
            catch (Exception cleanup) when (cleanup is IOException or UnauthorizedAccessException)
            {
            }
        }
    }

    private static void CopyTree(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var file in Directory.EnumerateFiles(source))
        {
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), overwrite: false);
        }

        foreach (var directory in Directory.EnumerateDirectories(source))
        {
            CopyTree(directory, Path.Combine(destination, Path.GetFileName(directory)));
        }
    }
}
