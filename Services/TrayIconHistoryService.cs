using System.ComponentModel;
using System.Diagnostics;
using System.Security;
using Microsoft.Win32;

namespace WindowDeck.Services;

internal sealed record TrayIconHistoryEntry(string KeyName, string ExecutablePath);

internal static class TrayIconHistoryService
{
    private const string HistoryRegistryPath = @"Control Panel\NotifyIconSettings";
    private const string ExecutableValueName = "ExecutablePath";

    public static IReadOnlyList<TrayIconHistoryEntry> GetOldEntries()
    {
        using RegistryKey? history = Registry.CurrentUser.OpenSubKey(HistoryRegistryPath);
        if (history is null)
        {
            return [];
        }

        string currentExecutablePath = Path.GetFullPath(Application.ExecutablePath);
        List<TrayIconHistoryEntry> entries = [];

        foreach (string keyName in history.GetSubKeyNames())
        {
            using RegistryKey? key = history.OpenSubKey(keyName);
            string? executablePath = key?.GetValue(ExecutableValueName) as string;
            string? normalizedPath = NormalizeWindowDeckPath(executablePath);
            if (normalizedPath is null
                || string.Equals(normalizedPath, currentExecutablePath, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            entries.Add(new TrayIconHistoryEntry(keyName, normalizedPath));
        }

        return entries.OrderBy(entry => entry.ExecutablePath, StringComparer.OrdinalIgnoreCase)
            .ThenBy(entry => entry.KeyName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static string RemoveOldEntries(IReadOnlyList<TrayIconHistoryEntry> entries)
    {
        if (entries.Count == 0)
        {
            throw new InvalidOperationException("There are no old WindowDeck tray entries to remove.");
        }

        // Recheck the entries before writing. The currently running executable is
        // never a deletion target, even if Windows changes its history meanwhile.
        Dictionary<string, string> currentlyEligible = GetOldEntries()
            .ToDictionary(entry => entry.KeyName, entry => entry.ExecutablePath,
                StringComparer.OrdinalIgnoreCase);
        if (entries.Any(entry => !currentlyEligible.TryGetValue(entry.KeyName, out string? path)
            || !string.Equals(path, entry.ExecutablePath, StringComparison.OrdinalIgnoreCase))
            || entries.Select(entry => entry.KeyName).Distinct(StringComparer.OrdinalIgnoreCase).Count() != entries.Count)
        {
            throw new IOException("Windows tray icon history changed. Reopen the cleanup dialog and try again.");
        }

        string backupDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WindowDeck", "TrayIconBackups", DateTime.Now.ToString("yyyyMMdd-HHmmss-fff"));
        Directory.CreateDirectory(backupDirectory);

        // Export ALL target keys successfully before deleting the first key.
        for (int index = 0; index < entries.Count; index++)
        {
            string registryKey = $@"HKEY_CURRENT_USER\{HistoryRegistryPath}\{entries[index].KeyName}";
            string backupPath = Path.Combine(backupDirectory, $"{index + 1:D4}.reg");
            ExportRegistryKey(registryKey, backupPath);
        }

        using RegistryKey? writableHistory = Registry.CurrentUser.OpenSubKey(HistoryRegistryPath, writable: true);
        if (writableHistory is null)
        {
            throw new IOException("Cannot open Windows tray icon history for writing.");
        }

        int removed = 0;
        try
        {
            foreach (TrayIconHistoryEntry entry in entries)
            {
                // Do not remove a changed key between preview and deletion.
                using (RegistryKey? key = writableHistory.OpenSubKey(entry.KeyName))
                {
                    string? path = NormalizeWindowDeckPath(key?.GetValue(ExecutableValueName) as string);
                    if (!string.Equals(path, entry.ExecutablePath, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new IOException("An icon entry changed during cleanup.");
                    }
                }

                writableHistory.DeleteSubKeyTree(entry.KeyName, throwOnMissingSubKey: true);
                removed++;
            }
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or SecurityException
            or ArgumentException)
        {
            throw new IOException(
                $"Removed {removed} of {entries.Count} old entries. Registry backups: {backupDirectory}. {error.Message}",
                error);
        }

        return backupDirectory;
    }

    private static string? NormalizeWindowDeckPath(string? rawPath)
    {
        if (string.IsNullOrWhiteSpace(rawPath))
        {
            return null;
        }

        string candidate = rawPath.Trim().Trim('"');
        if (!Path.IsPathFullyQualified(candidate)
            || !string.Equals(Path.GetFileName(candidate), "WindowDeck.exe", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        try
        {
            return Path.GetFullPath(candidate);
        }
        catch (Exception error) when (error is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return null;
        }
    }

    private static void ExportRegistryKey(string registryKey, string backupPath)
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = Path.Combine(Environment.SystemDirectory, "reg.exe"),
            CreateNoWindow = true,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add("export");
        startInfo.ArgumentList.Add(registryKey);
        startInfo.ArgumentList.Add(backupPath);
        startInfo.ArgumentList.Add("/y");

        using Process process = Process.Start(startInfo)
            ?? throw new Win32Exception("Failed to start Windows registry backup.");
        if (!process.WaitForExit(10000))
        {
            process.Kill();
            process.WaitForExit();
            throw new IOException($"Registry backup timed out for {registryKey}. No entries have been removed.");
        }

        if (process.ExitCode != 0 || !File.Exists(backupPath))
        {
            throw new IOException($"Could not back up {registryKey}. No entries have been removed.");
        }
    }
}
