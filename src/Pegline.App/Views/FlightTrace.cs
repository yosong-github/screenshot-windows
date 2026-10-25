namespace Pegline.App.Views;

/// <summary>
/// Frame log for the capture flight. Set PEGLINE_FLIGHT_TRACE=1.
/// Lines land in %LOCALAPPDATA%\Pegline\flight-trace.log.
/// </summary>
static class FlightTrace
{
#if DEBUG
    private const bool DebugBuild = true;
#else
    private const bool DebugBuild = false;
#endif

    public static bool Enabled { get; } =
        DebugBuild
        || string.Equals(Environment.GetEnvironmentVariable("PEGLINE_FLIGHT_TRACE"), "1", StringComparison.Ordinal);

    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Pegline",
        "flight-trace.log");

    public static void Write(string line)
    {
        if (!Enabled)
        {
            return;
        }

        System.Diagnostics.Trace.WriteLine(line);
        try
        {
            var directory = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.AppendAllText(FilePath, line + Environment.NewLine);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }
}
