using System.Reflection;

namespace WindowDeck.Services;

internal static class WindowDeckIcon
{
    private const string ResourceName = "WindowDeck.Assets.WindowDeck.ico";

    public static Icon Load()
    {
        using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException("The embedded WindowDeck icon is unavailable.");
        using Icon embedded = new(stream);
        return (Icon)embedded.Clone();
    }
}
