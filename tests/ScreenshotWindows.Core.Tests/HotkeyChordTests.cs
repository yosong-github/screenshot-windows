using ScreenshotWindows.Core.Domain;

namespace ScreenshotWindows.Core.Tests;

public class HotkeyChordTests
{
    [Fact]
    public void CaptureDefault_IsCtrlAltS_AndNotTheQqChord()
    {
        var chord = HotkeyChord.CaptureDefault;

        Assert.Equal(HotkeyChord.VkS, chord.VirtualKey);
        Assert.Equal(HotkeyModifiers.Control | HotkeyModifiers.Alt, chord.Modifiers);
        Assert.Equal("Ctrl+Alt+S", chord.ToDisplay());
        Assert.NotEqual(HotkeyChord.VkA, chord.VirtualKey);
    }

    [Fact]
    public void LineDefault_IsCtrlAltL_AndDoesNotClashWithCapture()
    {
        Assert.Equal("Ctrl+Alt+L", HotkeyChord.LineDefault.ToDisplay());
        Assert.False(HotkeyChord.Conflicts(HotkeyChord.CaptureDefault, HotkeyChord.LineDefault));
        Assert.True(HotkeyChord.Conflicts(HotkeyChord.CaptureDefault, HotkeyChord.CaptureDefault));
    }

    [Fact]
    public void ModifierFlags_MatchRegisterHotKey()
    {
        Assert.Equal(1, (int)HotkeyModifiers.Alt);
        Assert.Equal(2, (int)HotkeyModifiers.Control);
        Assert.Equal(4, (int)HotkeyModifiers.Shift);
        Assert.Equal(8, (int)HotkeyModifiers.Windows);
    }

    [Theory]
    [InlineData(0x41, HotkeyModifiers.None)]
    [InlineData(0x53, HotkeyModifiers.Shift)]
    [InlineData(0x11, HotkeyModifiers.Control | HotkeyModifiers.Alt)]
    public void RejectsBareKeysAndModifierKeys(int virtualKey, HotkeyModifiers modifiers)
    {
        Assert.False(new HotkeyChord(virtualKey, modifiers).IsUsable);
    }

    [Fact]
    public void UnusableCaptureChord_FallsBackToDefault()
    {
        var chord = SettingsRules.CaptureOrDefault(virtualKey: 0x11, modifiers: (int)HotkeyModifiers.Control);

        Assert.Equal(HotkeyChord.CaptureDefault, chord);
    }

    [Fact]
    public void DisabledLineHotkey_IsNone()
    {
        Assert.Null(SettingsRules.LineOrNone(enabled: false, HotkeyChord.VkL, (int)HotkeyModifiers.Control));
    }
}
