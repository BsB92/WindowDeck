namespace WindowDeck.Models;

internal sealed record WindowInfo(
    nint Handle,
    uint ProcessId,
    string ProcessName,
    string OriginalTitle,
    string DisplayTitle);
