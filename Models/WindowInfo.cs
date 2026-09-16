namespace WindowDeck.Models;

internal sealed record WindowInfo(
    nint Handle,
    uint ProcessId,
    string OriginalTitle,
    string DisplayTitle);
