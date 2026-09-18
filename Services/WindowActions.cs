using WindowDeck.Interop;
using WindowDeck.Models;

namespace WindowDeck.Services;

internal sealed class WindowActions
{
    public bool Minimize(WindowInfo window)
    {
        if (!IsSameWindow(window))
        {
            return false;
        }

        // ShowWindowAsync reports the previous visibility state, not operation success.
        NativeMethods.ShowWindowAsync(window.Handle, NativeMethods.SwMinimize);
        return IsSameWindow(window);
    }

    public bool RequestClose(WindowInfo window)
    {
        return IsSameWindow(window)
            && NativeMethods.PostMessage(
                window.Handle,
                NativeMethods.WmSysCommand,
                NativeMethods.ScClose,
                0);
    }

    private static bool IsSameWindow(WindowInfo window)
    {
        return NativeMethods.IsWindow(window.Handle)
            && NativeMethods.GetWindowThreadProcessId(window.Handle, out uint processId) != 0
            && processId == window.ProcessId;
    }
}
