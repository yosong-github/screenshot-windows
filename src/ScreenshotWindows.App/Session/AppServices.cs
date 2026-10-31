using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ScreenshotWindows.App.Tray;
using ScreenshotWindows.App.ViewModels;
using ScreenshotWindows.App.Views;
using ScreenshotWindows.Core.Abstractions;
using ScreenshotWindows.Core.Domain;
using ScreenshotWindows.Platform.Capture;
using ScreenshotWindows.Platform.Clipboard;
using ScreenshotWindows.Platform.Display;
using ScreenshotWindows.Platform.Files;
using ScreenshotWindows.Platform.Hotkeys;
using ScreenshotWindows.Platform.Settings;
using ScreenshotWindows.Platform.Startup;
using ScreenshotWindows.Platform.Win32;

namespace ScreenshotWindows.App.Session;

public static class AppServices
{
    public static IHost Build()
    {
        return Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton<PlatformLoop>();
                services.AddSingleton<IGlobalHotkey, GlobalHotkeyService>();
                services.AddSingleton<IDisplayEnvironment, DisplayEnvironment>();
                services.AddSingleton<IScreenCapture, GdiScreenCapture>();
                services.AddSingleton<IClipboardImageWriter, ClipboardImageWriter>();
                services.AddSingleton<ICaptureStore, CaptureStore>();
                services.AddSingleton<ISettingsStore, JsonSettingsStore>();
                services.AddSingleton<IAutostart, RegistryAutostart>();
                services.AddSingleton<Clothesline>();
                services.AddSingleton<RevealEngine>();
                services.AddSingleton<LineViewModel>();
                services.AddSingleton<LineWindow>();
                services.AddSingleton<RegionCaptureController>();
                services.AddSingleton<LineSession>();
                services.AddSingleton<TrayIconController>();
            })
            .Build();
    }
}
