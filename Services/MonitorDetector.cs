using System.Runtime.InteropServices;
using WindowDeck.Interop;

namespace WindowDeck.Services;

internal sealed class MonitorDetector
{
    private const uint DisplayConfigDeviceInfoGetSourceName = 1;
    private Dictionary<string, int> displayNumbers = new(StringComparer.OrdinalIgnoreCase);

    public void RefreshDisplayMapping()
    {
        displayNumbers = GetDisplayNumbers();
    }

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
        if (deviceName is not null
            && displayNumbers.TryGetValue(deviceName, out int displayNumber))
        {
            number = displayNumber;
        }
    }

    private static Dictionary<string, int> GetDisplayNumbers()
    {
        Dictionary<string, int> numbers = new(StringComparer.OrdinalIgnoreCase);

        for (int attempt = 0; attempt < 3; attempt++)
        {
            int result = NativeMethods.GetDisplayConfigBufferSizes(
                NativeMethods.QdcOnlyActivePaths,
                out uint pathCount,
                out uint modeCount);
            if (result != 0)
            {
                return numbers;
            }

            NativeMethods.DisplayConfigPathInfo[] paths = new NativeMethods.DisplayConfigPathInfo[pathCount];
            NativeMethods.DisplayConfigModeInfo[] modes = new NativeMethods.DisplayConfigModeInfo[modeCount];

            result = NativeMethods.QueryDisplayConfig(
                NativeMethods.QdcOnlyActivePaths,
                ref pathCount,
                paths,
                ref modeCount,
                modes,
                0);
            if (result == NativeMethods.ErrorInsufficientBuffer)
            {
                continue;
            }

            if (result != 0)
            {
                return numbers;
            }

            for (int index = 0; index < pathCount; index++)
            {
                NativeMethods.DisplayConfigPathSourceInfo source = paths[index].SourceInfo;
                NativeMethods.DisplayConfigSourceDeviceName sourceName = new()
                {
                    Type = DisplayConfigDeviceInfoGetSourceName,
                    Size = (uint)Marshal.SizeOf<NativeMethods.DisplayConfigSourceDeviceName>(),
                    AdapterId = source.AdapterId,
                    Id = source.Id
                };

                if (NativeMethods.DisplayConfigGetDeviceInfo(ref sourceName) == 0
                    && !string.IsNullOrEmpty(sourceName.ViewGdiDeviceName)
                    && source.Id < int.MaxValue)
                {
                    // Use the display topology's one-based source number, not the unrelated GDI suffix.
                    numbers[sourceName.ViewGdiDeviceName] = (int)source.Id + 1;
                }
            }

            return RemoveAmbiguousNumbers(numbers);
        }

        return numbers;
    }

    private static Dictionary<string, int> RemoveAmbiguousNumbers(Dictionary<string, int> numbers)
    {
        HashSet<int> duplicates = numbers
            .GroupBy(entry => entry.Value)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToHashSet();

        return numbers
            .Where(entry => !duplicates.Contains(entry.Value))
            .ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.OrdinalIgnoreCase);
    }
}
