using ScreenshotWindows.Core.Domain;

namespace ScreenshotWindows.Core.Tests;

public class AutostartTests
{
    [Fact]
    public void RunValue_NamesTheExecutableAndTheQuietArgument()
    {
        Assert.Equal("screenshot-windows", AutostartCommand.ValueName);
        Assert.Equal("--autostart", AutostartCommand.Argument);
        Assert.Equal(@"Software\Microsoft\Windows\CurrentVersion\Run", AutostartCommand.RunKeyPath);
    }

    [Fact]
    public void Format_QuotesThePath_AndAppendsAutostart()
    {
        var path = @"C:\Users\Ada\AppData\Local\Programs\screenshot-windows\screenshot-windows.exe";
        var command = AutostartCommand.Format(path);

        Assert.Equal($"\"{path}\" --autostart", command);
        Assert.Equal(path, AutostartCommand.ExecutableFrom(command));
        Assert.True(AutostartCommand.HasQuietArgument(command));
        Assert.False(AutostartCommand.NeedsRepair(command, path));
    }

    [Fact]
    public void Format_EscapesQuotesInsideThePath()
    {
        var path = @"C:\a""b\screenshot-windows.exe";
        var command = AutostartCommand.Format(path);

        Assert.Equal("\"C:\\a\"\"b\\screenshot-windows.exe\" --autostart", command);
        Assert.Equal(path, AutostartCommand.ExecutableFrom(command));
        Assert.False(AutostartCommand.NeedsRepair(command, path));
    }

    [Fact]
    public void Format_RejectsAnEmptyPath()
    {
        Assert.Throws<ArgumentException>(() => AutostartCommand.Format("  "));
    }

    [Theory]
    [InlineData("--autostart", true)]
    [InlineData("--Autostart", true)]
    [InlineData("--start", false)]
    public void QuietLaunch_MatchesTheAutostartArgument(string arg, bool quiet)
    {
        Assert.Equal(quiet, AutostartCommand.IsQuietLaunch([arg]));
        Assert.False(AutostartCommand.IsQuietLaunch([]));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyRunValue_IsDisabled(string? value)
    {
        Assert.False(AutostartCommand.IsEnabled(value));
        Assert.False(AutostartCommand.NeedsRepair(value, @"C:\app\screenshot-windows.exe"));
    }

    [Fact]
    public void MovedExecutable_NeedsRepair_AndAMatchingPathDoesNot()
    {
        var current = @"C:\Users\Ada\AppData\Local\Programs\screenshot-windows\screenshot-windows.exe";
        var stale = "\"D:\\old\\screenshot-windows.exe\" --autostart";

        Assert.True(AutostartCommand.IsEnabled(stale));
        Assert.True(AutostartCommand.NeedsRepair(stale, current));
        Assert.False(AutostartCommand.NeedsRepair(
            $"\"{current}\" --AUTOSTART",
            current.Replace('\\', '/')));
    }

    [Fact]
    public void MissingQuietArgument_NeedsRepair()
    {
        var path = @"C:\app\screenshot-windows.exe";
        Assert.True(AutostartCommand.NeedsRepair($"\"{path}\"", path));
    }

    [Fact]
    public void Switch_EnableDisableAndRepair_UseTheStore()
    {
        var store = new FakeStore();
        var path = @"C:\Programs\screenshot-windows\screenshot-windows.exe";
        var autostart = new LoginAutostart(store, () => path);

        Assert.False(autostart.IsEnabled);
        autostart.Repair();
        Assert.Null(store.Value);
        Assert.Equal(0, store.Writes);

        autostart.Enable();
        Assert.True(autostart.IsEnabled);
        Assert.Equal(AutostartCommand.Format(path), store.Value);

        store.Value = "\"D:\\moved\\screenshot-windows.exe\" --autostart";
        autostart.Repair();
        Assert.Equal(AutostartCommand.Format(path), store.Value);
        Assert.True(autostart.IsEnabled);

        var writes = store.Writes;
        autostart.Repair();
        Assert.Equal(writes, store.Writes);

        autostart.Disable();
        Assert.False(autostart.IsEnabled);
        Assert.Null(store.Value);
    }

    [Fact]
    public void Switch_RejectsAnEmptyExecutable()
    {
        var autostart = new LoginAutostart(new FakeStore(), () => "");
        Assert.Throws<ArgumentException>(() => autostart.Enable());
    }

    private sealed class FakeStore : IAutostartStore
    {
        public string? Value { get; set; }

        public int Writes { get; private set; }

        public string? Read() => Value;

        public void Write(string command)
        {
            Writes++;
            Value = command;
        }

        public void Delete() => Value = null;
    }
}
