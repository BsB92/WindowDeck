using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Linq;
using WindowDeck.Interop;
using WindowDeck.Localization;

namespace WindowDeck.Services;

internal sealed record ApplicationIdentity(
    string StableId,
    string DisplayName,
    IReadOnlyList<string> TitleAliases);

internal static class ApplicationNameResolver
{
    private static readonly IReadOnlyDictionary<string, string> KnownAliases =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["POWERPNT"] = "PowerPoint",
            ["olk"] = "Outlook",
            ["ms-teams"] = "Microsoft Teams"
        };

    public static ApplicationIdentity Resolve(nint windowHandle, uint processId)
    {
        // ApplicationFrameHost owns the outer window, while the packaged application's
        // CoreWindow normally appears below it with the actual process identity.
        ProcessDetails details = ReadDetails(processId);
        if (details.ProcessName.Equals("ApplicationFrameHost", StringComparison.OrdinalIgnoreCase)
            && TryFindHostedProcess(windowHandle, processId, out uint hostedProcessId))
        {
            ProcessDetails hostedDetails = ReadDetails(hostedProcessId);
            if (!hostedDetails.IsUnknown)
            {
                details = hostedDetails;
            }
        }

        string displayName = FirstUsefulName(details.PackageDisplayName)
            ?? (KnownAliases.TryGetValue(details.ProcessName, out string? alias) ? alias : null)
            ?? FirstUsefulName(details.FileDescription, details.ProductName)
            ?? CleanProcessName(details.ProcessName);
        if (details.ProcessName.Equals("explorer", StringComparison.OrdinalIgnoreCase))
        {
            displayName = LocalizationService.Get("Application_FileExplorer");
        }

        string stableId = !string.IsNullOrWhiteSpace(details.PackageFamilyName)
            ? $"package:{details.PackageFamilyName}"
            : !string.IsNullOrWhiteSpace(details.ExecutablePath)
                ? $"exe:{details.ExecutablePath.ToUpperInvariant()}"
                : $"process:{details.ProcessName.ToUpperInvariant()}";
        string[] titleAliases = new[]
            {
                displayName, details.ProcessName, details.FileDescription, details.ProductName,
                KnownAliases.TryGetValue(details.ProcessName, out string? knownAlias) ? knownAlias : null,
                details.ProcessName.Equals("POWERPNT", StringComparison.OrdinalIgnoreCase)
                    ? "Microsoft PowerPoint" : null
            }
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return new ApplicationIdentity(stableId, displayName, titleAliases);
    }

    private static ProcessDetails ReadDetails(uint processId)
    {
        try
        {
            using Process process = Process.GetProcessById(checked((int)processId));
            string processName = process.ProcessName;
            ReadPackageDetails(processId, out string? familyName, out string? displayName);
            try
            {
                string? path = process.MainModule?.FileName;
                FileVersionInfo? version = string.IsNullOrWhiteSpace(path)
                    ? null : FileVersionInfo.GetVersionInfo(path);
                return new(processName, path, NormalizeMetadata(version?.FileDescription),
                    NormalizeMetadata(version?.ProductName), familyName, displayName);
            }
            catch (Exception exception) when (exception is InvalidOperationException or Win32Exception
                or IOException or NotSupportedException or UnauthorizedAccessException)
            {
                return new(processName, null, null, null, familyName, displayName);
            }
        }
        catch (Exception exception) when (exception is ArgumentException or OverflowException
            or InvalidOperationException or Win32Exception or IOException
            or NotSupportedException or UnauthorizedAccessException)
        {
            return ProcessDetails.Unknown;
        }
    }

    private static void ReadPackageDetails(uint processId, out string? familyName, out string? displayName)
    {
        familyName = null;
        displayName = null;
        nint processHandle = NativeMethods.OpenProcess(NativeMethods.ProcessQueryLimitedInformation,
            false, processId);
        if (processHandle == 0)
        {
            return;
        }

        try
        {
            familyName = ReadPackageValue(processHandle, NativeMethods.GetPackageFamilyName);
            string? fullName = ReadPackageValue(processHandle, NativeMethods.GetPackageFullName);
            if (string.IsNullOrWhiteSpace(fullName))
            {
                return;
            }

            uint pathLength = 0;
            int result = NativeMethods.GetPackagePathByFullName(fullName, ref pathLength, null);
            if (result != NativeMethods.ErrorInsufficientBuffer || pathLength == 0)
            {
                return;
            }

            char[] pathBuffer = new char[pathLength];
            if (NativeMethods.GetPackagePathByFullName(fullName, ref pathLength, pathBuffer) != 0)
            {
                return;
            }

            string manifestPath = Path.Combine(new string(pathBuffer).TrimEnd('\0'), "AppxManifest.xml");
            XElement? displayNameElement = XDocument.Load(manifestPath).Root?
                .Elements().FirstOrDefault(element => element.Name.LocalName == "Properties")?
                .Elements().FirstOrDefault(element => element.Name.LocalName == "DisplayName");
            string? manifestName = displayNameElement?.Value.Trim();
            if (!string.IsNullOrWhiteSpace(manifestName)
                && !manifestName.StartsWith("ms-resource:", StringComparison.OrdinalIgnoreCase))
            {
                displayName = manifestName;
            }
        }
        catch (Exception exception) when (exception is ArgumentException or IOException
            or NotSupportedException or UnauthorizedAccessException or System.Xml.XmlException)
        {
            // Package metadata is an optional enhancement; keep enumerating other windows.
        }
        finally
        {
            NativeMethods.CloseHandle(processHandle);
        }
    }

    private static string? ReadPackageValue(nint processHandle, NativeMethods.PackageNameReader reader)
    {
        uint length = 0;
        int result = reader(processHandle, ref length, null);
        if (result != NativeMethods.ErrorInsufficientBuffer || length == 0)
        {
            return null;
        }

        char[] buffer = new char[length];
        return reader(processHandle, ref length, buffer) == 0
            ? new string(buffer).TrimEnd('\0') : null;
    }

    private static bool TryFindHostedProcess(nint windowHandle, uint hostProcessId, out uint processId)
    {
        uint foundProcessId = 0;
        NativeMethods.EnumChildWindows(windowHandle, (childHandle, _) =>
        {
            NativeMethods.GetWindowThreadProcessId(childHandle, out uint childProcessId);
            if (childProcessId != 0 && childProcessId != hostProcessId)
            {
                foundProcessId = childProcessId;
                return false;
            }

            return true;
        }, 0);
        processId = foundProcessId;
        return foundProcessId != 0;
    }

    private static string? FirstUsefulName(params string?[] names) => names.FirstOrDefault(name =>
        !string.IsNullOrWhiteSpace(name) && !name.Equals("ApplicationFrameHost",
            StringComparison.OrdinalIgnoreCase));

    private static string? NormalizeMetadata(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string CleanProcessName(string processName) => string.IsNullOrWhiteSpace(processName)
        || processName.Equals("ApplicationFrameHost", StringComparison.OrdinalIgnoreCase)
        ? LocalizationService.Get("Application_Unknown")
        : char.ToUpperInvariant(processName[0]) + processName[1..];

    private sealed record ProcessDetails(string ProcessName, string? ExecutablePath,
        string? FileDescription, string? ProductName, string? PackageFamilyName,
        string? PackageDisplayName)
    {
        internal static ProcessDetails Unknown { get; } = new("Unknown", null, null, null, null, null);
        internal bool IsUnknown => ReferenceEquals(this, Unknown);
    }
}
