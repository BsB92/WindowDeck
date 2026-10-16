using WindowDeck.Models;

namespace WindowDeck.Services;

// Read-only readiness checks can be slow for offline and cloud-synced paths.
// Call from a worker thread, never while building WinForms rows.
internal static class SavedWindowReadiness
{
    public static bool IsReady(SavedWindowEntry entry)
    {
        try
        {
            return entry.Kind switch
            {
                SavedWindowKind.Application =>
                    SavedWindowCaptureService.IsSupportedExecutable(entry.ExecutablePath)
                    && File.Exists(entry.ExecutablePath),
                SavedWindowKind.ExplorerFolder =>
                    SavedWindowCaptureService.IsSupportedDocument(
                        entry.Kind, entry.ResourcePath)
                    && Directory.Exists(entry.ResourcePath),
                SavedWindowKind.Unsupported => false,
                _ => SavedWindowCaptureService.IsSupportedDocument(
                        entry.Kind, entry.ResourcePath)
                    && File.Exists(entry.ResourcePath)
            };
        }
        catch (Exception error) when (error is ArgumentException
            or NotSupportedException or UnauthorizedAccessException
            or IOException)
        {
            return false;
        }
    }
}
