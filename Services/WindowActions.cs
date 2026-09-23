using System.Runtime.InteropServices;
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

    public bool MoveToMonitor(WindowInfo window, MonitorDisplay target)
    {
        if (!IsSameWindow(window))
        {
            return false;
        }

        NativeMethods.WindowPlacement placement = new()
        {
            Length = (uint)Marshal.SizeOf<NativeMethods.WindowPlacement>()
        };
        if (!NativeMethods.GetWindowPlacement(window.Handle, ref placement))
        {
            return false;
        }

        Rectangle sourceArea = Screen.FromHandle(window.Handle).WorkingArea;
        Rectangle targetArea = target.WorkArea;
        NativeMethods.Rect normal = placement.NormalPosition;
        int width = Math.Min(normal.Right - normal.Left, targetArea.Width);
        int height = Math.Min(normal.Bottom - normal.Top, targetArea.Height);
        int relativeX = normal.Left - sourceArea.Left;
        int relativeY = normal.Top - sourceArea.Top;
        int left = Math.Clamp(targetArea.Left + relativeX, targetArea.Left, targetArea.Right - width);
        int top = Math.Clamp(targetArea.Top + relativeY, targetArea.Top, targetArea.Bottom - height);
        placement.NormalPosition = new NativeMethods.Rect
        {
            Left = left,
            Top = top,
            Right = left + width,
            Bottom = top + height
        };

        return IsSameWindow(window)
            && NativeMethods.SetWindowPlacement(window.Handle, ref placement);
    }

    private static bool IsSameWindow(WindowInfo window)
    {
        return NativeMethods.IsWindow(window.Handle)
            && NativeMethods.GetWindowThreadProcessId(window.Handle, out uint processId) != 0
            && processId == window.ProcessId;
    }
}
