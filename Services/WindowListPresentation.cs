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

        IEnumerable<WindowInfo> filtered = snapshot
            .Where(window => showMinimizedWindows || !window.IsMinimized)
            .Where(window => query.Length == 0
                || window.DisplayTitle.Contains(query, StringComparison.CurrentCultureIgnoreCase)
                || window.ApplicationName.Contains(query, StringComparison.CurrentCultureIgnoreCase))
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
            .GroupBy(window => groupByApplication ? window.ApplicationName : string.Empty, DisplayComparer)
            .ToList();
    }
}
