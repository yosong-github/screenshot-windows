using System.Text.Json.Serialization;

namespace Pegline.Core.Domain;

public sealed class StoredShot
{
    public string Path { get; set; } = "";

    public int Width { get; set; }

    public int Height { get; set; }

    public double Tilt { get; set; }

    public bool AccentKnown { get; set; }

    public int? AccentRgb { get; set; }
}

public static class SettingsRules
{
    public static HotkeyChord CaptureOrDefault(int virtualKey, int modifiers)
    {
        var chord = new HotkeyChord(virtualKey, (HotkeyModifiers)modifiers);
        return chord.IsUsable ? chord : HotkeyChord.CaptureDefault;
    }

    public static HotkeyChord? LineOrNone(bool enabled, int virtualKey, int modifiers)
    {
        if (!enabled)
        {
            return null;
        }

        var chord = new HotkeyChord(virtualKey, (HotkeyModifiers)modifiers);
        return chord.IsUsable ? chord : HotkeyChord.LineDefault;
    }
}

public sealed class AppSettings
{
    public int CaptureVirtualKey { get; set; } = HotkeyChord.VkS;

    public int CaptureModifiers { get; set; } = (int)(HotkeyModifiers.Control | HotkeyModifiers.Alt);

    public bool LineHotkeyEnabled { get; set; } = true;

    public int LineVirtualKey { get; set; } = HotkeyChord.VkL;

    public int LineModifiers { get; set; } = (int)(HotkeyModifiers.Control | HotkeyModifiers.Alt);

    public List<StoredShot> Hanging { get; set; } = new();

    [JsonIgnore]
    public HotkeyChord CaptureHotkey
    {
        get => SettingsRules.CaptureOrDefault(CaptureVirtualKey, CaptureModifiers);
        set
        {
            CaptureVirtualKey = value.VirtualKey;
            CaptureModifiers = (int)value.Modifiers;
        }
    }

    [JsonIgnore]
    public HotkeyChord? LineHotkey
    {
        get => SettingsRules.LineOrNone(LineHotkeyEnabled, LineVirtualKey, LineModifiers);
        set
        {
            if (value is null)
            {
                LineHotkeyEnabled = false;
                return;
            }

            LineHotkeyEnabled = true;
            LineVirtualKey = value.Value.VirtualKey;
            LineModifiers = (int)value.Value.Modifiers;
        }
    }
}
