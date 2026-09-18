using Microsoft.Win32;

namespace WindowDeck.Services;

internal sealed class StartupManager
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "WindowDeck";

    private static string StartupCommand => $"\"{Application.ExecutablePath}\"";

    public bool IsEnabled()
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
        return key?.GetValue(ValueName) is string value
            && string.Equals(value, StartupCommand, StringComparison.OrdinalIgnoreCase);
    }

    public bool TrySetEnabled(bool enabled, out string? errorMessage)
    {
        try
        {
            using RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKeyPath, true);
            if (enabled)
            {
                key.SetValue(ValueName, StartupCommand, RegistryValueKind.String);
            }
            else
            {
                key.DeleteValue(ValueName, false);
            }

            errorMessage = null;
            return true;
        }
        catch (Exception exception) when (exception is UnauthorizedAccessException
            or IOException
            or System.Security.SecurityException)
        {
            errorMessage = $"WindowDeck could not update Windows startup. {exception.Message}";
            return false;
        }
    }
}
