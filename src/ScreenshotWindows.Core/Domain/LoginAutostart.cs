namespace ScreenshotWindows.Core.Domain;

/// <summary>Reads and writes the login Run value. The store is the registry on Windows and a fake in tests.</summary>
public interface IAutostartStore
{
    string? Read();

    void Write(string command);

    void Delete();
}

/// <summary>
/// Login startup for this executable. <see cref="Repair"/> rewrites a Run value that still
/// points at an older copy of the program.
/// </summary>
public sealed class LoginAutostart
{
    private readonly IAutostartStore _store;
    private readonly Func<string> _executable;

    public LoginAutostart(IAutostartStore store, Func<string> executable)
    {
        _store = store;
        _executable = executable;
    }

    public bool IsEnabled => AutostartCommand.IsEnabled(_store.Read());

    public void Enable() => _store.Write(AutostartCommand.Format(Executable()));

    public void Disable() => _store.Delete();

    public void Repair()
    {
        var path = Executable();
        if (!AutostartCommand.NeedsRepair(_store.Read(), path))
        {
            return;
        }

        _store.Write(AutostartCommand.Format(path));
    }

    private string Executable()
    {
        var path = _executable();
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("The executable path is empty.");
        }

        return path;
    }
}
