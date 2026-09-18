using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using WindowDeck.Interop;
using WindowDeck.Models;

namespace WindowDeck.Services;

internal sealed class ApplicationIconProvider : IDisposable
{
    private const uint IconMessageTimeoutMilliseconds = 100;
    private readonly Dictionary<uint, Image> iconCache = [];
    private readonly Image fallbackIcon = SystemIcons.Application.ToBitmap();
    private bool disposed;

    public Image GetIcon(WindowInfo window)
    {
        if (iconCache.TryGetValue(window.ProcessId, out Image? cachedIcon))
        {
            return cachedIcon;
        }

        Image icon = TryGetWindowIcon(window.Handle)
            ?? TryGetExecutableIcon(window.ProcessId)
            ?? fallbackIcon;
        iconCache.Add(window.ProcessId, icon);
        return icon;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        foreach (Image icon in iconCache.Values.Distinct())
        {
            if (!ReferenceEquals(icon, fallbackIcon))
            {
                icon.Dispose();
            }
        }

        fallbackIcon.Dispose();
        iconCache.Clear();
    }

    private static Image? TryGetWindowIcon(nint windowHandle)
    {
        if (!NativeMethods.IsWindow(windowHandle))
        {
            return null;
        }

        nint iconHandle = GetWindowIconHandle(windowHandle, NativeMethods.IconSmall2);
        if (iconHandle == 0)
        {
            iconHandle = GetWindowIconHandle(windowHandle, NativeMethods.IconSmall);
        }

        if (iconHandle == 0)
        {
            iconHandle = GetWindowIconHandle(windowHandle, NativeMethods.IconBig);
        }

        if (iconHandle == 0)
        {
            iconHandle = NativeMethods.GetClassLongPtr(windowHandle, NativeMethods.GclpHiconSmall);
        }

        if (iconHandle == 0)
        {
            iconHandle = NativeMethods.GetClassLongPtr(windowHandle, NativeMethods.GclpHicon);
        }

        if (iconHandle == 0)
        {
            return null;
        }

        try
        {
            using Icon icon = (Icon)Icon.FromHandle(iconHandle).Clone();
            return icon.ToBitmap();
        }
        catch (Exception exception) when (exception is ArgumentException
            or ExternalException)
        {
            return null;
        }
    }

    private static nint GetWindowIconHandle(nint windowHandle, nint iconSize)
    {
        nint sendResult = NativeMethods.SendMessageTimeout(
            windowHandle,
            NativeMethods.WmGetIcon,
            iconSize,
            0,
            NativeMethods.SmtoAbortIfHung,
            IconMessageTimeoutMilliseconds,
            out nint iconHandle);
        return sendResult == 0 ? 0 : iconHandle;
    }

    private static Image? TryGetExecutableIcon(uint processId)
    {
        try
        {
            using Process process = Process.GetProcessById(checked((int)processId));
            string? executablePath = process.MainModule?.FileName;
            if (string.IsNullOrEmpty(executablePath))
            {
                return null;
            }

            using Icon? icon = Icon.ExtractAssociatedIcon(executablePath);
            return icon?.ToBitmap();
        }
        catch (Exception exception) when (exception is ArgumentException
            or InvalidOperationException
            or Win32Exception
            or IOException
            or NotSupportedException
            or UnauthorizedAccessException
            or OverflowException)
        {
            return null;
        }
    }
}
