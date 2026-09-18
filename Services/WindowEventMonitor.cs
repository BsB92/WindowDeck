using System.ComponentModel;
using System.Runtime.InteropServices;
using WindowDeck.Interop;
using WindowDeck.Localization;

namespace WindowDeck.Services;

internal sealed class WindowEventMonitor : IDisposable
{
    private const int DebounceMilliseconds = 150;

    private static readonly uint[] MonitoredEvents =
    [
        NativeMethods.EventObjectCreate,
        NativeMethods.EventObjectDestroy,
        NativeMethods.EventObjectShow,
        NativeMethods.EventObjectHide,
        NativeMethods.EventObjectNameChange,
        NativeMethods.EventObjectLocationChange,
        NativeMethods.EventSystemMinimizeStart,
        NativeMethods.EventSystemMinimizeEnd
    ];

    private readonly object syncRoot = new();
    private readonly Action refreshRequested;
    private readonly NativeMethods.WinEventProc callback;
    private readonly System.Threading.Timer debounceTimer;
    private readonly List<nint> hookHandles = [];
    private bool disposed;

    private WindowEventMonitor(Action refreshRequested)
    {
        this.refreshRequested = refreshRequested;
        callback = OnWinEvent;
        debounceTimer = new System.Threading.Timer(
            OnDebounceElapsed,
            null,
            Timeout.Infinite,
            Timeout.Infinite);
    }

    public static bool TryStart(
        Action refreshRequested,
        out WindowEventMonitor? monitor,
        out string? errorMessage)
    {
        WindowEventMonitor candidate = new(refreshRequested);

        foreach (uint eventType in MonitoredEvents)
        {
            nint hookHandle = NativeMethods.SetWinEventHook(
                eventType,
                eventType,
                0,
                candidate.callback,
                0,
                0,
                NativeMethods.WineventOutofcontext | NativeMethods.WineventSkipownprocess);

            if (hookHandle == 0)
            {
                int error = Marshal.GetLastWin32Error();
                candidate.Dispose();
                monitor = null;
                errorMessage = new Win32Exception(
                    error,
                    LocalizationService.Format("Message_MonitoringFailed", eventType)).Message;
                return false;
            }

            candidate.hookHandles.Add(hookHandle);
        }

        monitor = candidate;
        errorMessage = null;
        return true;
    }

    public void Dispose()
    {
        lock (syncRoot)
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            debounceTimer.Change(Timeout.Infinite, Timeout.Infinite);

            foreach (nint hookHandle in hookHandles)
            {
                NativeMethods.UnhookWinEvent(hookHandle);
            }

            hookHandles.Clear();
            debounceTimer.Dispose();
        }
    }

    private void OnWinEvent(
        nint hookHandle,
        uint eventType,
        nint windowHandle,
        int objectId,
        int childId,
        uint eventThreadId,
        uint eventTime)
    {
        if (windowHandle == 0 || !IsRelevantEvent(eventType, objectId, childId))
        {
            return;
        }

        lock (syncRoot)
        {
            if (!disposed)
            {
                debounceTimer.Change(DebounceMilliseconds, Timeout.Infinite);
            }
        }
    }

    private void OnDebounceElapsed(object? state)
    {
        lock (syncRoot)
        {
            if (disposed)
            {
                return;
            }
        }

        refreshRequested();
    }

    private static bool IsRelevantEvent(uint eventType, int objectId, int childId)
    {
        if (eventType is NativeMethods.EventSystemMinimizeStart
            or NativeMethods.EventSystemMinimizeEnd)
        {
            return true;
        }

        return objectId == NativeMethods.ObjidWindow
            && childId == NativeMethods.ChildidSelf;
    }
}
