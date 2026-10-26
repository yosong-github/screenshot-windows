using System.Windows;
using System.Windows.Input;
using ScreenshotWindows.Core.Domain;

namespace ScreenshotWindows.App.Views;

public partial class ShortcutWindow : Window
{
    private readonly AppSettings _draft;
    private int _recording;

    public ShortcutWindow(AppSettings current)
    {
        InitializeComponent();
        _draft = new AppSettings
        {
            CaptureVirtualKey = current.CaptureVirtualKey,
            CaptureModifiers = current.CaptureModifiers,
            LineHotkeyEnabled = current.LineHotkeyEnabled,
            LineVirtualKey = current.LineVirtualKey,
            LineModifiers = current.LineModifiers,
        };
        Refresh();
    }

    public AppSettings Result => _draft;

    private void BeginCapture(object sender, RoutedEventArgs e)
    {
        _recording = 1;
        Refresh();
    }

    private void BeginLine(object sender, RoutedEventArgs e)
    {
        _recording = 2;
        Refresh();
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (_recording == 0)
        {
            return;
        }

        e.Handled = true;
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key == Key.Escape)
        {
            _recording = 0;
            Refresh();
            return;
        }

        if (key is Key.Back or Key.Delete)
        {
            if (_recording == 2)
            {
                _draft.LineHotkey = null;
            }

            _recording = 0;
            Refresh();
            return;
        }

        if (IsBareModifier(key))
        {
            return;
        }

        var modifiers = HotkeyModifiers.None;
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            modifiers |= HotkeyModifiers.Control;
        }

        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
        {
            modifiers |= HotkeyModifiers.Alt;
        }

        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
        {
            modifiers |= HotkeyModifiers.Shift;
        }

        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Windows))
        {
            modifiers |= HotkeyModifiers.Windows;
        }

        var chord = new HotkeyChord(KeyInterop.VirtualKeyFromKey(key), modifiers);
        if (!chord.IsUsable)
        {
            return;
        }

        if (_recording == 1)
        {
            _draft.CaptureHotkey = chord;
        }
        else
        {
            _draft.LineHotkey = chord;
        }

        _recording = 0;
        Refresh();
    }

    private void Accept(object sender, RoutedEventArgs e)
    {
        if (HotkeyChord.Conflicts(_draft.CaptureHotkey, _draft.LineHotkey))
        {
            MessageBox.Show(
                this,
                "Those shortcuts are the same. Pick two different chords.\n这两个快捷键相同，请换成不同的组合。",
                "screenshot-windows",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        DialogResult = true;
    }

    private void Refresh()
    {
        CaptureLabel.Text = _recording == 1
            ? "Press a shortcut…  请按快捷键…"
            : _draft.CaptureHotkey.ToDisplay();
        if (_recording == 2)
        {
            LineLabel.Text = "Press a shortcut…  请按快捷键…";
        }
        else if (_draft.LineHotkey is { } line)
        {
            LineLabel.Text = line.ToDisplay();
        }
        else
        {
            LineLabel.Text = "Off  关";
        }
    }

    private static bool IsBareModifier(Key key) => key is
        Key.LeftCtrl or Key.RightCtrl
        or Key.LeftAlt or Key.RightAlt
        or Key.LeftShift or Key.RightShift
        or Key.LWin or Key.RWin
        or Key.System;
}
