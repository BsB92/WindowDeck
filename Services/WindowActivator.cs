using WindowDeck.Interop;
using WindowDeck.Models;

namespace WindowDeck.Services;

internal sealed class WindowActivator
{
    public WindowActivationResult Activate(WindowInfo window)
    {
        nint windowHandle = window.Handle;

        if (!NativeMethods.IsWindow(windowHandle))
        {
            return WindowActivationResult.WindowUnavailable;
        }

        if (NativeMethods.IsIconic(windowHandle)
            && !NativeMethods.ShowWindowAsync(windowHandle, NativeMethods.SwRestore))
        {
            return NativeMethods.IsWindow(windowHandle)
                ? WindowActivationResult.RestorationFailed
                : WindowActivationResult.WindowUnavailable;
        }

        if (!NativeMethods.IsWindow(windowHandle))
        {
            return WindowActivationResult.WindowUnavailable;
        }

        if (NativeMethods.SetForegroundWindow(windowHandle))
        {
            return WindowActivationResult.Activated;
        }

        return NativeMethods.IsWindow(windowHandle)
            ? WindowActivationResult.ForegroundActivationFailed
            : WindowActivationResult.WindowUnavailable;
    }
}
