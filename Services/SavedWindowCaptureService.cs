using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using WindowDeck.Interop;
using WindowDeck.Models;

namespace WindowDeck.Services;

// Capture only resources that can be associated with an exact HWND through
// documented Shell/Office automation. A window title is NOT a file path.
internal sealed class SavedWindowCaptureService
{
    private readonly Dictionary<string, Dictionary<long, string?>> officeWindows =
        new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<long, string?>? explorerWindows;

    public SavedWindowEntry Capture(WindowInfo window)
    {
        SavedWindowEntry entry = new()
        {
            ApplicationName = window.ApplicationName,
            WindowTitle = window.DisplayTitle
        };

        if (window.OwnerWindow.HasValue)
        {
            return entry; // Transient dialog/tool window, not a restorable document.
        }

        string process;
        try
        {
            using Process instance = Process.GetProcessById(checked((int)window.ProcessId));
            process = instance.ProcessName;
        }
        catch (Exception error) when (error is ArgumentException
            or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return entry;
        }

        if (process.Equals("explorer", StringComparison.OrdinalIgnoreCase))
        {
            explorerWindows ??= GetExplorerWindows();
            if (explorerWindows.TryGetValue(window.Handle.ToInt64(), out string? folder)
                && folder is not null
                && Directory.Exists(folder))
            {
                entry.Kind = SavedWindowKind.ExplorerFolder;
                entry.ResourcePath = folder;
            }
            return entry;
        }

        (string progId, SavedWindowKind kind)? office = process.ToUpperInvariant() switch
        {
            "EXCEL" => ("Excel.Application", SavedWindowKind.ExcelWorkbook),
            "WINWORD" => ("Word.Application", SavedWindowKind.WordDocument),
            "POWERPNT" => ("PowerPoint.Application", SavedWindowKind.PowerPointPresentation),
            _ => null
        };
        if (office is null)
        {
            return entry;
        }

        (string progId, SavedWindowKind kind) = office.Value;
        if (!officeWindows.TryGetValue(progId, out Dictionary<long, string?>? documentWindows))
        {
            documentWindows = GetOfficeWindows(progId);
            officeWindows[progId] = documentWindows;
        }

        if (documentWindows.TryGetValue(window.Handle.ToInt64(), out string? file)
            && IsSupportedDocument(kind, file))
        {
            entry.Kind = kind;
            entry.ResourcePath = file;
        }

        return entry;
    }

    public static bool IsSupportedDocument(SavedWindowKind kind, string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        string extension;
        try
        {
            if (!Path.IsPathFullyQualified(path))
            {
                return false;
            }

            extension = Path.GetExtension(path);
        }
        catch (Exception error) when (error is ArgumentException
            or NotSupportedException or PathTooLongException)
        {
            return false;
        }

        if (kind == SavedWindowKind.ExplorerFolder)
        {
            return true;
        }
        return kind switch
        {
            SavedWindowKind.ExcelWorkbook => extension.Equals(".xlsx", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".xlsm", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".xlsb", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".xls", StringComparison.OrdinalIgnoreCase),
            SavedWindowKind.WordDocument => extension.Equals(".docx", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".docm", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".doc", StringComparison.OrdinalIgnoreCase),
            SavedWindowKind.PowerPointPresentation => extension.Equals(".pptx", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".pptm", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".ppt", StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }

    private static Dictionary<long, string?> GetExplorerWindows()
    {
        Dictionary<long, string?> paths = [];
        object? instance = null;
        try
        {
            Type? shellType = Type.GetTypeFromProgID("Shell.Application");
            if (shellType is null)
            {
                return paths;
            }

            instance = Activator.CreateInstance(shellType);
            if (instance is null)
            {
                return paths;
            }

            dynamic shell = instance;
            foreach (object window in shell.Windows())
            {
                try
                {
                    dynamic view = window;
                    long hwnd = Convert.ToInt64(view.HWND, CultureInfo.InvariantCulture);
                    string path = (string)view.Document.Folder.Self.Path;
                    if (!Directory.Exists(path))
                    {
                        continue;
                    }

                    AddUnambiguous(paths, hwnd, path);
                }
                catch (Exception error) when (error is not OutOfMemoryException)
                {
                    // Explorer tabs, virtual folders and non-Explorer shell windows
                    // cannot always be resolved unambiguously.
                }
            }
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            // No Shell automation in this session.
        }
        finally
        {
            if (instance is not null && Marshal.IsComObject(instance))
            {
                try { Marshal.ReleaseComObject(instance); }
                catch (COMException) { /* Ignore released automation instances. */ }
            }
        }

        return paths;
    }

    private static Dictionary<long, string?> GetOfficeWindows(string progId)
    {
        Dictionary<long, string?> paths = [];
        object? instance = null;
        try
        {
            if (NativeMethods.CLSIDFromProgID(progId, out Guid clsid) != 0
                || NativeMethods.GetActiveObject(ref clsid, 0, out object runningApp) != 0)
            {
                return paths;
            }

            instance = runningApp;
            dynamic app = runningApp;
            foreach (object officeWindow in app.Windows)
            {
                try
                {
                    dynamic view = officeWindow;
                    long hwnd;
                    string file;
                    if (progId.Equals("Excel.Application", StringComparison.OrdinalIgnoreCase))
                    {
                        hwnd = Convert.ToInt64(view.Hwnd, CultureInfo.InvariantCulture);
                        file = (string)view.ActiveSheet.Parent.FullName;
                    }
                    else if (progId.Equals("Word.Application", StringComparison.OrdinalIgnoreCase))
                    {
                        hwnd = Convert.ToInt64(view.Hwnd, CultureInfo.InvariantCulture);
                        file = (string)view.Document.FullName;
                    }
                    else
                    {
                        hwnd = Convert.ToInt64(view.HWND, CultureInfo.InvariantCulture);
                        file = (string)view.Presentation.FullName;
                    }

                    if (Path.IsPathFullyQualified(file))
                    {
                        AddUnambiguous(paths, hwnd, file);
                    }
                }
                catch (Exception error) when (error is not OutOfMemoryException)
                {
                    // Unsupported Office window/state; never guess its target.
                }
            }
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            // Office not installed/running, or this instance is not available
            // through the ROT. Unsupported windows are clearly marked.
        }
        finally
        {
            if (instance is not null && Marshal.IsComObject(instance))
            {
                try { Marshal.ReleaseComObject(instance); }
                catch (COMException) { /* Ignore released automation instances. */ }
            }
        }

        return paths;
    }

    private static void AddUnambiguous(Dictionary<long, string?> targets, long hwnd, string path)
    {
        if (targets.TryGetValue(hwnd, out string? previous))
        {
            if (!string.Equals(previous, path, StringComparison.OrdinalIgnoreCase))
            {
                targets[hwnd] = null;
            }
        }
        else
        {
            targets.Add(hwnd, path);
        }
    }
}
