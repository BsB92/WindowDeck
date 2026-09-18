using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using WindowDeck.Interop;
using WindowDeck.Models;
using WindowDeck.Localization;

namespace WindowDeck.Services;

internal sealed class WindowEnumerator
{
    private readonly uint currentProcessId = (uint)Environment.ProcessId;
    private readonly MonitorDetector monitorDetector = new();

    public IReadOnlyList<WindowInfo> Enumerate()
    {
        List<WindowInfo> windows = [];
        monitorDetector.RefreshDisplayMapping();

        bool succeeded = NativeMethods.EnumWindows((windowHandle, _) =>
            {
                if (TryCreateWindowInfo(windowHandle, out WindowInfo? window))
                {
                    windows.Add(window);
                }

                return true;
            },
            0);

        if (!succeeded)
        {
            int error = Marshal.GetLastWin32Error();
            throw new Win32Exception(error, LocalizationService.Get("Message_EnumerationFailed"));
        }

        return windows;
    }

    private bool TryCreateWindowInfo(nint windowHandle, out WindowInfo? window)
    {
        window = null;

        if (windowHandle == NativeMethods.GetShellWindow()
            || !NativeMethods.IsWindowVisible(windowHandle)
            || IsCloaked(windowHandle))
        {
            return false;
        }

        long extendedStyle = NativeMethods.GetWindowLongPtr(
            windowHandle,
            NativeMethods.GwlExStyle).ToInt64();

        if ((extendedStyle & NativeMethods.WsExToolWindow) != 0)
        {
            return false;
        }

        bool hasOwner = NativeMethods.GetWindow(windowHandle, NativeMethods.GwOwner) != 0;
        bool appearsOnTaskbar = (extendedStyle & NativeMethods.WsExAppWindow) != 0;
        if (hasOwner && !appearsOnTaskbar)
        {
            return false;
        }

        NativeMethods.GetWindowThreadProcessId(windowHandle, out uint processId);
        if (processId == 0 || processId == currentProcessId)
        {
            return false;
        }

        string? title = GetWindowTitle(windowHandle);
        if (string.IsNullOrWhiteSpace(title))
        {
            return false;
        }

        monitorDetector.Detect(windowHandle, out string? monitorDeviceName, out int? monitorNumber);
        window = new WindowInfo(
            windowHandle,
            processId,
            GetApplicationName(processId),
            title,
            title,
            monitorDeviceName,
            monitorNumber,
            NativeMethods.IsIconic(windowHandle));
        return true;
    }

    private static string GetApplicationName(uint processId)
    {
        try
        {
            using Process process = Process.GetProcessById(checked((int)processId));
            string processName = process.ProcessName;
            if (processName.Equals("explorer", StringComparison.OrdinalIgnoreCase))
            {
                return LocalizationService.Get("Application_FileExplorer");
            }

            return string.IsNullOrEmpty(processName)
                ? LocalizationService.Get("Application_Unknown")
                : char.ToUpperInvariant(processName[0]) + processName[1..].ToLowerInvariant();
        }
        catch (ArgumentException)
        {
            return LocalizationService.Get("Application_Unknown");
        }
        catch (OverflowException)
        {
            return LocalizationService.Get("Application_Unknown");
        }
        catch (InvalidOperationException)
        {
            return LocalizationService.Get("Application_Unknown");
        }
        catch (Win32Exception)
        {
            return LocalizationService.Get("Application_Unknown");
        }
    }

    private static string? GetWindowTitle(nint windowHandle)
    {
        int titleLength = NativeMethods.GetWindowTextLength(windowHandle);
        if (titleLength <= 0)
        {
            return null;
        }

        StringBuilder title = new(titleLength + 1);
        return NativeMethods.GetWindowText(windowHandle, title, title.Capacity) > 0
            ? title.ToString()
            : null;
    }

    private static bool IsCloaked(nint windowHandle)
    {
        int result = NativeMethods.DwmGetWindowAttribute(
            windowHandle,
            NativeMethods.DwmaCloaked,
            out int cloaked,
            sizeof(int));

        return result == 0 && cloaked != 0;
    }
}
