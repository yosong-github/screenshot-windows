namespace ScreenshotWindows.Platform.Startup;

/// <summary>
/// Login startup via HKCU\Software\Microsoft\Windows\CurrentVersion\Run.
/// The value name is screenshot-windows. No administrator rights.
/// </summary>
public interface IAutostart
{
    bool IsEnabled { get; }

    void Enable();

    void Disable();

    /// <summary>When startup is on but the Run value points at another copy, rewrite it to this executable.</summary>
    void Repair();
}
