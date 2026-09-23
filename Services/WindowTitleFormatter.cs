namespace WindowDeck.Services;

internal static class WindowTitleFormatter
{
    private const string Separator = " - ";

    public static string CreateDisplayTitle(string originalTitle, ApplicationIdentity application)
    {
        foreach (string alias in application.TitleAliases.OrderByDescending(value => value.Length))
        {
            string suffix = Separator + alias;
            if (originalTitle.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)
                && originalTitle.Length > suffix.Length)
            {
                return originalTitle[..^suffix.Length];
            }
        }

        return originalTitle;
    }
}
