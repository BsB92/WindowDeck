using System.Diagnostics;
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
    // Remember launches during this WindowDeck session to prevent pressing Open
    // twice while an Office app is still loading the document.
    private readonly HashSet<string> launchedThisSession = new(StringComparer.OrdinalIgnoreCase);

    public SavedWindowRestoreResult Open(SavedWindowSet set, AppSettings settings)
    {
        SavedWindowRestoreResult result = new();
        AppSettings discoverSettings = settings.Copy();
        discoverSettings.ShowAuxiliaryWindows = true;

        IReadOnlyList<WindowInfo> currentlyOpen = new WindowEnumerator().Enumerate(discoverSettings);
        SavedWindowCaptureService capture = new();
        HashSet<string> openResources = currentlyOpen
            .Select(capture.Capture)
            .Where(entry => entry.Kind != SavedWindowKind.Unsupported
                && !string.IsNullOrWhiteSpace(entry.ResourcePath))
            .Select(entry => NormalizeResource(entry.Kind, entry.ResourcePath!))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (SavedWindowEntry window in set.Windows)
        {
            if (!SavedWindowCaptureService.IsSupportedDocument(window.Kind, window.ResourcePath))
            {
                result.Unsupported++;
                continue;
            }

            string path = window.ResourcePath!;
            string key = NormalizeResource(window.Kind, path);
            if (openResources.Contains(key) || launchedThisSession.Contains(key))
            {
                result.AlreadyOpen++;
                continue;
            }

            bool exists;
            try
            {
                exists = window.Kind == SavedWindowKind.ExplorerFolder
                    ? Directory.Exists(path)
                    : File.Exists(path);
            }
            catch (Exception error) when (error is ArgumentException or IOException
                or UnauthorizedAccessException or NotSupportedException)
            {
                exists = false;
            }

            if (!exists)
            {
                result.Missing++;
                result.Notes.Add(window.ApplicationName + " — " + window.WindowTitle);
                continue;
            }

            try
            {
                ProcessStartInfo start = new()
                {
                    FileName = path,
                    UseShellExecute = true
                };
                using Process? process = Process.Start(start);
                // Process.Start may return null for a shell association; lack of
                // a Process object does not mean ShellExecute failed.
                launchedThisSession.Add(key);
                openResources.Add(key);
                result.Requested++;
            }
            catch (Exception error) when (error is System.ComponentModel.Win32Exception
                or IOException or UnauthorizedAccessException or InvalidOperationException)
            {
                result.Failed++;
                result.Notes.Add(window.ApplicationName + " — " + window.WindowTitle
                    + ": " + error.Message);
            }
        }

        return result;
    }

    private static string NormalizeResource(SavedWindowKind kind, string path)
    {
        return ((int)kind).ToString(System.Globalization.CultureInfo.InvariantCulture)
            + ":" + Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar);
    }
}
