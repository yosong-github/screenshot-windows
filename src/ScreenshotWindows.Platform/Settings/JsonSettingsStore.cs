using System.Text.Json;
using ScreenshotWindows.Core.Abstractions;
using ScreenshotWindows.Core.Domain;

namespace ScreenshotWindows.Platform.Settings;

public sealed class JsonSettingsStore : ISettingsStore
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    private readonly string _path;

    public JsonSettingsStore()
    {
        var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        StateFolder.CopyLegacyIfMissing(root);
        _path = Path.Combine(root, StateFolder.Name, "settings.json");
    }

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(_path))
            {
                return new AppSettings();
            }

            return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_path), Options) ?? new AppSettings();
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        var json = JsonSerializer.Serialize(settings, Options);
        var temporary = _path + ".tmp";
        File.WriteAllText(temporary, json);
        File.Move(temporary, _path, overwrite: true);
    }
}
