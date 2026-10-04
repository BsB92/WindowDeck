using WindowDeck.Models;

namespace WindowDeck.Services;

internal static class WindowListPresentation
{
    private static readonly StringComparer DisplayComparer = StringComparer.CurrentCultureIgnoreCase;

    public static IReadOnlyList<IGrouping<string, WindowInfo>> Create(
        IReadOnlyList<WindowInfo> snapshot,
        string searchText,
        bool groupByApplication,
        bool showMinimizedWindows)
    {
        string query = searchText.Trim();

        WindowInfo[] eligible = snapshot
            .Where(window => showMinimizedWindows || !window.IsMinimized)
            .ToArray();
        HashSet<WindowIdentity> matchingIds = eligible
            .Where(window => query.Length == 0
                || window.DisplayTitle.Contains(query, StringComparison.CurrentCultureIgnoreCase)
                || window.ApplicationName.Contains(query, StringComparison.CurrentCultureIgnoreCase))
            .Select(WindowIdentity.From)
            .ToHashSet();
        // Keep a matching auxiliary window's owner as context during search.
        HashSet<WindowIdentity> ownerIds = eligible
            .Where(window => matchingIds.Contains(WindowIdentity.From(window)))
            .Where(window => window.OwnerWindow.HasValue)
            .Select(window => window.OwnerWindow!.Value)
            .ToHashSet();

        IEnumerable<WindowInfo> filtered = eligible
            .Where(window => matchingIds.Contains(WindowIdentity.From(window))
                || ownerIds.Contains(WindowIdentity.From(window)))
            .OrderBy(window => window.ApplicationName, DisplayComparer)
            .ThenBy(window => window.DisplayTitle, DisplayComparer)
            .ThenBy(window => window.OriginalTitle, DisplayComparer)
            .ThenBy(window => window.ProcessId)
            .ThenBy(window => window.Handle.ToInt64());

        if (!groupByApplication)
        {
            filtered = filtered
                .OrderBy(window => window.DisplayTitle, DisplayComparer)
                .ThenBy(window => window.OriginalTitle, DisplayComparer)
                .ThenBy(window => window.Handle.ToInt64());
        }

        return filtered
            .GroupBy(window => groupByApplication ? window.ApplicationId : string.Empty,
                StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
