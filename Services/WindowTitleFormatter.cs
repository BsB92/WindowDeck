namespace WindowDeck.Services;

internal static class WindowTitleFormatter
{
    private const string Separator = " - ";

    public static string CreateDisplayTitle(
        string originalTitle,
        IReadOnlySet<string> applicationSuffixes)
    {
        int separatorIndex = originalTitle.LastIndexOf(Separator, StringComparison.Ordinal);
        if (separatorIndex <= 0)
        {
            return originalTitle;
        }

        string suffix = originalTitle[(separatorIndex + Separator.Length)..].Trim();
        return applicationSuffixes.Contains(suffix)
            ? originalTitle[..separatorIndex].TrimEnd()
            : originalTitle;
    }
}
