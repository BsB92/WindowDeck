using System.ComponentModel;
using System.Diagnostics;
using WindowDeck.Localization;
using WindowDeck.Models;

namespace WindowDeck.Services;

internal sealed class SavedWindowRestoreResult
{
    public int Requested { get; set; }
    public int AlreadyOpen { get; set; }
    public int Unsupported { get; set; }
    public int Missing { get; set; }
    public int Failed { get; set; }
    public List<string> Notes { get; } = [];
}

internal sealed class SavedWindowRestoreService
{
    // Protect against double-clicks while programs are starting, but don't
    // permanently mark a document open if it was closed later in the session.
    private static readonly Dictionary<string, DateTime> recentlyRequested =
        new(StringComparer.OrdinalIgnoreCase);

    public SavedWindowRestoreResult Open(SavedWindowSet set, AppSettings settings)
    {
        SavedWindowRestoreResult result = new();
        AppSettings discoverSettings = settings.Copy();
        discoverSettings.ShowAuxiliaryWindows = true;

        IReadOnlyList<WindowInfo> currentlyOpen = new WindowEnumerator().Enumerate(discoverSettings);
        SavedWindowCaptureService capture = new();
        HashSet<string> openResources = new(StringComparer.OrdinalIgnoreCase);
        HashSet<string> openPrograms = new(StringComparer.OrdinalIgnoreCase);
        HashSet<uint> checkedProcesses = [];

        foreach (WindowInfo window in currentlyOpen)
        {
            try
            {
                SavedWindowEntry known = capture.Capture(window);
                if (known.Kind != SavedWindowKind.Unsupported
                    && known.Kind != SavedWindowKind.Application
                    && SavedWindowCaptureService.IsSupportedDocument(known.Kind, known.ResourcePath))
                {
                    openResources.Add(NormalizeDocument(known.Kind, known.ResourcePath!));
                }

                if (checkedProcesses.Add(window.ProcessId))
                {
                    using Process process = Process.GetProcessById(checked((int)window.ProcessId));
                    string? executable = process.MainModule?.FileName;
                    if (SavedWindowCaptureService.IsSupportedExecutable(executable))
                    {
                        openPrograms.Add(NormalizeProgram(executable!));
                    }
                }
            }
            catch (Exception error) when (error is not OutOfMemoryException)
            {
                // An inaccessible process must not prevent the other resources
                // in the user's set from reopening.
            }
        }

        DateTime cutoff = DateTime.UtcNow.AddSeconds(-20);
        foreach (string key in recentlyRequested.Where(pair => pair.Value < cutoff)
                     .Select(pair => pair.Key).ToArray())
        {
            recentlyRequested.Remove(key);
        }

        RecentDocumentResolver recentDocuments = new();
        foreach (SavedWindowEntry window in set.Windows)
        {
            try
            {
                string processName = !string.IsNullOrWhiteSpace(window.ExecutablePath)
                    ? Path.GetFileNameWithoutExtension(window.ExecutablePath)
                    : window.ApplicationName;

                // Older sets can contain Foxit/Notepad as a program-only entry.
                // Recover the document if possible; otherwise NEVER treat
                // opening a blank reader/editor as a successful restore.
                bool documentWindow =
                    (window.Kind is SavedWindowKind.Application or SavedWindowKind.Unsupported)
                    && RecentDocumentResolver.LooksLikeDocumentWindow(
                        window.WindowTitle, processName);
                if (documentWindow)
                {
                    string? resolved = recentDocuments.Resolve(window.WindowTitle, processName);
                    if (SavedWindowCaptureService.IsSupportedDocument(
                        SavedWindowKind.DocumentFile, resolved))
                    {
                        OpenDocument(new SavedWindowEntry
                        {
                            ApplicationName = window.ApplicationName,
                            WindowTitle = window.WindowTitle,
                            ExecutablePath = window.ExecutablePath,
                            Kind = SavedWindowKind.DocumentFile,
                            ResourcePath = resolved
                        }, openResources, result);
                    }
                    else
                    {
                        result.Unsupported++;
                        result.Notes.Add(LocalizationService.Format(
                            "Sets_RestoreNeedsDocumentPath", window.WindowTitle));
                    }
                }
                else if (window.Kind == SavedWindowKind.Application)
                {
                    OpenApplication(window, openPrograms, result);
                }
                else
                {
                    OpenDocument(window, openResources, result);
                }
            }
            catch (Exception error) when (error is not OutOfMemoryException)
            {
                result.Failed++;
                result.Notes.Add(LocalizationService.Format(
                    "Sets_OpenFailed", window.WindowTitle, error.Message));
            }
        }

        return result;
    }

    private static void OpenApplication(
        SavedWindowEntry window, HashSet<string> running, SavedWindowRestoreResult result)
    {
        string? executable = window.ExecutablePath;
        if (!SavedWindowCaptureService.IsSupportedExecutable(executable))
        {
            result.Unsupported++;
            return;
        }

        string key = NormalizeProgram(executable!);
        if (running.Contains(key) || recentlyRequested.ContainsKey(key))
        {
            result.AlreadyOpen++;
            return;
        }

        if (!File.Exists(executable))
        {
            result.Missing++;
            result.Notes.Add(LocalizationService.Format("Sets_MissingProgram", executable));
            return;
        }

        using Process? launched = Process.Start(new ProcessStartInfo
        {
            FileName = executable,
            UseShellExecute = true
        });
        recentlyRequested[key] = DateTime.UtcNow;
        running.Add(key);
        result.Requested++;
    }

    private static void OpenDocument(
        SavedWindowEntry window, HashSet<string> openResources, SavedWindowRestoreResult result)
    {
        if (!SavedWindowCaptureService.IsSupportedDocument(window.Kind, window.ResourcePath))
        {
            result.Unsupported++;
            if (window.NeedsDocumentPath)
            {
                result.Notes.Add(LocalizationService.Format(
                    "Sets_RestoreNeedsDocumentPath", window.WindowTitle));
            }
            return;
        }

        string file = window.ResourcePath!;
        string key = NormalizeDocument(window.Kind, file);
        if (openResources.Contains(key) || recentlyRequested.ContainsKey(key))
        {
            result.AlreadyOpen++;
            return;
        }

        bool exists = window.Kind == SavedWindowKind.ExplorerFolder
            ? Directory.Exists(file) : File.Exists(file);
        if (!exists)
        {
            result.Missing++;
            result.Notes.Add(LocalizationService.Format("Sets_MissingFile", file));
            return;
        }

        ProcessStartInfo start;
        if (window.Kind == SavedWindowKind.DocumentFile
            && SavedWindowCaptureService.IsSupportedExecutable(window.ExecutablePath)
            && File.Exists(window.ExecutablePath))
        {
            // Use the same application for manually linked files, e.g. Notepad
            // or Foxit Reader. ArgumentList guarantees correct quoting.
            start = new ProcessStartInfo
            {
                FileName = window.ExecutablePath!,
                UseShellExecute = false
            };
            start.ArgumentList.Add(file);
        }
        else
        {
            // Let Windows use the user's file association for verified Office
            // documents, Explorer directories or missing original application.
            start = new ProcessStartInfo
            {
                FileName = file,
                UseShellExecute = true
            };
        }

        try
        {
            using Process? launched = Process.Start(start);
        }
        catch (Exception error) when (!start.UseShellExecute && error is
            (Win32Exception or IOException or UnauthorizedAccessException
                or InvalidOperationException))
        {
            // The original application may have been moved, unregistered or
            // installed as a packaged app. Try the user's current association
            // for the exact same document instead of abandoning the set.
            using Process? launched = Process.Start(new ProcessStartInfo
            {
                FileName = file,
                UseShellExecute = true
            });
        }

        recentlyRequested[key] = DateTime.UtcNow;
        openResources.Add(key);
        result.Requested++;
    }

    private static string NormalizeDocument(SavedWindowKind kind, string path)
    {
        string prefix = kind == SavedWindowKind.ExplorerFolder ? "folder:" : "file:";
        return prefix + Path.GetFullPath(path).TrimEnd(
            Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    private static string NormalizeProgram(string executable) =>
        "app:" + Path.GetFullPath(executable);
}
