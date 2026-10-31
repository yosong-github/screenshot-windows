namespace ScreenshotWindows.Core.Domain;

/// <summary>
/// The HKCU Run value screenshot-windows writes for login startup.
/// The command is the quoted executable plus <see cref="Argument"/>.
/// </summary>
public static class AutostartCommand
{
    public const string ValueName = "screenshot-windows";

    public const string Argument = "--autostart";

    public const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public static string Format(string exePath)
    {
        if (string.IsNullOrWhiteSpace(exePath))
        {
            throw new ArgumentException("The executable path is empty.", nameof(exePath));
        }

        return $"\"{exePath.Replace("\"", "\"\"", StringComparison.Ordinal)}\" {Argument}";
    }

    public static bool IsQuietLaunch(IReadOnlyList<string> args)
    {
        foreach (var arg in args)
        {
            if (string.Equals(arg, Argument, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>A non-empty Run value means login startup is on, even when the path is stale.</summary>
    public static bool IsEnabled(string? runValue) => !string.IsNullOrWhiteSpace(runValue);

    /// <summary>
    /// Enabled, but the value does not launch this executable with <see cref="Argument"/>.
    /// </summary>
    public static bool NeedsRepair(string? runValue, string exePath)
    {
        if (!IsEnabled(runValue) || string.IsNullOrWhiteSpace(exePath))
        {
            return false;
        }

        var installed = ExecutableFrom(runValue);
        if (!SamePath(installed, exePath))
        {
            return true;
        }

        return !HasQuietArgument(runValue);
    }

    public static string? ExecutableFrom(string? command)
    {
        return TryReadExecutable(command, out var executable, out _) ? executable : null;
    }

    public static bool HasQuietArgument(string? command)
    {
        if (!TryReadExecutable(command, out _, out var remainder))
        {
            return false;
        }

        foreach (var token in remainder.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries))
        {
            var cleaned = token.Trim('"');
            if (string.Equals(cleaned, Argument, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public static bool SamePath(string? left, string right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
        {
            return false;
        }

        return string.Equals(Normalize(left), Normalize(right), StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryReadExecutable(string? command, out string executable, out string remainder)
    {
        executable = "";
        remainder = "";
        if (string.IsNullOrWhiteSpace(command))
        {
            return false;
        }

        var text = command.Trim();
        if (text[0] == '"')
        {
            var builder = new System.Text.StringBuilder();
            var index = 1;
            while (index < text.Length)
            {
                if (text[index] == '"')
                {
                    if (index + 1 < text.Length && text[index + 1] == '"')
                    {
                        builder.Append('"');
                        index += 2;
                        continue;
                    }

                    index++;
                    break;
                }

                builder.Append(text[index]);
                index++;
            }

            executable = builder.ToString();
            remainder = index < text.Length ? text[index..] : "";
            return executable.Length > 0;
        }

        var space = text.IndexOfAny([' ', '\t']);
        if (space < 0)
        {
            executable = text;
            return true;
        }

        executable = text[..space];
        remainder = text[space..];
        return executable.Length > 0;
    }

    private static string Normalize(string path) => path.Replace('/', '\\').Trim().TrimEnd('\\');
}
