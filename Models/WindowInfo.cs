namespace WindowDeck.Models;

internal sealed record WindowInfo(
    nint Handle,
    uint ProcessId,
    string ApplicationId,
    string ApplicationName,
    string OriginalTitle,
    string DisplayTitle,
    string? MonitorDeviceName,
    int? MonitorNumber,
    bool IsMinimized);
