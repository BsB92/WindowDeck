using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using WindowDeck.Interop;
using WindowDeck.Models;

namespace WindowDeck.Services;

internal sealed class WindowEnumerator
{
    public IReadOnlyList<WindowInfo> Enumerate(nint excludedWindowHandle)
    {
        List<WindowInfo> windows = [];
        nint shellWindowHandle = NativeMethods.GetShellWindow();

        NativeMethods.EnumWindowsCallback callback = (windowHandle, _) =>
        {
            WindowInfo? window = TryCreateWindowInfo(
                windowHandle,
                excludedWindowHandle,
                shellWindowHandle);
            if (window is not null)
            {
                windows.Add(window);
            }

            return true;
        };

        if (!NativeMethods.EnumWindows(callback, nint.Zero))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Top-level window enumeration failed.");
        }

        return windows;
    }

    private static WindowInfo? TryCreateWindowInfo(
        nint windowHandle,
        nint excludedWindowHandle,
        nint shellWindowHandle)
    {
        if (windowHandle == excludedWindowHandle
            || windowHandle == shellWindowHandle
            || !NativeMethods.IsWindowVisible(windowHandle))
        {
            return null;
        }

        nint extendedStyle = NativeMethods.GetWindowLongPtr(
            windowHandle,
            NativeMethods.ExtendedWindowStyleIndex);

        if ((extendedStyle.ToInt64() & NativeMethods.ExtendedStyleToolWindow) != 0)
        {
            return null;
        }

        int cloakedResult = NativeMethods.DwmGetWindowAttribute(
            windowHandle,
            NativeMethods.DwmWindowAttributeCloaked,
            out int cloaked,
            sizeof(int));

        if (cloakedResult >= 0 && cloaked != 0)
        {
            return null;
        }

        string? originalTitle = TryGetWindowTitle(windowHandle);
        if (string.IsNullOrWhiteSpace(originalTitle))
        {
            return null;
        }

        if (NativeMethods.GetWindowThreadProcessId(windowHandle, out uint processId) == 0)
        {
            return null;
        }

        string processName = TryGetProcessName(processId) ?? "Unknown";
        string displayTitle = CreateDisplayTitle(originalTitle, processName);

        return new WindowInfo(windowHandle, processId, processName, originalTitle, displayTitle);
    }

    private static string? TryGetWindowTitle(nint windowHandle)
    {
        int titleLength = NativeMethods.GetWindowTextLength(windowHandle);
        if (titleLength <= 0)
        {
            return null;
        }

        StringBuilder title = new(titleLength + 1);
        int charactersCopied = NativeMethods.GetWindowText(windowHandle, title, title.Capacity);

        return charactersCopied > 0 ? title.ToString() : null;
    }

    private static string? TryGetProcessName(uint processId)
    {
        if (processId > int.MaxValue)
        {
            return null;
        }

        try
        {
            using Process process = Process.GetProcessById((int)processId);
            return process.ProcessName;
        }
        catch (ArgumentException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
        catch (Win32Exception)
        {
            return null;
        }
    }

    private static string CreateDisplayTitle(string originalTitle, string processName)
    {
        string? redundantSuffix = processName switch
        {
            var name when name.Equals("EXCEL", StringComparison.OrdinalIgnoreCase) => " - Excel",
            var name when name.Equals("POWERPNT", StringComparison.OrdinalIgnoreCase) => " - PowerPoint",
            _ => null
        };

        if (redundantSuffix is not null
            && originalTitle.EndsWith(redundantSuffix, StringComparison.Ordinal)
            && originalTitle.Length > redundantSuffix.Length)
        {
            return originalTitle[..^redundantSuffix.Length];
        }

        return originalTitle;
    }
}
