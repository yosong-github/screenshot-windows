using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pegline.App.Tray;
using Pegline.App.ViewModels;
using Pegline.App.Views;
using Pegline.Core.Abstractions;
using Pegline.Core.Domain;
using Pegline.Platform.Capture;
using Pegline.Platform.Clipboard;
using Pegline.Platform.Display;
using Pegline.Platform.Files;
using Pegline.Platform.Hotkeys;
using Pegline.Platform.Settings;
using Pegline.Platform.Win32;

namespace Pegline.App.Session;

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
