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

    public bool Restore(WindowInfo window)
    {
        if (!IsSameWindow(window))
        {
            return false;
        }

        if (!NativeMethods.IsIconic(window.Handle))
        {
            return true;
        }

        // ShowWindowAsync reports the previous visibility state, not operation success.
        NativeMethods.ShowWindowAsync(window.Handle, NativeMethods.SwRestore);
        return IsSameWindow(window);
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

        bool isMinimized = NativeMethods.IsIconic(window.Handle);
        bool isMaximized = placement.ShowCommand == NativeMethods.SwMaximize;
        Rectangle targetArea = target.WorkArea;
        Rectangle restoredBounds;
        if (!isMinimized && !isMaximized
            && NativeMethods.GetWindowRect(window.Handle, out NativeMethods.Rect windowRectangle))
        {
            restoredBounds = ToRectangle(windowRectangle);
        }
        else
        {
            restoredBounds = WorkspaceToScreen(ToRectangle(placement.NormalPosition));
        }

        Rectangle sourceArea = Screen.FromRectangle(restoredBounds).WorkingArea;
        int width = Math.Clamp(restoredBounds.Width, 1, targetArea.Width);
        int height = Math.Clamp(restoredBounds.Height, 1, targetArea.Height);
        int relativeX = restoredBounds.Left - sourceArea.Left;
        int relativeY = restoredBounds.Top - sourceArea.Top;
        int left = Math.Clamp(targetArea.Left + relativeX, targetArea.Left, targetArea.Right - width);
        int top = Math.Clamp(targetArea.Top + relativeY, targetArea.Top, targetArea.Bottom - height);
        Rectangle targetBounds = new(left, top, width, height);
        placement.NormalPosition = ToNativeRect(ScreenToWorkspace(targetBounds));

        if (!IsSameWindow(window) || !NativeMethods.SetWindowPlacement(window.Handle, ref placement))
        {
            return false;
        }

        if (isMinimized)
        {
            return IsSameWindow(window);
        }

        if (isMaximized)
        {
            // Restore synchronously so SetWindowPos moves the restored window rather
            // than racing a queued state transition on the owning UI thread.
            NativeMethods.ShowWindow(window.Handle, NativeMethods.SwRestore);
        }

        bool moved = IsSameWindow(window)
            && NativeMethods.SetWindowPos(
                window.Handle,
                0,
                targetBounds.Left,
                targetBounds.Top,
                targetBounds.Width,
                targetBounds.Height,
                NativeMethods.SwpNoActivate | NativeMethods.SwpNoZOrder);

        if (moved && isMaximized && IsSameWindow(window))
        {
            NativeMethods.ShowWindow(window.Handle, NativeMethods.SwMaximize);
        }

        return moved && IsSameWindow(window);
    }

    private static Rectangle WorkspaceToScreen(Rectangle rectangle)
    {
        Screen primary = Screen.PrimaryScreen ?? Screen.AllScreens[0];
        return new Rectangle(
            rectangle.X + primary.WorkingArea.Left - primary.Bounds.Left,
            rectangle.Y + primary.WorkingArea.Top - primary.Bounds.Top,
            rectangle.Width,
            rectangle.Height);
    }

    private static Rectangle ScreenToWorkspace(Rectangle rectangle)
    {
        Screen primary = Screen.PrimaryScreen ?? Screen.AllScreens[0];
        return new Rectangle(
            rectangle.X - primary.WorkingArea.Left + primary.Bounds.Left,
            rectangle.Y - primary.WorkingArea.Top + primary.Bounds.Top,
            rectangle.Width,
            rectangle.Height);
    }

    private static Rectangle ToRectangle(NativeMethods.Rect rectangle) => new(
        rectangle.Left,
        rectangle.Top,
        rectangle.Right - rectangle.Left,
        rectangle.Bottom - rectangle.Top);

    private static NativeMethods.Rect ToNativeRect(Rectangle rectangle) => new()
    {
        Left = rectangle.Left,
        Top = rectangle.Top,
        Right = rectangle.Right,
        Bottom = rectangle.Bottom
    };

    private static bool IsSameWindow(WindowInfo window)
    {
        return NativeMethods.IsWindow(window.Handle)
            && NativeMethods.GetWindowThreadProcessId(window.Handle, out uint processId) != 0
            && processId == window.ProcessId;
    }
}
