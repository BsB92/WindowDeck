using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace WindowDeck.Services;

// Windows does not expose a universal document-path property per top-level HWND.
// Resolve only an UNAMBIGUOUS, existing file whose complete filename matches the
// displayed document/window title. Never search the user's entire disk.
internal sealed class RecentDocumentResolver
{
    private readonly Lazy<IReadOnlyList<string>> candidates = new(ReadCandidates);

    private static readonly HashSet<string> DocumentExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".txt", ".md", ".log", ".rtf", ".csv", ".json", ".xml",
            ".doc", ".docx", ".docm", ".xls", ".xlsx", ".xlsm", ".xlsb",
            ".ppt", ".pptx", ".pptm", ".odt", ".ods", ".odp", ".dwg",
            ".dxf", ".svg", ".html", ".htm", ".ini", ".cfg", ".yaml", ".yml"
        };

    public string? Resolve(string windowTitle, string processName)
    {
        if (string.IsNullOrWhiteSpace(windowTitle) || IsBrowser(processName))
        {
            return null;
        }

        string[] matches = candidates.Value
            .Where(path => TitleMatchesFile(windowTitle, Path.GetFileName(path)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(2)
            .ToArray();

        // A repeated filename in two directories is genuinely ambiguous.
        // In that case, require explicit selection rather than guessing.
        return matches.Length == 1 ? matches[0] : null;
    }

    public static bool LooksLikeDocumentWindow(string windowTitle, string processName)
    {
        if (IsBrowser(processName))
        {
            return false;
        }

        string name = processName.ToLowerInvariant();
        if (name is "notepad" or "notepad++" or "foxitreader" or "foxitpdfreader"
            or "foxitpdfeditor" or "acrord32" or "acrobat" or "sumatrapdf"
            or "pdfxedit" or "nitropdf" or "pdfxchange")
        {
            return true;
        }

        return DocumentExtensions.Any(extension =>
            Regex.IsMatch(windowTitle,
                Regex.Escape(extension) + @"(?=$|[\s\-–—\]\)])",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant));
    }

    private static bool IsBrowser(string processName) =>
        processName.Equals("chrome", StringComparison.OrdinalIgnoreCase)
        || processName.Equals("firefox", StringComparison.OrdinalIgnoreCase)
        || processName.Equals("msedge", StringComparison.OrdinalIgnoreCase)
        || processName.Equals("brave", StringComparison.OrdinalIgnoreCase)
        || processName.Equals("opera", StringComparison.OrdinalIgnoreCase)
        || processName.Equals("vivaldi", StringComparison.OrdinalIgnoreCase);

    private static bool TitleMatchesFile(string title, string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)
            || !DocumentExtensions.Contains(Path.GetExtension(fileName)))
        {
            return false;
        }

        // Match the complete basename, never a substring of another basename.
        // Some document programs show "document" instead of "document.pdf".
        bool fullName = Regex.IsMatch(title,
            @"(?<![\p{L}\p{N}._-])" + Regex.Escape(fileName)
                + @"(?![\p{L}\p{N}._-])",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (fullName)
        {
            return true;
        }

        string withoutExtension = Path.GetFileNameWithoutExtension(fileName);
        return string.Equals(title.Trim(), withoutExtension,
            StringComparison.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<string> ReadCandidates()
    {
        HashSet<string> paths = new(StringComparer.OrdinalIgnoreCase);
        ReadWindowsRecentLinks(paths);
        ReadFoxitRecentDocuments(paths);
        return paths.ToArray();
    }

    private static void ReadWindowsRecentLinks(ISet<string> paths)
    {
        string recentDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Recent);
        if (!Directory.Exists(recentDirectory))
        {
            return;
        }

        object? shell = null;
        object? folder = null;
        try
        {
            Type? type = Type.GetTypeFromProgID("Shell.Application");
            if (type is null)
            {
                return;
            }

            shell = Activator.CreateInstance(type);
            if (shell is null)
            {
                return;
            }

            dynamic shellObject = shell;
            folder = shellObject.NameSpace(recentDirectory);
            if (folder is null)
            {
                return;
            }

            dynamic recentFolder = folder;
            foreach (FileInfo shortcut in new DirectoryInfo(recentDirectory)
                .EnumerateFiles("*.lnk", SearchOption.TopDirectoryOnly)
                .OrderByDescending(file => file.LastWriteTimeUtc)
                .Take(300))
            {
                try
                {
                    dynamic item = recentFolder.ParseName(shortcut.Name);
                    if (item is null)
                    {
                        continue;
                    }

                    dynamic link = item.GetLink;
                    if (link is null)
                    {
                        continue;
                    }

                    AddFile(paths, (string)link.Path);
                }
                catch (Exception error) when (error is not OutOfMemoryException)
                {
                    // Corrupt, inaccessible or non-document shortcuts are ignored.
                }
            }
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            // Missing shell history or restricted COM must not block saving.
        }
        finally
        {
            ReleaseCom(folder);
            ReleaseCom(shell);
        }
    }

    private static void ReadFoxitRecentDocuments(ISet<string> paths)
    {
        // Some Foxit versions keep their own history instead of registering every
        // opened PDF with Windows Recent. This is optional, read-only, and
        // deliberately never accepts ambiguous filename matches.
        string[] locations =
        [
            @"Software\Foxit Software\Foxit PDF Reader\Continuous\MRU\File MRU",
            @"Software\Foxit Software\Foxit Reader\Continuous\MRU\File MRU"
        ];

        foreach (string location in locations)
        {
            try
            {
                using RegistryKey? key = Registry.CurrentUser.OpenSubKey(location);
                if (key is null)
                {
                    continue;
                }

                foreach (string name in key.GetValueNames().Take(200))
                {
                    if (key.GetValue(name) is not string value)
                    {
                        continue;
                    }

                    // The Foxit MRU value may contain metadata preceding the
                    // absolute path. Only extract valid existing PDF paths.
                    foreach (Match match in Regex.Matches(value,
                        @"[A-Za-z]:\\(?:[^<>|?*""\r\n\\]+\\)*[^<>|?*""\r\n\\]+?\.pdf",
                        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                    {
                        AddFile(paths, match.Value);
                    }
                }
            }
            catch (Exception error) when (error is not OutOfMemoryException)
            {
                // An unsupported Foxit version can still use Windows history or
                // explicit user selection.
            }
        }
    }

    private static void AddFile(ISet<string> paths, string? target)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(target)
                && Path.IsPathFullyQualified(target)
                && DocumentExtensions.Contains(Path.GetExtension(target))
                && File.Exists(target))
            {
                paths.Add(Path.GetFullPath(target));
            }
        }
        catch (Exception error) when (error is ArgumentException
            or IOException or UnauthorizedAccessException or NotSupportedException)
        {
            // Invalid/restricted shortcut targets are not trusted.
        }
    }

    private static void ReleaseCom(object? value)
    {
        if (value is null)
        {
            return;
        }

        try
        {
            if (Marshal.IsComObject(value))
            {
                Marshal.ReleaseComObject(value);
            }
        }
        catch (COMException)
        {
            // Ignore a shell object that Windows has already released.
        }
    }
}
