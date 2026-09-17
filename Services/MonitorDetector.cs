using System.Runtime.InteropServices;
using WindowDeck.Interop;

namespace WindowDeck.Services;

internal sealed class MonitorDetector
{
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
    }
}
