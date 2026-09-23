using System.Runtime.InteropServices;
using System.Text;

namespace WindowDeck.Interop;

internal static class NativeMethods
{
    internal const int DwmaCloaked = 14;
    internal const int DwmaExtendedFrameBounds = 9;
    internal const int DwmaUseImmersiveDarkMode = 20;
    internal const uint EventObjectCreate = 0x8000;
    internal const uint EventObjectDestroy = 0x8001;
    internal const uint EventObjectShow = 0x8002;
    internal const uint EventObjectHide = 0x8003;
    internal const uint EventObjectLocationChange = 0x800B;
    internal const uint EventObjectNameChange = 0x800C;
    internal const uint EventSystemMinimizeStart = 0x0016;
    internal const uint EventSystemMinimizeEnd = 0x0017;
    internal const int ErrorInsufficientBuffer = 122;
    internal const int GwlExStyle = -20;
    internal const int GclpHicon = -14;
    internal const int GclpHiconSmall = -34;
    internal const nint IconSmall = 0;
    internal const nint IconBig = 1;
    internal const nint IconSmall2 = 2;
    internal const uint MonitorDefaultToNearest = 2;
    internal const uint MonitorDefaultToNull = 0;
    internal const uint ModWin = 0x0008;
    internal const uint ModAlt = 0x0001;
    internal const uint ModControl = 0x0002;
    internal const uint ModShift = 0x0004;
    internal const uint SupportedHotkeyModifiers = ModAlt | ModControl | ModShift | ModWin;
    internal const uint QdcOnlyActivePaths = 2;
    internal const int SwRestore = 9;
    internal const int SwMinimize = 6;
    internal const uint WmSysCommand = 0x0112;
    internal static readonly nint ScClose = 0xF060;
    internal const uint WmGetIcon = 0x007F;
    internal const int WmHotkey = 0x0312;
    internal const uint VkOem3 = 0xC0;
    internal const uint GwOwner = 4;
    internal const long WsExAppWindow = 0x00040000L;
    internal const long WsExToolWindow = 0x00000080L;
    internal const int ObjidWindow = 0;
    internal const int ChildidSelf = 0;
    internal const uint WineventOutofcontext = 0;
    internal const uint WineventSkipownprocess = 2;
    internal const uint SmtoAbortIfHung = 0x0002;

    internal delegate bool EnumWindowsProc(nint windowHandle, nint parameter);
    internal delegate void WinEventProc(
        nint hookHandle,
        uint eventType,
        nint windowHandle,
        int objectId,
        int childId,
        uint eventThreadId,
        uint eventTime);

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

    [StructLayout(LayoutKind.Sequential)]
    internal struct NativePoint
    {
        internal int X;
        internal int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct WindowPosition
    {
        internal nint WindowHandle;
        internal nint InsertAfter;
        internal int X;
        internal int Y;
        internal int Width;
        internal int Height;
        internal uint Flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct WindowPlacement
    {
        internal uint Length;
        internal uint Flags;
        internal uint ShowCommand;
        internal NativePoint MinimumPosition;
        internal NativePoint MaximumPosition;
        internal Rect NormalPosition;
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

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool EnumChildWindows(
        nint parentWindowHandle,
        EnumWindowsProc callback,
        nint parameter);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    internal static extern int GetPackageFamilyName(
        nint processHandle,
        ref uint packageFamilyNameLength,
        [Out] char[]? packageFamilyName);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    internal static extern int GetPackageFullName(
        nint processHandle,
        ref uint packageFullNameLength,
        [Out] char[]? packageFullName);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    internal static extern int GetPackagePathByFullName(
        string packageFullName,
        ref uint pathLength,
        [Out] char[]? path);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool RegisterHotKey(
        nint windowHandle,
        int identifier,
        uint modifiers,
        uint virtualKey);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool UnregisterHotKey(nint windowHandle, int identifier);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern nint SetWinEventHook(
        uint eventMinimum,
        uint eventMaximum,
        nint eventHookModule,
        WinEventProc callback,
        uint processId,
        uint threadId,
        uint flags);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool UnhookWinEvent(nint hookHandle);

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

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetWindowPlacement(
        nint windowHandle,
        ref WindowPlacement placement);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetWindowPlacement(
        nint windowHandle,
        ref WindowPlacement placement);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool PostMessage(
        nint windowHandle,
        uint message,
        nint wParam,
        nint lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern nint SendMessageTimeout(
        nint windowHandle,
        uint message,
        nint wParam,
        nint lParam,
        uint flags,
        uint timeout,
        out nint result);

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

    [DllImport("user32.dll")]
    internal static extern nint GetForegroundWindow();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetCursorPos(out NativePoint point);

    [DllImport("user32.dll")]
    internal static extern nint MonitorFromPoint(NativePoint point, uint flags);

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

    [DllImport("user32.dll", EntryPoint = "GetClassLongW", SetLastError = true)]
    private static extern uint GetClassLong32(nint windowHandle, int index);

    [DllImport("user32.dll", EntryPoint = "GetClassLongPtrW", SetLastError = true)]
    private static extern nint GetClassLongPtr64(nint windowHandle, int index);

    [DllImport("dwmapi.dll")]
    internal static extern int DwmGetWindowAttribute(
        nint windowHandle,
        int attribute,
        out int attributeValue,
        int attributeSize);

    [DllImport("dwmapi.dll")]
    internal static extern int DwmSetWindowAttribute(
        nint windowHandle,
        int attribute,
        ref int attributeValue,
        int attributeSize);

    [DllImport("dwmapi.dll")]
    internal static extern int DwmGetWindowAttribute(
        nint windowHandle,
        int attribute,
        out Rect attributeValue,
        int attributeSize);

    internal static nint GetWindowLongPtr(nint windowHandle, int index)
    {
        return nint.Size == 8
            ? GetWindowLongPtr64(windowHandle, index)
            : GetWindowLong32(windowHandle, index);
    }

    internal static nint GetClassLongPtr(nint windowHandle, int index)
    {
        return nint.Size == 8
            ? GetClassLongPtr64(windowHandle, index)
            : (nint)GetClassLong32(windowHandle, index);
    }
}
