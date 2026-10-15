using WindowDeck.Interop;
using WindowDeck.Models;

namespace WindowDeck.Services;

internal sealed record SavedSetCloseTarget(WindowInfo Window, string ResourceKey);

internal sealed record SavedSetClosePlan(
    IReadOnlyList<SavedSetCloseTarget> Targets,
    int SkippedSavedEntries);

internal sealed class SavedSetCloseResult
{
    // These are requests, not proof that the app actually closed its window.
    public int Requested { get; set; }
    public int Failed { get; set; }
    public int NoLongerMatching { get; set; }
}

internal sealed class SavedWindowSetCloseService
{
    private readonly WindowActions windowActions = new();

    public SavedSetClosePlan FindOpenWindows(SavedWindowSet set, AppSettings settings)
    {
        HashSet<string> savedTargets = set.Windows
            .Select(ResourceKey)
            .Where(key => key is not null)
            .Select(key => key!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Resolve by the live window's *actual document/folder path*, not by
        // its process name or possibly stale visible title.
        Dictionary<string, List<WindowInfo>> live = GetOpenDocumentWindows(settings);
        List<SavedSetCloseTarget> matches = [];
        HashSet<nint> usedHandles = [];

        foreach (string key in savedTargets)
        {
            if (!live.TryGetValue(key, out List<WindowInfo>? windows))
            {
                continue;
            }

            foreach (WindowInfo window in windows)
            {
                if (usedHandles.Add(window.Handle))
                {
                    matches.Add(new SavedSetCloseTarget(window, key));
                }
            }
        }

        int skipped = set.Windows.Count(entry =>
        {
            string? key = ResourceKey(entry);
            return key is null || !live.ContainsKey(key);
        });
        return new SavedSetClosePlan(matches, skipped);
    }

    public SavedSetCloseResult RequestClose(
        SavedSetClosePlan confirmedPlan, AppSettings settings)
    {
        SavedSetCloseResult result = new();

        // Recheck windows after the user has read the confirmation, since the
        // document may have changed or the window may have disappeared.
        Dictionary<string, List<WindowInfo>> current = GetOpenDocumentWindows(settings);
        Dictionary<nint, WindowInfo> byHandle = current.Values.SelectMany(windows => windows)
            .GroupBy(window => window.Handle)
            .ToDictionary(group => group.Key, group => group.First());

        foreach (SavedSetCloseTarget target in confirmedPlan.Targets)
        {
            if (!byHandle.TryGetValue(target.Window.Handle, out WindowInfo? window)
                || window.ProcessId != target.Window.ProcessId
                || !current.TryGetValue(target.ResourceKey, out List<WindowInfo>? resourceWindows)
                || !resourceWindows.Any(candidate => candidate.Handle == window.Handle))
            {
                result.NoLongerMatching++;
                continue;
            }

            // SC_CLOSE is graceful. The target app, not WindowDeck, controls
            // Save / Don't Save / Cancel and may decline to close.
            if (windowActions.RequestClose(window))
            {
                result.Requested++;
            }
            else
            {
                result.Failed++;
            }
        }

        return result;
    }

    private static Dictionary<string, List<WindowInfo>> GetOpenDocumentWindows(
        AppSettings settings)
    {
        AppSettings scanSettings = settings.Copy();
        scanSettings.ShowAuxiliaryWindows = true;
        SavedWindowCaptureService capture = new();
        Dictionary<string, List<WindowInfo>> byResource =
            new(StringComparer.OrdinalIgnoreCase);

        foreach (WindowInfo window in new WindowEnumerator().Enumerate(scanSettings))
        {
            if (window.OwnerWindow is not null
                || !NativeMethods.IsWindow(window.Handle)
                || NativeMethods.GetWindowThreadProcessId(
                    window.Handle, out uint pid) == 0
                || pid != window.ProcessId)
            {
                continue;
            }

            SavedWindowEntry entry = capture.Capture(window);
            string? key = ResourceKey(entry);
            if (key is null)
            {
                continue;
            }

            if (!byResource.TryGetValue(key, out List<WindowInfo>? group))
            {
                group = [];
                byResource.Add(key, group);
            }

            group.Add(window);
        }

        return byResource;
    }

    private static string? ResourceKey(SavedWindowEntry entry)
    {
        // Never close program-only/browser windows: they may have unsaved
        // tabs or other documents that are not part of this saved set.
        if (entry.Kind is SavedWindowKind.Unsupported or SavedWindowKind.Application
            || !SavedWindowCaptureService.IsSupportedDocument(entry.Kind, entry.ResourcePath))
        {
            return null;
        }

        try
        {
            string kind = entry.Kind == SavedWindowKind.ExplorerFolder ? "folder:" : "file:";
            string normalized = Path.GetFullPath(entry.ResourcePath!);
            return kind + Path.TrimEndingDirectorySeparator(normalized);
        }
        catch (Exception error) when (error is ArgumentException
            or NotSupportedException or PathTooLongException)
        {
            return null;
        }
    }
}
