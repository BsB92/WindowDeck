using System.Runtime.InteropServices;
using WindowDeck.Localization;
using WindowDeck.Models;
using WindowDeck.Services;

namespace WindowDeck;

// Read-only, grouped and theme-aware preview of the exact files saved in a set.
// No ListView, column headers, GDI bitmaps, or owner-drawn table backgrounds.
internal sealed class SavedSetGroupedView : UserControl
{
    private const int GroupHeaderHeight = 37;
    private const int DocumentRowHeight = 64;
    private const int GroupGap = 13;
    private const int ItemGap = 3;
    private const int StatusWidth = 132;
    private const int CopyButtonSize = 24;

    private sealed record DocumentRow(
        Panel Container,
        Label Title,
        Label Path,
        Button? CopyPath,
        SavedWindowStatusIcon StatusIcon,
        Label StatusText,
        Panel Divider);

    private sealed record ApplicationGroup(
        string Name,
        Button Header,
        List<DocumentRow> Rows);

    private readonly ThemePalette palette;
    private readonly Panel viewport;
    private readonly Panel canvas;
    private readonly ToolTip toolTip = new();
    private readonly List<ApplicationGroup> groups = [];
    private readonly HashSet<string> collapsed = new(StringComparer.OrdinalIgnoreCase);
    private readonly Font headingFont;
    private readonly Font titleFont;
    private readonly Font pathFont;
    private readonly Font stateFont;
    private bool isUpdatingLayout;

    public SavedSetGroupedView(ThemePalette palette)
    {
        this.palette = palette;
        BackColor = palette.Background;
        Font = new Font("Segoe UI", 9F);
        headingFont = new Font(Font, FontStyle.Bold);
        titleFont = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        pathFont = new Font("Segoe UI", 8.5F);
        stateFont = new Font("Segoe UI", 8.5F);

        viewport = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = palette.Background
        };
        canvas = new Panel
        {
            BackColor = palette.Background,
            Location = Point.Empty
        };
        viewport.Controls.Add(canvas);
        Controls.Add(viewport);
        viewport.Resize += (_, _) => LayoutGroups();
    }

    public void ShowEntries(IReadOnlyList<SavedWindowEntry> entries)
    {
        viewport.AutoScrollPosition = Point.Empty;
        canvas.SuspendLayout();
        try
        {
            // Explicitly dispose controls when changing sets; Controls.Clear()
            // alone does not guarantee disposal of descendant controls.
            while (canvas.Controls.Count != 0)
            {
                Control child = canvas.Controls[0];
                canvas.Controls.RemoveAt(0);
                child.Dispose();
            }

            groups.Clear();
            foreach (IGrouping<string, SavedWindowEntry> group in entries
                .GroupBy(entry => entry.ApplicationName)
                .OrderBy(group => group.Key, StringComparer.CurrentCultureIgnoreCase))
            {
                string groupName = group.Key;
                Button heading = new()
                {
                    Height = GroupHeaderHeight,
                    BackColor = palette.RaisedSurface,
                    ForeColor = palette.Foreground,
                    Font = headingFont,
                    FlatStyle = FlatStyle.Flat,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Padding = new Padding(11, 0, 8, 0),
                    TabStop = true,
                    UseVisualStyleBackColor = false,
                    AccessibleDescription = LocalizationService.Get("Sets_PreviewCollapseHelp")
                };
                heading.FlatAppearance.BorderSize = 0;
                heading.FlatAppearance.MouseOverBackColor = palette.Hover;
                heading.FlatAppearance.MouseDownBackColor = palette.Pressed;

                List<DocumentRow> rows = [];
                foreach (SavedWindowEntry entry in group)
                {
                    rows.Add(CreateDocumentRow(entry));
                }

                ApplicationGroup section = new(groupName, heading, rows);
                heading.Click += (_, _) =>
                {
                    if (!collapsed.Add(groupName))
                    {
                        collapsed.Remove(groupName);
                    }
                    LayoutGroups();
                };
                groups.Add(section);
                canvas.Controls.Add(heading);
                foreach (DocumentRow row in rows)
                {
                    canvas.Controls.Add(row.Container);
                }
            }
        }
        finally
        {
            canvas.ResumeLayout(false);
        }

        LayoutGroups();
    }

    private DocumentRow CreateDocumentRow(SavedWindowEntry entry)
    {
        Panel row = new()
        {
            BackColor = palette.Surface,
            Height = DocumentRowHeight,
            Margin = Padding.Empty,
            Cursor = Cursors.Default
        };

        Label title = new()
        {
            Text = entry.WindowTitle,
            Font = titleFont,
            ForeColor = palette.Foreground,
            BackColor = palette.Surface,
            AutoEllipsis = true,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoSize = false
        };

        string? resource = entry.ResourcePath ?? entry.ExecutablePath;
        Label path = new()
        {
            Text = string.IsNullOrWhiteSpace(resource)
                ? LocalizationService.Get("Sets_PreviewNoPath")
                : CompactPath(resource),
            Font = pathFont,
            ForeColor = palette.SecondaryForeground,
            BackColor = palette.Surface,
            AutoEllipsis = true,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoSize = false
        };

        bool ready = IsRestorable(entry);
        SavedWindowStatusIcon statusIcon = new()
        {
            IsReady = ready,
            BackColor = palette.Surface
        };
        string stateKey = entry.Kind switch
        {
            SavedWindowKind.Application when ready => "Sets_PreviewProgramOnly",
            SavedWindowKind.Application => "Sets_PreviewUnavailable",
            SavedWindowKind.Unsupported => "Sets_PreviewUnavailable",
            _ when ready => "Sets_PreviewReady",
            _ => "Sets_PreviewUnavailable"
        };
        Label state = new()
        {
            Text = LocalizationService.Get(stateKey),
            Font = stateFont,
            ForeColor = ready ? palette.SecondaryForeground
                : Color.FromArgb(211, 58, 58),
            BackColor = palette.Surface,
            AutoEllipsis = true,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft
        };

        Button? copyPath = null;
        void CopyFullPath()
        {
            if (string.IsNullOrWhiteSpace(resource))
            {
                return;
            }

            try
            {
                Clipboard.SetText(resource);
            }
            catch (Exception error) when (error is ExternalException
                or System.Threading.ThreadStateException)
            {
                // Clipboard access may temporarily be unavailable.
            }
        }

        if (!string.IsNullOrWhiteSpace(resource))
        {
            copyPath = new Button
            {
                AccessibleName = LocalizationService.Get("Sets_CopyPath"),
                Text = "\uE8C8",
                Font = new Font("Segoe MDL2 Assets", 10F),
                TextAlign = ContentAlignment.MiddleCenter,
                Padding = Padding.Empty,
                TabStop = true
            };
            ThemeManager.StyleButton(copyPath, palette);
            copyPath.Click += (_, _) => CopyFullPath();
            toolTip.SetToolTip(copyPath, copyPath.AccessibleName);
        }

        string fullPathHint = string.IsNullOrWhiteSpace(resource)
            ? LocalizationService.Get("Sets_PreviewNoPath")
            : resource;
        toolTip.SetToolTip(title, entry.WindowTitle);
        toolTip.SetToolTip(path, fullPathHint);
        string stateHint = LocalizationService.Get(entry.Kind == SavedWindowKind.Application && ready
            ? "Sets_StatusProgramOnly"
            : ready ? "Sets_StatusReady" : "Sets_StatusMissing");
        toolTip.SetToolTip(statusIcon, stateHint);
        toolTip.SetToolTip(state, stateHint);

        Panel divider = new()
        {
            BackColor = palette.Border,
            Height = 1
        };
        row.Controls.Add(title);
        row.Controls.Add(path);
        if (copyPath is not null)
        {
            row.Controls.Add(copyPath);
        }
        row.Controls.Add(statusIcon);
        row.Controls.Add(state);
        row.Controls.Add(divider);

        if (!string.IsNullOrWhiteSpace(resource))
        {
            ContextMenuStrip menu = new();
            menu.Items.Add(LocalizationService.Get("Sets_CopyPath"),
                null, (_, _) => CopyFullPath());
            row.ContextMenuStrip = menu;
            title.ContextMenuStrip = menu;
            path.ContextMenuStrip = menu;
            row.Disposed += (_, _) => menu.Dispose();
        }

        return new DocumentRow(row, title, path, copyPath, statusIcon, state, divider);
    }

    private void LayoutGroups()
    {
        if (isUpdatingLayout || viewport.IsDisposed)
        {
            return;
        }

        isUpdatingLayout = true;
        try
        {
            int width = Math.Max(180, viewport.ClientSize.Width
                - SystemInformation.VerticalScrollBarWidth - 5);
            canvas.SuspendLayout();
            try
            {
                canvas.Width = width;
                int y = 0;
                foreach (ApplicationGroup group in groups)
                {
                    bool expanded = !collapsed.Contains(group.Name);
                    group.Header.Text = (expanded ? "▾  " : "▸  ") + group.Name
                        + "  (" + group.Rows.Count + ")";
                    group.Header.AccessibleName = group.Name + ", " +
                        group.Rows.Count + ", " +
                        LocalizationService.Get(expanded
                            ? "Sets_PreviewExpanded" : "Sets_PreviewCollapsed");
                    group.Header.SetBounds(0, y, width, GroupHeaderHeight);
                    y += GroupHeaderHeight + (expanded ? 5 : 0);

                    foreach (DocumentRow row in group.Rows)
                    {
                        row.Container.Visible = expanded;
                        if (!expanded)
                        {
                            continue;
                        }

                        row.Container.SetBounds(0, y, width, DocumentRowHeight);
                        int contentWidth = Math.Max(60, width - StatusWidth - 24);
                        row.Title.SetBounds(12, 6, contentWidth, 26);
                        int pathWidth = contentWidth
                            - (row.CopyPath is null ? 0 : CopyButtonSize + 6);
                        row.Path.SetBounds(12, 34, Math.Max(1, pathWidth), 20);
                        row.CopyPath?.SetBounds(
                            12 + contentWidth - CopyButtonSize, 31,
                            CopyButtonSize, CopyButtonSize);
                        row.StatusIcon.SetBounds(width - StatusWidth + 2, 17, 24, 28);
                        row.StatusText.SetBounds(width - StatusWidth + 29, 16,
                            StatusWidth - 35, 31);
                        row.Divider.SetBounds(12, DocumentRowHeight - 1,
                            Math.Max(0, width - 24), 1);
                        y += DocumentRowHeight + ItemGap;
                    }

                    y += GroupGap;
                }

                canvas.Height = Math.Max(0, y);
            }
            finally
            {
                canvas.ResumeLayout(false);
            }
        }
        finally
        {
            isUpdatingLayout = false;
        }
    }

    private static bool IsRestorable(SavedWindowEntry entry)
    {
        try
        {
            return entry.Kind switch
            {
                SavedWindowKind.Application =>
                    SavedWindowCaptureService.IsSupportedExecutable(entry.ExecutablePath)
                    && File.Exists(entry.ExecutablePath),
                SavedWindowKind.ExplorerFolder =>
                    SavedWindowCaptureService.IsSupportedDocument(
                        entry.Kind, entry.ResourcePath)
                    && Directory.Exists(entry.ResourcePath),
                SavedWindowKind.Unsupported => false,
                _ => SavedWindowCaptureService.IsSupportedDocument(
                        entry.Kind, entry.ResourcePath)
                    && File.Exists(entry.ResourcePath)
            };
        }
        catch (Exception error) when (error is ArgumentException
            or NotSupportedException or UnauthorizedAccessException
            or IOException)
        {
            return false;
        }
    }

    private static string CompactPath(string path)
    {
        if (path.Length <= 76)
        {
            return path;
        }

        try
        {
            string trimmed = Path.TrimEndingDirectorySeparator(path);
            string leaf = Path.GetFileName(trimmed);
            string parent = Path.GetFileName(Path.GetDirectoryName(trimmed) ?? "");
            string root = Path.GetPathRoot(trimmed) ?? "";
            if (string.IsNullOrEmpty(leaf))
            {
                return path;
            }

            return string.IsNullOrEmpty(parent)
                ? root + "…" + Path.DirectorySeparatorChar + leaf
                : root + "…" + Path.DirectorySeparatorChar
                    + parent + Path.DirectorySeparatorChar + leaf;
        }
        catch (Exception error) when (error is ArgumentException
            or NotSupportedException or PathTooLongException)
        {
            return path;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            toolTip.Dispose();
            headingFont.Dispose();
            titleFont.Dispose();
            pathFont.Dispose();
            stateFont.Dispose();
        }
        base.Dispose(disposing);
    }
}
