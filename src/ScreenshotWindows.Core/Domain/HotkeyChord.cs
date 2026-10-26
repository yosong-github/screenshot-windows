namespace ScreenshotWindows.Core.Domain;

/// <summary>
/// A global shortcut. Modifier values match the Win32 <c>RegisterHotKey</c> flags
/// so the platform can pass them through without a second mapping table.
/// </summary>
[Flags]
public enum HotkeyModifiers
{
    None = 0,
    Alt = 1,
    Control = 2,
    Shift = 4,
    Windows = 8,
}

/// <summary>
/// Capture defaults to Ctrl+Alt+S. Ctrl+Alt+A is the screenshot chord used by QQ and WeChat,
/// so it is intentionally not the default.
/// The line defaults to Ctrl+Alt+L (show or hide).
/// </summary>
public readonly record struct HotkeyChord(int VirtualKey, HotkeyModifiers Modifiers)
{
    public const int VkA = 0x41;
    public const int VkL = 0x4C;
    public const int VkS = 0x53;

    public static HotkeyChord CaptureDefault { get; } = new(VkS, HotkeyModifiers.Control | HotkeyModifiers.Alt);

    public static HotkeyChord LineDefault { get; } = new(VkL, HotkeyModifiers.Control | HotkeyModifiers.Alt);

    public bool HasPrimaryModifier =>
        Modifiers.HasFlag(HotkeyModifiers.Control)
        || Modifiers.HasFlag(HotkeyModifiers.Alt)
        || Modifiers.HasFlag(HotkeyModifiers.Windows);

    public bool IsUsable => HasPrimaryModifier && !IsModifierKey(VirtualKey);

    public string ToDisplay()
    {
        var parts = new List<string>(5);
        if (Modifiers.HasFlag(HotkeyModifiers.Control))
        {
            parts.Add("Ctrl");
        }

        if (Modifiers.HasFlag(HotkeyModifiers.Alt))
        {
            parts.Add("Alt");
        }

        if (Modifiers.HasFlag(HotkeyModifiers.Shift))
        {
            parts.Add("Shift");
        }

        if (Modifiers.HasFlag(HotkeyModifiers.Windows))
        {
            parts.Add("Win");
        }

        parts.Add(VirtualKeyNames.Name(VirtualKey));
        return string.Join('+', parts);
    }

    public static bool IsModifierKey(int virtualKey) => virtualKey is
        0x10 or 0x11 or 0x12 or 0x5B or 0x5C
        or (>= 0xA0 and <= 0xA5);

    public static bool Conflicts(HotkeyChord? left, HotkeyChord? right) =>
        left is { } a && right is { } b && a.Equals(b);
}

public static class HotkeyIds
{
    public const int Capture = 1;
    public const int ToggleLine = 2;
}

public static class VirtualKeyNames
{
    public static string Name(int virtualKey) => virtualKey switch
    {
        0x20 => "Space",
        >= 0x30 and <= 0x39 => ((char)virtualKey).ToString(),
        >= 0x41 and <= 0x5A => ((char)virtualKey).ToString(),
        >= 0x70 and <= 0x87 => "F" + (virtualKey - 0x6F),
        _ => "VK" + virtualKey.ToString("X2"),
    };
}
