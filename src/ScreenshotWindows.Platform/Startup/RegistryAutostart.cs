using Microsoft.Win32;
using ScreenshotWindows.Core.Domain;

namespace ScreenshotWindows.Platform.Startup;

public sealed class RegistryAutostart : IAutostart
{
    private readonly LoginAutostart _autostart;

    public RegistryAutostart()
        : this(new RegistryRunValue(), CurrentExecutable)
    {
    }

    internal RegistryAutostart(IAutostartStore store, Func<string> executable)
    {
        _autostart = new LoginAutostart(store, executable);
    }

    public bool IsEnabled => _autostart.IsEnabled;

    public void Enable() => _autostart.Enable();

    public void Disable() => _autostart.Disable();

    public void Repair() => _autostart.Repair();

    private static string CurrentExecutable()
    {
        if (!string.IsNullOrWhiteSpace(Environment.ProcessPath))
        {
            return Environment.ProcessPath;
        }

        var module = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
        if (!string.IsNullOrWhiteSpace(module))
        {
            return module;
        }

        return Path.Combine(AppContext.BaseDirectory, "screenshot-windows.exe");
    }

    private sealed class RegistryRunValue : IAutostartStore
    {
        public string? Read()
        {
            using var key = Registry.CurrentUser.OpenSubKey(AutostartCommand.RunKeyPath, writable: false);
            return key?.GetValue(AutostartCommand.ValueName) as string;
        }

        public void Write(string command)
        {
            using var key = Registry.CurrentUser.CreateSubKey(AutostartCommand.RunKeyPath, writable: true)
                ?? throw new InvalidOperationException("Couldn't open the Run key.");
            key.SetValue(AutostartCommand.ValueName, command, RegistryValueKind.String);
        }

        public void Delete()
        {
            using var key = Registry.CurrentUser.OpenSubKey(AutostartCommand.RunKeyPath, writable: true);
            key?.DeleteValue(AutostartCommand.ValueName, throwOnMissingValue: false);
        }
    }
}
