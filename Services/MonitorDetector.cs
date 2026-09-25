using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using WindowDeck.Interop;
using WindowDeck.Models;

namespace WindowDeck.Services;

internal sealed class MonitorDetector
{
    private const uint DisplayConfigDeviceInfoGetSourceName = 1;
    private const uint DisplayConfigDeviceInfoGetTargetName = 2;
    private const string DisplayDevicePrefix = @"\\.\DISPLAY";
    private Dictionary<string, int> displayNumbers = new(StringComparer.OrdinalIgnoreCase);

    public void RefreshDisplayMapping(AppSettings settings)
    {
        _ = GetDisplays(settings);
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

    public IReadOnlyList<MonitorDisplay> GetDisplays(AppSettings settings)
    {
        Dictionary<string, DisplayPathDetails> pathDetails = GetDisplayPathDetails();
        MonitorDisplay[] baseDisplays = Screen.AllScreens
            .Select(screen =>
            {
                pathDetails.TryGetValue(screen.DeviceName, out DisplayPathDetails? path);
                int? windowsNumber = path?.WindowsNumber ?? TryParseWindowsDisplayNumber(screen.DeviceName);
                string displayName = !string.IsNullOrWhiteSpace(path?.FriendlyName)
                    ? path.FriendlyName
                    : screen.DeviceName;

                return new MonitorDisplay(
                    screen.DeviceName,
                    path?.StableId,
                    displayName,
                    windowsNumber,
                    windowsNumber,
                    screen.WorkingArea);
            })
            .Where(display => display.WindowsNumber.HasValue)
            .ToArray();

        string? configurationId = GetConfigurationId(baseDisplays);
        Dictionary<string, int>? customNumbers = null;
        if (configurationId is not null
            && settings.ScreenNumberingConfigurations.TryGetValue(
                configurationId,
                out Dictionary<string, int> savedNumbers)
            && IsValidCustomNumbering(baseDisplays, savedNumbers))
        {
            customNumbers = savedNumbers;
        }

        MonitorDisplay[] resolvedDisplays = baseDisplays
            .Select(display =>
            {
                int? number = display.WindowsNumber;
                if (customNumbers is not null
                    && display.StableId is not null
                    && customNumbers.TryGetValue(display.StableId, out int customNumber))
                {
                    number = customNumber;
                }

                return display with { Number = number };
            })
            .OrderBy(display => display.Number ?? int.MaxValue)
            .ThenBy(display => display.WindowsNumber ?? int.MaxValue)
            .ThenBy(display => display.DeviceName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        displayNumbers = resolvedDisplays
            .Where(display => display.Number.HasValue)
            .ToDictionary(
                display => display.DeviceName,
                display => display.Number!.Value,
                StringComparer.OrdinalIgnoreCase);

        return resolvedDisplays;
    }

    public static string? GetConfigurationId(IReadOnlyList<MonitorDisplay> displays)
    {
        if (displays.Count == 0
            || displays.Any(display => string.IsNullOrWhiteSpace(display.StableId)))
        {
            return null;
        }

        string[] stableIds = displays
            .Select(display => display.StableId!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (stableIds.Length != displays.Count)
        {
            return null;
        }

        string fingerprint = string.Join("\n", stableIds);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(fingerprint)));
    }

    private static bool IsValidCustomNumbering(
        IReadOnlyList<MonitorDisplay> displays,
        IReadOnlyDictionary<string, int> savedNumbers)
    {
        if (savedNumbers.Count != displays.Count
            || savedNumbers.Values.Distinct().Count() != displays.Count)
        {
            return false;
        }

        int screenCount = displays.Count;
        foreach (MonitorDisplay display in displays)
        {
            if (display.StableId is null
                || !savedNumbers.TryGetValue(display.StableId, out int number)
                || number < 1
                || number > screenCount)
            {
                return false;
            }
        }

        return true;
    }

    private static int? TryParseWindowsDisplayNumber(string deviceName)
    {
        if (!deviceName.StartsWith(DisplayDevicePrefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return int.TryParse(deviceName.AsSpan(DisplayDevicePrefix.Length), out int number)
            && number > 0
            ? number
            : null;
    }

    private static Dictionary<string, DisplayPathDetails> GetDisplayPathDetails()
    {
        Dictionary<string, DisplayPathDetails> details =
            new(StringComparer.OrdinalIgnoreCase);

        for (int attempt = 0; attempt < 3; attempt++)
        {
            int result = NativeMethods.GetDisplayConfigBufferSizes(
                NativeMethods.QdcOnlyActivePaths,
                out uint pathCount,
                out uint modeCount);
            if (result != 0)
            {
                return details;
            }

            NativeMethods.DisplayConfigPathInfo[] paths =
                new NativeMethods.DisplayConfigPathInfo[pathCount];
            NativeMethods.DisplayConfigModeInfo[] modes =
                new NativeMethods.DisplayConfigModeInfo[modeCount];

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
                return details;
            }

            for (int index = 0; index < pathCount; index++)
            {
                NativeMethods.DisplayConfigPathInfo path = paths[index];
                NativeMethods.DisplayConfigPathSourceInfo source = path.SourceInfo;
                NativeMethods.DisplayConfigSourceDeviceName sourceName = new()
                {
                    Type = DisplayConfigDeviceInfoGetSourceName,
                    Size = (uint)Marshal.SizeOf<NativeMethods.DisplayConfigSourceDeviceName>(),
                    AdapterId = source.AdapterId,
                    Id = source.Id
                };

                if (NativeMethods.DisplayConfigGetDeviceInfo(ref sourceName) != 0
                    || string.IsNullOrWhiteSpace(sourceName.ViewGdiDeviceName))
                {
                    continue;
                }

                NativeMethods.DisplayConfigPathTargetInfo target = path.TargetInfo;
                NativeMethods.DisplayConfigTargetDeviceName targetName = new()
                {
                    Type = DisplayConfigDeviceInfoGetTargetName,
                    Size = (uint)Marshal.SizeOf<NativeMethods.DisplayConfigTargetDeviceName>(),
                    AdapterId = target.AdapterId,
                    Id = target.Id
                };

                string? stableId = null;
                string? friendlyName = null;
                if (NativeMethods.DisplayConfigGetDeviceInfo(ref targetName) == 0)
                {
                    if (!string.IsNullOrWhiteSpace(targetName.MonitorDevicePath))
                    {
                        stableId = targetName.MonitorDevicePath.Trim().ToUpperInvariant();
                    }

                    if (!string.IsNullOrWhiteSpace(targetName.MonitorFriendlyDeviceName))
                    {
                        friendlyName = targetName.MonitorFriendlyDeviceName.Trim();
                    }
                }

                details.TryAdd(
                    sourceName.ViewGdiDeviceName,
                    new DisplayPathDetails(index + 1, stableId, friendlyName));
            }

            return details;
        }

        return details;
    }

    private sealed record DisplayPathDetails(
        int WindowsNumber,
        string? StableId,
        string? FriendlyName);
}

internal sealed record MonitorDisplay(
    string DeviceName,
    string? StableId,
    string DisplayName,
    int? WindowsNumber,
    int? Number,
    Rectangle WorkArea);
