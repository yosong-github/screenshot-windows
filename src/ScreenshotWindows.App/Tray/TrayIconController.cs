using System.Drawing;
using System.Windows.Forms;
using ScreenshotWindows.App.Session;
using ScreenshotWindows.Platform.Startup;

namespace ScreenshotWindows.App.Tray;

public sealed class TrayIconController : IDisposable
{
    private readonly NotifyIcon _icon;
    private readonly MemoryStream? _iconStream;
    private readonly IAutostart _autostart;
    private LineSession? _session;
    private bool _disposed;

    public TrayIconController(IAutostart autostart)
    {
        _autostart = autostart;
        _icon = new NotifyIcon { Text = "screenshot-windows", Visible = false };
        _iconStream = LoadIcon();
        _icon.Icon ??= new Icon(SystemIcons.Application, 32, 32);
        _icon.ContextMenuStrip = new ContextMenuStrip();
        _icon.ContextMenuStrip.Opening += (_, _) => Rebuild();
    }

    public void Bind(LineSession session)
    {
        _session = session;
        _icon.Visible = true;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _icon.Visible = false;
        _icon.Dispose();
        _iconStream?.Dispose();
    }

    private void Rebuild()
    {
        if (_icon.ContextMenuStrip is not { } menu)
        {
            return;
        }

        var session = _session;
        if (session is null)
        {
            return;
        }

        menu.Items.Clear();
        var line = session.IsLineVisible ? "Hide line  隐藏" : "Show line  显示";
        var lineShortcut = session.LineShortcut?.ToDisplay();
        menu.Items.Add(Item(lineShortcut is null ? line : line + "    " + lineShortcut, () => session.ToggleLine()));
        menu.Items.Add(Item("Take screenshot  截图    " + session.CaptureShortcut.ToDisplay(), () => session.Capture()));
        menu.Items.Add(Item("Shortcuts…  快捷键…", () => session.ChangeShortcuts()));
        menu.Items.Add(LoginItem());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(Item("Quit  退出", () => session.Quit()));
    }

    private MemoryStream? LoadIcon()
    {
        try
        {
            var resource = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/Assets/screenshot-windows.ico"));
            if (resource?.Stream is null)
            {
                return null;
            }

            var stream = new MemoryStream();
            resource.Stream.CopyTo(stream);
            resource.Stream.Dispose();
            stream.Position = 0;
            _icon.Icon = new Icon(stream);
            return stream;
        }
        catch (Exception ex) when (ex is IOException or ArgumentException or InvalidOperationException)
        {
            return null;
        }
    }

    private ToolStripMenuItem LoginItem()
    {
        var item = new ToolStripMenuItem("开机自启")
        {
            Checked = _autostart.IsEnabled,
        };
        item.Click += (_, _) => ToggleAutostart();
        return item;
    }

    private void ToggleAutostart()
    {
        try
        {
            if (_autostart.IsEnabled)
            {
                _autostart.Disable();
            }
            else
            {
                _autostart.Enable();
            }
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException or IOException)
        {
            System.Windows.MessageBox.Show(
                "Couldn't update startup.\n无法修改开机自启。\n" + ex.Message,
                "screenshot-windows",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }
    }

    private static ToolStripMenuItem Item(string text, Action action)
    {
        var item = new ToolStripMenuItem(text);
        item.Click += (_, _) => action();
        return item;
    }
}
