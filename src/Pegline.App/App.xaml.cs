using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pegline.App.Session;
using Pegline.App.Tray;

namespace Pegline.App;

public partial class App : Application
{
    private IHost? _host;
    private Mutex? _mutex;
    private bool _ownsMutex;

    static App()
    {
        // Before WPF creates a dispatcher window, so BitBlt sees physical pixels.
        System.Windows.Forms.Application.SetHighDpiMode(System.Windows.Forms.HighDpiMode.PerMonitorV2);
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        _mutex = new Mutex(true, @"Local\Pegline", out _ownsMutex);
        if (!_ownsMutex)
        {
            _mutex.Dispose();
            _mutex = null;
            Shutdown();
            return;
        }

        _host = AppServices.Build();
        var session = _host.Services.GetRequiredService<LineSession>();
        var tray = _host.Services.GetRequiredService<TrayIconController>();
        tray.Bind(session);
        session.Start();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _host?.Dispose();
        if (_ownsMutex && _mutex is not null)
        {
            _mutex.ReleaseMutex();
            _mutex.Dispose();
        }

        base.OnExit(e);
    }
}
