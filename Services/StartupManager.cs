using Microsoft.Win32;
using WindowDeck.Localization;

namespace WindowDeck.Services;

internal sealed class StartupManager
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string StartupApprovedRunKeyPath =
        @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";
    private const string ValueName = "WindowDeck";
    private const byte EnabledStartupApprovalState = 0x02;

    private static string StartupCommand => $"\"{Application.ExecutablePath}\"";

    public bool IsEnabled()
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
            if (key?.GetValue(ValueName) is not string value
                || !string.Equals(value, StartupCommand, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            using RegistryKey? approvalKey = Registry.CurrentUser.OpenSubKey(
                StartupApprovedRunKeyPath, false);
            return approvalKey?.GetValue(ValueName) is not byte[] approval
                || (approval.Length > 0 && approval[0] == EnabledStartupApprovalState);
        }
        catch (Exception exception) when (exception is UnauthorizedAccessException
            or IOException
            or System.Security.SecurityException)
        {
            return false;
        }
    }

    public bool TrySetEnabled(bool enabled, out string? errorMessage)
    {
        object? previousRunValue = null;
        object? previousApprovalValue = null;
        bool snapshotCaptured = false;
        try
        {
            using (RegistryKey? existingRunKey = Registry.CurrentUser.OpenSubKey(RunKeyPath, false))
            {
                previousRunValue = existingRunKey?.GetValue(ValueName, null,
                    RegistryValueOptions.DoNotExpandEnvironmentNames);
            }

            using (RegistryKey? existingApprovalKey = Registry.CurrentUser.OpenSubKey(
                StartupApprovedRunKeyPath, false))
            {
                previousApprovalValue = existingApprovalKey?.GetValue(ValueName);
            }
            snapshotCaptured = true;

            using RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKeyPath, true);
            if (enabled)
            {
                key.SetValue(ValueName, StartupCommand, RegistryValueKind.String);
                using RegistryKey? approvalKey = Registry.CurrentUser.OpenSubKey(
                    StartupApprovedRunKeyPath, true);
                approvalKey?.DeleteValue(ValueName, false);
            }
            else
            {
                key.DeleteValue(ValueName, false);
            }

            if (IsEnabled() != enabled)
            {
                throw new IOException("Windows did not apply the requested startup state.");
            }

            errorMessage = null;
            return true;
        }
        catch (Exception exception) when (exception is UnauthorizedAccessException
            or IOException
            or System.Security.SecurityException)
        {
            if (snapshotCaptured)
            {
                TryRestoreValue(RunKeyPath, previousRunValue);
                TryRestoreValue(StartupApprovedRunKeyPath, previousApprovalValue);
            }
            errorMessage = LocalizationService.Format("Message_UpdateStartupFailed", exception.Message);
            return false;
        }
    }

    private static void TryRestoreValue(string keyPath, object? value)
    {
        try
        {
            using RegistryKey key = Registry.CurrentUser.CreateSubKey(keyPath, true);
            if (value is null)
            {
                key.DeleteValue(ValueName, false);
            }
            else
            {
                key.SetValue(ValueName, value);
            }
        }
        catch (Exception exception) when (exception is UnauthorizedAccessException
            or IOException
            or System.Security.SecurityException)
        {
            // Best-effort rollback only; the caller reports the original failure.
        }
    }
}
