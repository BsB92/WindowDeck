using System.Globalization;
using System.Runtime.InteropServices;
using WindowDeck.Interop;

namespace WindowDeck.Services;

internal sealed class MonitorDetector
{
    private const string DisplayDevicePrefix = @"\\.\DISPLAY";

    public void Detect(nint windowHandle, out string? deviceName, out int? number)
    {
        deviceName = null;
        number = null;

        nint monitorHandle = NativeMethods.MonitorFromWindow(
            windowHandle,
            NativeMethods.MonitorDefaultToNearest);
        if (monitorHandle == 0)
        {
            return;
        }

        NativeMethods.MonitorInfoEx monitorInfo = new()
        {
            Size = (uint)Marshal.SizeOf<NativeMethods.MonitorInfoEx>()
        };

        if (!NativeMethods.GetMonitorInfo(monitorHandle, ref monitorInfo))
        {
            return;
        }

        deviceName = monitorInfo.DeviceName;
        number = ParseDisplayNumber(deviceName);
    }

    private static int? ParseDisplayNumber(string? deviceName)
    {
        if (deviceName is null
            || !deviceName.StartsWith(DisplayDevicePrefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        ReadOnlySpan<char> suffix = deviceName.AsSpan(DisplayDevicePrefix.Length);
        return int.TryParse(
            suffix,
            NumberStyles.None,
            CultureInfo.InvariantCulture,
            out int number)
            && number > 0
                ? number
                : null;
    }
}
