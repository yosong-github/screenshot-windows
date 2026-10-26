using System.Text;
using System.Windows.Media;
using ScreenshotWindows.Platform.Settings;

namespace ScreenshotWindows.App.Views;

/// <summary>
/// Frame log for the capture flight. Set SCREENSHOT_WINDOWS_FLIGHT_TRACE=1.
/// Lines land in %LOCALAPPDATA%\screenshot-windows\flight-trace.log.
/// Frames are buffered and flushed once, when the flight ends.
/// </summary>
static class FlightTrace
{
#if DEBUG
    private const bool DebugBuild = true;
#else
    private const bool DebugBuild = false;
#endif

    private const double SlowMilliseconds = 20;

    public static bool Enabled { get; } =
        DebugBuild
        || string.Equals(Environment.GetEnvironmentVariable("SCREENSHOT_WINDOWS_FLIGHT_TRACE"), "1", StringComparison.Ordinal);

    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        StateFolder.Name,
        "flight-trace.log");

    private static readonly StringBuilder Buffer = new(8 * 1024);

    private static bool _folderReady;
    private static bool _tierNoted;
    private static int _tier = -1;
    private static int _frames;
    private static int _timed;
    private static int _slow;
    private static double _sum;
    private static double _max;

    public static void NoteRenderTier()
    {
        if (!Enabled)
        {
            return;
        }

        var tier = RenderCapability.Tier >> 16;
        if (_tierNoted && (tier == _tier || tier == 0))
        {
            return;
        }

        _tier = tier;
        _tierNoted = true;
        FlushText(FormattableString.Invariant($"render-tier={tier}{Environment.NewLine}"));
    }

    public static void BeginFlight(string header)
    {
        if (!Enabled)
        {
            return;
        }

        _frames = 0;
        _timed = 0;
        _slow = 0;
        _sum = 0;
        _max = 0;
        Buffer.Clear();
        Buffer.Append(header).AppendLine();
    }

    public static void Frame(double dt, string detail)
    {
        if (!Enabled)
        {
            return;
        }

        _frames++;
        var slow = false;
        if (dt > 0)
        {
            _timed++;
            _sum += dt;
            if (dt > _max)
            {
                _max = dt;
            }

            if (dt > SlowMilliseconds)
            {
                _slow++;
                slow = true;
            }
        }

        if (slow)
        {
            Buffer.Append("SLOW ");
        }

        Buffer.Append(detail).AppendLine();
    }

    public static void EndFlight()
    {
        if (!Enabled || Buffer.Length == 0 && _frames == 0)
        {
            return;
        }

        var average = _timed > 0 ? _sum / _timed : 0;
        Buffer.Append(FormattableString.Invariant(
            $"summary frames={_frames} avg={average:0.0} max={_max:0.0} slow={_slow}"));
        Buffer.AppendLine();
        var text = Buffer.ToString();
        Buffer.Clear();
        _frames = 0;
        _timed = 0;
        _slow = 0;
        _sum = 0;
        _max = 0;
        FlushText(text);
    }

    private static void FlushText(string text)
    {
        if (text.Length == 0)
        {
            return;
        }

        try
        {
            if (!_folderReady)
            {
                StateFolder.CopyLegacyIfMissing();
                var directory = Path.GetDirectoryName(FilePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                _folderReady = true;
            }

            File.AppendAllText(FilePath, text);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }
}
