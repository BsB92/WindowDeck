using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Linq;
using WindowDeck.Interop;
using WindowDeck.Localization;

namespace WindowDeck.Services;

internal sealed class ApplicationNameResolver
{
    private const int AppModelErrorNoPackage = 15700;
    private const int ErrorInsufficientBuffer = 122;
    private static readonly Dictionary<string, string> KnownNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["POWERPNT"] = "PowerPoint",
        ["olk"] = "Outlook",
        ["ms-teams"] = "Microsoft Teams"
    };
    private readonly Dictionary<uint, ProcessMetadata> metadataCache = [];

    public ResolvedApplication Resolve(nint windowHandle, uint windowProcessId)
    {
        uint processId = ResolveHostedProcessId(windowHandle, windowProcessId);
        ProcessMetadata metadata = GetMetadata(processId);
        bool unresolvedFrameHost = IsApplicationFrameHost(metadata.ProcessName);

        string name = FirstUsableName(
            metadata.PackageDisplayName,
            GetKnownName(metadata.ProcessName),
            metadata.FileDescription,
            metadata.ProductName,
            CleanProcessName(metadata.ProcessName)) ?? LocalizationService.Get("Application_Unknown");
        if (unresolvedFrameHost)
        {
            name = LocalizationService.Get("Application_Unknown");
        }

        string applicationId = metadata.PackageFamilyName
            ?? metadata.ExecutablePath
            ?? (string.IsNullOrWhiteSpace(metadata.ProcessName)
                ? $"pid:{windowProcessId}"
                : $"process:{metadata.ProcessName}");

        HashSet<string> titleSuffixes = new(StringComparer.OrdinalIgnoreCase) { name };
        AddUsable(titleSuffixes, metadata.PackageDisplayName);
        AddUsable(titleSuffixes, metadata.FileDescription);
        AddUsable(titleSuffixes, metadata.ProductName);
        AddUsable(titleSuffixes, metadata.ProcessName);
        AddUsable(titleSuffixes, GetKnownName(metadata.ProcessName));
        return new ResolvedApplication(applicationId, name, titleSuffixes);
    }

    private uint ResolveHostedProcessId(nint windowHandle, uint processId)
    {
        if (!IsApplicationFrameHost(GetMetadata(processId).ProcessName))
        {
            return processId;
        }

        uint hostedProcessId = 0;
        NativeMethods.EnumChildWindows(windowHandle, (childHandle, _) =>
        {
            NativeMethods.GetWindowThreadProcessId(childHandle, out uint childProcessId);
            if (childProcessId != 0
                && childProcessId != processId
                && !IsApplicationFrameHost(GetMetadata(childProcessId).ProcessName))
            {
                hostedProcessId = childProcessId;
                return false;
            }

            return true;
        }, 0);
        return hostedProcessId == 0 ? processId : hostedProcessId;
    }

    private ProcessMetadata GetMetadata(uint processId)
    {
        if (metadataCache.TryGetValue(processId, out ProcessMetadata? cached))
        {
            return cached;
        }

        ProcessMetadata metadata;
        try
        {
            using Process process = Process.GetProcessById(checked((int)processId));
            string processName = process.ProcessName;
            string? executablePath = null;
            string? description = null;
            string? productName = null;
            try
            {
                executablePath = process.MainModule?.FileName;
                if (!string.IsNullOrWhiteSpace(executablePath))
                {
                    FileVersionInfo version = FileVersionInfo.GetVersionInfo(executablePath);
                    description = version.FileDescription;
                    productName = version.ProductName;
                }
            }
            catch (Exception exception) when (IsMetadataException(exception))
            {
                // Package identity and the process name can still provide safe fallbacks.
            }

            string? familyName = null;
            string? packageDisplayName = null;
            try
            {
                GetPackageMetadata(process.Handle, out familyName, out packageDisplayName);
            }
            catch (Exception exception) when (IsMetadataException(exception))
            {
                // Executable metadata and the process name remain useful without package access.
            }
            if (processName.Equals("explorer", StringComparison.OrdinalIgnoreCase))
            {
                description = LocalizationService.Get("Application_FileExplorer");
            }

            metadata = new ProcessMetadata(
                processName, executablePath, description, productName, familyName, packageDisplayName);
        }
        catch (Exception exception) when (IsMetadataException(exception))
        {
            metadata = new ProcessMetadata(null, null, null, null, null, null);
        }

        metadataCache[processId] = metadata;
        return metadata;
    }

    private static void GetPackageMetadata(
        nint processHandle,
        out string? familyName,
        out string? displayName)
    {
        familyName = GetPackageString(processHandle, NativeMethods.GetPackageFamilyName);
        string? fullName = GetPackageString(processHandle, NativeMethods.GetPackageFullName);
        displayName = TryReadPackageDisplayName(fullName);
    }

    private static string? GetPackageString(nint processHandle, PackageStringReader reader)
    {
        uint length = 0;
        int result = reader(processHandle, ref length, null);
        if (result == AppModelErrorNoPackage || result != ErrorInsufficientBuffer || length == 0)
        {
            return null;
        }

        char[] buffer = new char[length];
        result = reader(processHandle, ref length, buffer);
        return result == 0 ? new string(buffer, 0, checked((int)length - 1)) : null;
    }

    private static string? TryReadPackageDisplayName(string? packageFullName)
    {
        if (string.IsNullOrWhiteSpace(packageFullName))
        {
            return null;
        }

        uint length = 0;
        int result = NativeMethods.GetPackagePathByFullName(packageFullName, ref length, null);
        if (result != ErrorInsufficientBuffer || length == 0)
        {
            return null;
        }

        char[] buffer = new char[length];
        result = NativeMethods.GetPackagePathByFullName(packageFullName, ref length, buffer);
        if (result != 0)
        {
            return null;
        }

        try
        {
            string path = new(buffer, 0, checked((int)length - 1));
            XDocument manifest = XDocument.Load(Path.Combine(path, "AppxManifest.xml"));
            string? displayName = manifest.Descendants()
                .FirstOrDefault(element => element.Name.LocalName == "Properties")?
                .Elements().FirstOrDefault(element => element.Name.LocalName == "DisplayName")?.Value;
            return displayName is not null
                && !displayName.StartsWith("ms-resource:", StringComparison.OrdinalIgnoreCase)
                ? displayName.Trim()
                : null;
        }
        catch (Exception exception) when (exception is IOException
            or UnauthorizedAccessException
            or System.Xml.XmlException)
        {
            return null;
        }
    }

    private static string? FirstUsableName(params string?[] candidates) =>
        candidates.FirstOrDefault(IsUsableName)?.Trim();

    private static bool IsUsableName(string? value) =>
        !string.IsNullOrWhiteSpace(value) && !IsApplicationFrameHost(value);

    private static bool IsApplicationFrameHost(string? value) =>
        string.Equals(value?.Replace(" ", "", StringComparison.Ordinal),
            "ApplicationFrameHost", StringComparison.OrdinalIgnoreCase);

    private static string? GetKnownName(string? processName) =>
        processName is not null && KnownNames.TryGetValue(processName, out string? name) ? name : null;

    private static string? CleanProcessName(string? processName) => string.IsNullOrWhiteSpace(processName)
        ? null
        : char.ToUpperInvariant(processName[0]) + processName[1..].ToLowerInvariant();

    private static void AddUsable(HashSet<string> values, string? value)
    {
        if (IsUsableName(value)) values.Add(value!.Trim());
    }

    private static bool IsMetadataException(Exception exception) => exception is ArgumentException
        or InvalidOperationException
        or Win32Exception
        or IOException
        or NotSupportedException
        or UnauthorizedAccessException
        or OverflowException;

    private delegate int PackageStringReader(nint processHandle, ref uint length, char[]? value);

    private sealed record ProcessMetadata(
        string? ProcessName,
        string? ExecutablePath,
        string? FileDescription,
        string? ProductName,
        string? PackageFamilyName,
        string? PackageDisplayName);
}

internal sealed record ResolvedApplication(
    string Id,
    string Name,
    IReadOnlySet<string> TitleSuffixes);
