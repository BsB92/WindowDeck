using WindowDeck.Models;

namespace WindowDeck.Services;

// A set is durable: updating it MUST NOT delete documents simply because
// they are not open at the moment the user chooses "Update".
internal sealed record SavedWindowSelectionItem(
    SavedWindowEntry Entry,
    bool InSavedSet,
    bool CurrentlyOpen,
    bool InitiallyChecked);

internal static class SavedWindowSetUpdatePlanner
{
    public static IReadOnlyList<SavedWindowSelectionItem> ForNewSet(
        IReadOnlyList<SavedWindowEntry> openWindows) =>
        openWindows.Select(entry => new SavedWindowSelectionItem(
            Copy(entry), InSavedSet: false, CurrentlyOpen: true, InitiallyChecked: true))
        .ToArray();

    public static IReadOnlyList<SavedWindowSelectionItem> ForUpdate(
        IReadOnlyList<SavedWindowEntry> savedWindows,
        IReadOnlyList<SavedWindowEntry> openWindows)
    {
        List<SavedWindowSelectionItem> result = savedWindows
            .Select(entry => new SavedWindowSelectionItem(
                Copy(entry), InSavedSet: true, CurrentlyOpen: false, InitiallyChecked: true))
            .ToList();

        // Each live window may match at most one stored entry. A matching
        // document/folder is the same resource, regardless of application kind.
        HashSet<int> matched = [];
        foreach (SavedWindowEntry live in openWindows)
        {
            int index = -1;
            for (int i = 0; i < result.Count; i++)
            {
                if (!matched.Contains(i) && Matches(result[i].Entry, live))
                {
                    index = i;
                    break;
                }
            }

            if (index >= 0)
            {
                SavedWindowSelectionItem previous = result[index];
                SavedWindowEntry original = previous.Entry;
                // Upgrade an old unresolved item when a trustworthy document
                // path became available; otherwise keep the saved file path.
                if (original.Kind == SavedWindowKind.Unsupported
                    && live.Kind != SavedWindowKind.Unsupported
                    && !string.IsNullOrWhiteSpace(live.ResourcePath))
                {
                    original.Kind = live.Kind;
                    original.ResourcePath = live.ResourcePath;
                    original.NeedsDocumentPath = false;
                }
                result[index] = previous with { CurrentlyOpen = true };
                matched.Add(index);
            }
            else
            {
                result.Add(new SavedWindowSelectionItem(
                    Copy(live), InSavedSet: false, CurrentlyOpen: true,
                    InitiallyChecked: false));
            }
        }

        return result;
    }

    private static bool Matches(SavedWindowEntry saved, SavedWindowEntry live)
    {
        // Specific resource paths are stronger than window titles; avoid
        // matching two distinct workbooks with the same display title.
        bool savedHasPath = !string.IsNullOrWhiteSpace(saved.ResourcePath);
        bool liveHasPath = !string.IsNullOrWhiteSpace(live.ResourcePath);
        if (savedHasPath && liveHasPath)
        {
            return SamePath(saved.ResourcePath!, live.ResourcePath!);
        }

        // An app-only entry isn't identical to a document-bearing entry.
        if (saved.Kind == SavedWindowKind.Application
            && live.Kind == SavedWindowKind.Application
            && !string.IsNullOrWhiteSpace(saved.ExecutablePath)
            && !string.IsNullOrWhiteSpace(live.ExecutablePath))
        {
            return SamePath(saved.ExecutablePath!, live.ExecutablePath!)
                && string.Equals(saved.WindowTitle, live.WindowTitle,
                    StringComparison.CurrentCultureIgnoreCase);
        }

        // Only permit a title-based match if one side lacks a path and the
        // original window/app identity agrees. This can upgrade unresolved
        // entries, but cannot replace two different known document paths.
        return (!savedHasPath || !liveHasPath)
            && string.Equals(saved.ApplicationName, live.ApplicationName,
                StringComparison.CurrentCultureIgnoreCase)
            && string.Equals(saved.WindowTitle, live.WindowTitle,
                StringComparison.CurrentCultureIgnoreCase);
    }

    private static bool SamePath(string left, string right)
    {
        try
        {
            return string.Equals(Path.GetFullPath(left).TrimEnd('\\', '/'),
                Path.GetFullPath(right).TrimEnd('\\', '/'),
                StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception e) when (e is ArgumentException or NotSupportedException
            or PathTooLongException)
        {
            return false;
        }
    }

    private static SavedWindowEntry Copy(SavedWindowEntry value) => new()
    {
        ApplicationName = value.ApplicationName,
        WindowTitle = value.WindowTitle,
        Kind = value.Kind,
        ResourcePath = value.ResourcePath,
        ExecutablePath = value.ExecutablePath,
        NeedsDocumentPath = value.NeedsDocumentPath
    };
}
