using System.Windows;
using System.Windows.Threading;
using Pegline.App.ViewModels;
using Pegline.App.Views;
using Pegline.Core.Abstractions;
using Pegline.Core.Domain;

namespace Pegline.App.Session;

public sealed class LineSession
{
    private readonly ISettingsStore _settingsStore;
    private readonly ICaptureStore _captures;
    private readonly IScreenCapture _screen;
    private readonly IDisplayEnvironment _displays;
    private readonly IGlobalHotkey _hotkeys;
    private readonly IClipboardImageWriter _clipboard;
    private readonly Clothesline _line;
    private readonly RevealEngine _reveal;
    private readonly LineViewModel _model;
    private readonly LineWindow _window;
    private readonly RegionCaptureController _overlay;
    private readonly DispatcherTimer _timer;
    private AppSettings _settings;
    private int _busy;
    private Guid? _copied;
    private DateTimeOffset _copiedUntil;

    public LineSession(
        ISettingsStore settingsStore,
        ICaptureStore captures,
        IScreenCapture screen,
        IDisplayEnvironment displays,
        IGlobalHotkey hotkeys,
        IClipboardImageWriter clipboard,
        Clothesline line,
        RevealEngine reveal,
        LineViewModel model,
        LineWindow window,
        RegionCaptureController overlay)
    {
        _settingsStore = settingsStore;
        _captures = captures;
        _screen = screen;
        _displays = displays;
        _hotkeys = hotkeys;
        _clipboard = clipboard;
        _line = line;
        _reveal = reveal;
        _model = model;
        _window = window;
        _overlay = overlay;
        _settings = settingsStore.Load();
        _model.CaptureShortcut = _settings.CaptureHotkey.ToDisplay();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) };
        _timer.Tick += (_, _) => Tick();
        _hotkeys.Pressed += (_, id) => OnHotkey(id);
        _model.CopyRequested += (_, id) => Copy(id);
        _model.RemoveRequested += (_, id) => Remove(id);
    }

    public bool IsLineVisible => _reveal.IsRevealed;

    public HotkeyChord CaptureShortcut => _settings.CaptureHotkey;

    public HotkeyChord? LineShortcut => _settings.LineHotkey;

    public void Start()
    {
        var restored = new List<HangingItem>();
        foreach (var shot in _settings.Hanging)
        {
            if (string.IsNullOrWhiteSpace(shot.Path) || !File.Exists(shot.Path))
            {
                continue;
            }

            restored.Add(new HangingItem(Guid.NewGuid(), shot.Path, shot.Width, shot.Height, shot.Tilt));
        }

        _line.Restore(restored);
        _model.Replace(_line.Items, null);
        Persist();
        BindHotkeys(announce: true);
        _timer.Start();
        if (_line.Count == 0)
        {
            return;
        }

        var monitor = MonitorUnderCursor() ?? FirstMonitor();
        if (monitor is not { } anchor)
        {
            return;
        }

        _window.Anchor = anchor;
        _reveal.Peek(DateTimeOffset.Now);
        _model.IsRevealed = true;
    }

    public void Capture()
    {
        if (Interlocked.CompareExchange(ref _busy, 1, 0) != 0)
        {
            return;
        }

        _ = CaptureCoreAsync();
    }

    public void ToggleLine()
    {
        if (Volatile.Read(ref _busy) != 0)
        {
            return;
        }

        if (!_reveal.IsRevealed)
        {
            var monitor = MonitorUnderCursor() ?? FirstMonitor();
            if (monitor is { } anchor)
            {
                _window.Anchor = anchor;
            }
        }

        _reveal.Toggle(DateTimeOffset.Now, _line.Count);
        _model.IsRevealed = _reveal.IsRevealed;
    }

    public void ChangeShortcuts()
    {
        _hotkeys.Unbind(HotkeyIds.Capture);
        _hotkeys.Unbind(HotkeyIds.ToggleLine);
        var dialog = new ShortcutWindow(_settings);
        var accepted = dialog.ShowDialog() == true;
        if (accepted)
        {
            _settings.CaptureHotkey = dialog.Result.CaptureHotkey;
            _settings.LineHotkey = dialog.Result.LineHotkey;
            Persist();
        }

        BindHotkeys(announce: true);
    }

    public void Quit()
    {
        _timer.Stop();
        Persist();
        _window.AllowClose();
        _window.Close();
        Application.Current.Shutdown();
    }

    private async Task CaptureCoreAsync()
    {
        try
        {
            _model.IsRevealed = false;
            _window.HideImmediate();
            await Task.Delay(40).ConfigureAwait(true);
            var frames = _screen.Freeze();
            if (frames.Count == 0)
            {
                MessageBox.Show(
                    "Couldn't capture the screen.\n无法截取屏幕。",
                    "Pegline",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var selection = await _overlay.RunAsync(frames).ConfigureAwait(true);
            if (selection is not { } shot || !CaptureCrop.CanEncode(shot.Region))
            {
                return;
            }

            var png = _screen.CropToPng(shot.Frame, shot.Region);
            var path = _captures.Save(png, DateTimeOffset.Now);
            var outcome = _line.Hang(path, shot.Region.Width, shot.Region.Height);
            if (outcome.Item is not { } item)
            {
                return;
            }

            _window.Anchor = shot.Frame.Monitor;
            _reveal.Peek(DateTimeOffset.Now);
            _model.Replace(_line.Items, item.Id);
            _model.IsRevealed = true;
            Persist();
            var image = _model.Find(item.Id)?.Image;
            if (image is not null)
            {
                await _window.FlyAsync(image, shot.ScreenDip, item.Id).ConfigureAwait(true);
            }
            else
            {
                _model.MarkLanded(item.Id);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException)
        {
            MessageBox.Show(
                "Couldn't save the screenshot.\n无法保存截图。\n" + ex.Message,
                "Pegline",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        finally
        {
            Volatile.Write(ref _busy, 0);
            if (_model.IsRevealed != _reveal.IsRevealed)
            {
                _model.IsRevealed = _reveal.IsRevealed;
            }
        }
    }

    private void Tick()
    {
        if (Volatile.Read(ref _busy) != 0)
        {
            return;
        }

        var now = DateTimeOffset.Now;
        if (_copied is { } copied && now >= _copiedUntil)
        {
            _model.ClearCopied(copied);
            _copied = null;
        }

        var cursor = _displays.QueryCursor();
        var under = MonitorUnderCursor();
        var sample = new RevealSample(
            now,
            _line.Count,
            under is { } band && band.InTopBand(cursor.X, cursor.Y),
            _window.IsInZone(cursor));
        var motion = _reveal.Advance(sample);
        if (motion == LineMotion.Show && under is { } shown)
        {
            _window.Anchor = shown;
        }

        if (_model.IsRevealed != _reveal.IsRevealed)
        {
            _model.IsRevealed = _reveal.IsRevealed;
        }

        _window.UpdateClickThrough(cursor);
    }

    private void OnHotkey(int id)
    {
        if (id == HotkeyIds.Capture)
        {
            Capture();
        }
        else if (id == HotkeyIds.ToggleLine)
        {
            ToggleLine();
        }
    }

    private void Copy(Guid id)
    {
        var item = _line.Find(id);
        if (item is null)
        {
            return;
        }

        if (!File.Exists(item.Path))
        {
            Remove(id);
            return;
        }

        try
        {
            _clipboard.SetPngFile(item.Path);
            _model.MarkCopied(id);
            _copied = id;
            _copiedUntil = DateTimeOffset.Now.AddSeconds(1.2);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            MessageBox.Show(
                "Couldn't copy the image.\n无法复制图片。",
                "Pegline",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }

    private void Remove(Guid id)
    {
        if (_line.TakeDown(id) is null)
        {
            return;
        }

        _model.Replace(_line.Items, null);
        Persist();
    }

    private void BindHotkeys(bool announce)
    {
        if (!_hotkeys.TryBind(HotkeyIds.Capture, _settings.CaptureHotkey) && announce)
        {
            MessageBox.Show(
                "Couldn't register " + _settings.CaptureHotkey.ToDisplay() + ". Change it from the tray menu.\n无法注册截图快捷键，可在托盘菜单里修改。",
                "Pegline",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        if (_settings.LineHotkey is { } line)
        {
            _hotkeys.TryBind(HotkeyIds.ToggleLine, line);
        }
        else
        {
            _hotkeys.Unbind(HotkeyIds.ToggleLine);
        }

        _model.CaptureShortcut = _settings.CaptureHotkey.ToDisplay();
    }

    private void Persist()
    {
        _settings.Hanging = _line.Items.Select(item => new StoredShot
        {
            Path = item.Path,
            Width = item.PixelWidth,
            Height = item.PixelHeight,
            Tilt = item.TiltDegrees,
        }).ToList();
        _settingsStore.Save(_settings);
    }

    private MonitorBounds? MonitorUnderCursor()
    {
        var cursor = _displays.QueryCursor();
        foreach (var monitor in _displays.QueryMonitors())
        {
            if (monitor.Contains(cursor.X, cursor.Y))
            {
                return monitor;
            }
        }

        return null;
    }

    private MonitorBounds? FirstMonitor()
    {
        foreach (var monitor in _displays.QueryMonitors())
        {
            return monitor;
        }

        return null;
    }
}
