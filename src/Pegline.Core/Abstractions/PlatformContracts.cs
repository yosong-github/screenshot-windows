using Pegline.Core.Domain;

namespace Pegline.Core.Abstractions;

public interface ISettingsStore
{
    AppSettings Load();

    void Save(AppSettings settings);
}

public interface ICaptureStore
{
    string Folder { get; }

    string Save(byte[] png, DateTimeOffset takenAt);
}

public interface IDisplayEnvironment
{
    IReadOnlyList<MonitorBounds> QueryMonitors();

    MousePoint QueryCursor();
}

/// <summary>Freezes each monitor in physical pixels and crops a region to PNG.</summary>
public interface IScreenCapture
{
    IReadOnlyList<FrozenMonitor> Freeze();

    byte[] CropToPng(FrozenMonitor monitor, PixelRect region);
}

public interface IGlobalHotkey
{
    bool TryBind(int id, HotkeyChord chord);

    void Unbind(int id);

    event EventHandler<int>? Pressed;
}

public interface IClipboardImageWriter
{
    void SetPngFile(string path);
}
