using System.Runtime.InteropServices;
using System.Text;

namespace WindowDeck.Interop;

internal static class NativeMethods
{
    internal const int DwmaCloaked = 14;
    internal const int ErrorInsufficientBuffer = 122;
    internal const int GwlExStyle = -20;
    internal const uint MonitorDefaultToNearest = 2;
    internal const uint QdcOnlyActivePaths = 2;
    internal const int SwRestore = 9;
    internal const uint GwOwner = 4;
    internal const long WsExAppWindow = 0x00040000L;
    internal const long WsExToolWindow = 0x00000080L;

    internal delegate bool EnumWindowsProc(nint windowHandle, nint parameter);

    [StructLayout(LayoutKind.Sequential)]
    internal struct LocallyUniqueIdentifier
    {
        internal uint LowPart;
        internal int HighPart;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct DisplayConfigPathSourceInfo
    {
        internal LocallyUniqueIdentifier AdapterId;
        internal uint Id;
        internal uint ModeInfoIndex;
        internal uint StatusFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct DisplayConfigRational
    {
        internal uint Numerator;
        internal uint Denominator;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct DisplayConfigPathTargetInfo
    {
        internal LocallyUniqueIdentifier AdapterId;
        internal uint Id;
        internal uint ModeInfoIndex;
        internal uint OutputTechnology;
        internal uint Rotation;
        internal uint Scaling;
        internal DisplayConfigRational RefreshRate;
        internal uint ScanLineOrdering;

        [MarshalAs(UnmanagedType.Bool)]
        internal bool TargetAvailable;

        internal uint StatusFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct DisplayConfigPathInfo
    {
        internal DisplayConfigPathSourceInfo SourceInfo;
        internal DisplayConfigPathTargetInfo TargetInfo;
        internal uint Flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct DisplayConfigModeInfo
    {
        internal uint InfoType;
        internal uint Id;
        internal LocallyUniqueIdentifier AdapterId;
        internal DisplayConfigModeInfoUnion ModeInfo;
    }

    [StructLayout(LayoutKind.Explicit, Size = 48)]
    internal struct DisplayConfigModeInfoUnion
    {
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct DisplayConfigSourceDeviceName
    {
        internal uint Type;
        internal uint Size;
        internal LocallyUniqueIdentifier AdapterId;
        internal uint Id;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        internal string? ViewGdiDeviceName;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Rect
    {
        internal int Left;
        internal int Top;
        internal int Right;
        internal int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct MonitorInfoEx
    {
        internal uint Size;
        internal Rect Monitor;
        internal Rect WorkArea;
        internal uint Flags;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        internal string? DeviceName;
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool EnumWindows(EnumWindowsProc callback, nint parameter);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool IsWindowVisible(nint windowHandle);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool IsWindow(nint windowHandle);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool IsIconic(nint windowHandle);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool ShowWindowAsync(nint windowHandle, int command);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetForegroundWindow(nint windowHandle);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern int GetWindowText(nint windowHandle, StringBuilder text, int maximumCount);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern int GetWindowTextLength(nint windowHandle);

    [DllImport("user32.dll")]
    internal static extern nint GetWindow(nint windowHandle, uint command);

    [DllImport("user32.dll")]
    internal static extern nint GetShellWindow();

    [DllImport("user32.dll")]
    internal static extern uint GetWindowThreadProcessId(nint windowHandle, out uint processId);

    [DllImport("user32.dll")]
    internal static extern nint MonitorFromWindow(nint windowHandle, uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetMonitorInfo(
        nint monitorHandle,
        ref MonitorInfoEx monitorInfo);

    [DllImport("user32.dll")]
    internal static extern int GetDisplayConfigBufferSizes(
        uint flags,
        out uint pathCount,
        out uint modeCount);

    [DllImport("user32.dll")]
    internal static extern int QueryDisplayConfig(
        uint flags,
        ref uint pathCount,
        [Out] DisplayConfigPathInfo[] paths,
        ref uint modeCount,
        [Out] DisplayConfigModeInfo[] modes,
        nint currentTopologyId);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    internal static extern int DisplayConfigGetDeviceInfo(
        ref DisplayConfigSourceDeviceName requestPacket);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
    private static extern int GetWindowLong32(nint windowHandle, int index);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern nint GetWindowLongPtr64(nint windowHandle, int index);

    [DllImport("dwmapi.dll")]
    internal static extern int DwmGetWindowAttribute(
        nint windowHandle,
        int attribute,
        out int attributeValue,
        int attributeSize);

    internal static nint GetWindowLongPtr(nint windowHandle, int index)
    {
        return nint.Size == 8
            ? GetWindowLongPtr64(windowHandle, index)
            : GetWindowLong32(windowHandle, index);
    }
}
