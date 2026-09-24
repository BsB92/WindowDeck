using WindowDeck.Models;

namespace WindowDeck.Models;

internal readonly record struct WindowIdentity(nint Handle, uint ProcessId)
{
    public static WindowIdentity From(WindowInfo window) => new(window.Handle, window.ProcessId);
}
