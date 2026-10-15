using WindowDeck.Localization;
using WindowDeck.Models;
using WindowDeck.Services;

namespace WindowDeck;

internal sealed class SavedWindowSetCaptureForm : Form
{
    private readonly TextBox nameBox;
    private readonly ToolTip fileToolTip = new();
    private readonly List<(CheckBox Check, SavedWindowEntry Entry)> entries = [];

    public string SetName => nameBox.Text.Trim();
    public List<SavedWindowEntry> SelectedWindows =>
        entries.Where(item => item.Check.Checked).Select(item => item.Entry).ToList();

    public SavedWindowSetCaptureForm(
        IReadOnlyList<SavedWindowEntry> windows, string proposedName, AppTheme theme)
    {
        ThemePalette palette = ThemeManager.Resolve(theme);
        Text = LocalizationService.Get("Sets_CaptureTitle");
        Icon = WindowDeckIcon.Load();
        Font = new Font("Segoe UI", 9F);
        AutoScaleMode = AutoScaleMode.Dpi;
        // The parent Saved window sets panel is TopMost. This second-level
        // modal dialog must stay in the same Z-order band, above its owner.
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        ShowInTaskbar = false;
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimumSize = new Size(580, 420);
        ClientSize = new Size(740, 660);
        BackColor = palette.Background;
        ForeColor = palette.Foreground;

        TableLayoutPanel layout = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(16, 12, 16, 12),
            BackColor = palette.Background
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        TableLayoutPanel nameRow = new() { ColumnCount = 2, Dock = DockStyle.Top, AutoSize = true };
        nameRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        nameRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        Label nameLabel = new()
        {
            Text = LocalizationService.Get("Sets_Name"),
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 7, 10, 0)
        };
        nameBox = new TextBox()
        {
            Text = proposedName,
            Dock = DockStyle.Fill,
            MaxLength = 120,
            BackColor = palette.Surface,
            ForeColor = palette.Foreground
        };
        nameRow.Controls.Add(nameLabel, 0, 0);
        nameRow.Controls.Add(nameBox, 1, 0);
        Label fileHelp = new()
        {
            Text = LocalizationService.Get("Sets_ChooseFileExplanation"),
            AutoSize = true,
            MaximumSize = new Size(650, 0),
            ForeColor = palette.SecondaryForeground,
            Margin = new Padding(0, 7, 0, 3)
        };
        nameRow.Controls.Add(fileHelp, 0, 1);
        nameRow.SetColumnSpan(fileHelp, 2);
        layout.Controls.Add(nameRow, 0, 0);

        FlowLayoutPanel toolbar = new()
        {
            AutoSize = true, FlowDirection = FlowDirection.LeftToRight,
            Margin = new Padding(0, 12, 0, 8)
        };
        Button selectAll = CreateButton("Sets_SelectAll", palette);
        Button deselectAll = CreateButton("Sets_DeselectAll", palette);
        selectAll.Click += (_, _) =>
        {
            foreach (var item in entries) item.Check.Checked = true;
        };
        deselectAll.Click += (_, _) =>
        {
            foreach (var item in entries) item.Check.Checked = false;
        };
        toolbar.Controls.AddRange([selectAll, deselectAll]);
        layout.Controls.Add(toolbar, 0, 1);

        FlowLayoutPanel windowList = new()
        {
            AutoScroll = true,
            WrapContents = false,
            FlowDirection = FlowDirection.TopDown,
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = palette.Surface,
            Padding = new Padding(8)
        };
        foreach (IGrouping<string, SavedWindowEntry> group in windows
                     .GroupBy(window => window.ApplicationName)
                     .OrderBy(group => group.Key, StringComparer.CurrentCultureIgnoreCase))
        {
            Label groupHeader = new()
            {
                AutoSize = false,
                Width = 645,
                Height = 27,
                Text = group.Key,
                Font = new Font(Font, FontStyle.Bold),
                ForeColor = palette.Foreground,
                Margin = new Padding(3, 9, 3, 0)
            };
            windowList.Controls.Add(groupHeader);
            foreach (SavedWindowEntry window in group)
            {
                FlowLayoutPanel row = new()
                {
                    WrapContents = false,
                    AutoSize = false,
                    Height = 35,
                    Width = 645,
                    Margin = new Padding(3, 1, 3, 1),
                    BackColor = palette.Surface
                };
                CheckBox check = new()
                {
                    Checked = true,
                    AutoSize = false,
                    Height = 32,
                    AutoEllipsis = true,
                    ForeColor = palette.Foreground,
                    BackColor = palette.Surface,
                    Margin = Padding.Empty
                };
                SavedWindowStatusIcon statusIcon = new()
                {
                    BackColor = palette.Surface
                };
                Button chooseFile = CreateButton("Sets_ChooseFile", palette);
                chooseFile.AutoSize = false;
                chooseFile.Width = 157;
                chooseFile.Height = 31;
                chooseFile.Margin = new Padding(6, 1, 0, 0);
                fileToolTip.SetToolTip(chooseFile,
                    LocalizationService.Get("Sets_ChooseFileHelp"));

                void LayoutRow()
                {
                    int pickerWidth = chooseFile.Visible
                        ? chooseFile.Width + chooseFile.Margin.Horizontal
                        : 0;
                    check.Width = Math.Max(140, row.ClientSize.Width -
                        statusIcon.Width - statusIcon.Margin.Horizontal -
                        pickerWidth - 2);
                }

                void UpdateRow()
                {
                    bool isReady = window.Kind switch
                    {
                        SavedWindowKind.Application =>
                            SavedWindowCaptureService.IsSupportedExecutable(window.ExecutablePath)
                            && File.Exists(window.ExecutablePath),
                        SavedWindowKind.ExplorerFolder =>
                            SavedWindowCaptureService.IsSupportedDocument(
                                window.Kind, window.ResourcePath)
                            && Directory.Exists(window.ResourcePath),
                        _ => SavedWindowCaptureService.IsSupportedDocument(
                                window.Kind, window.ResourcePath)
                            && File.Exists(window.ResourcePath)
                    };

                    statusIcon.IsReady = isReady;
                    statusIcon.AccessibleName = LocalizationService.Get(
                        isReady ? "Sets_StatusReady" : "Sets_StatusMissing");
                    fileToolTip.SetToolTip(statusIcon, statusIcon.AccessibleName);
                    chooseFile.Visible = !isReady;

                    string status = window.Kind switch
                    {
                        SavedWindowKind.Application when isReady =>
                            LocalizationService.Get("Sets_ProgramOnly"),
                        SavedWindowKind.Unsupported when window.NeedsDocumentPath =>
                            LocalizationService.Get("Sets_DocumentPathRequired"),
                        SavedWindowKind.Unsupported =>
                            LocalizationService.Get("Sets_Unsupported"),
                        _ when !isReady => LocalizationService.Get("Sets_DocumentPathRequired"),
                        _ => string.Empty
                    };
                    string fileName = !string.IsNullOrWhiteSpace(window.ResourcePath)
                        ? " — " + Path.GetFileName(window.ResourcePath)
                        : string.Empty;
                    check.Text = window.WindowTitle + fileName +
                        (status.Length > 0 ? "  " + status : string.Empty);
                    fileToolTip.SetToolTip(check, window.ResourcePath ?? window.WindowTitle);
                    LayoutRow();
                }

                chooseFile.Click += (_, _) =>
                {
                    using OpenFileDialog picker = new()
                    {
                        Title = LocalizationService.Get("Sets_ChooseFile"),
                        Filter = LocalizationService.Get("Sets_AllFilesFilter"),
                        CheckFileExists = true,
                        Multiselect = false
                    };
                    if (picker.ShowDialog(this) != DialogResult.OK)
                    {
                        return;
                    }
                    if (!SavedWindowCaptureService.IsSupportedDocument(
                            SavedWindowKind.DocumentFile, picker.FileName))
                    {
                        MessageBox.Show(this, LocalizationService.Get("Sets_UnsafeFile"),
                            Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    window.Kind = SavedWindowKind.DocumentFile;
                    window.ResourcePath = picker.FileName;
                    window.NeedsDocumentPath = false;
                    UpdateRow();
                };

                row.Controls.Add(check);
                row.Controls.Add(statusIcon);
                row.Controls.Add(chooseFile);
                UpdateRow();
                entries.Add((check, window));
                windowList.Controls.Add(row);
            }
        }
        windowList.Resize += (_, _) =>
        {
            int width = Math.Max(240, windowList.ClientSize.Width
                - SystemInformation.VerticalScrollBarWidth - 22);
            foreach (Control child in windowList.Controls)
            {
                child.Width = width;
                if (child is FlowLayoutPanel fileRow)
                {
                    CheckBox check = fileRow.Controls.OfType<CheckBox>().First();
                    SavedWindowStatusIcon indicator =
                        fileRow.Controls.OfType<SavedWindowStatusIcon>().First();
                    Button chooseFile = fileRow.Controls.OfType<Button>().First();
                    int pickerWidth = chooseFile.Visible
                        ? chooseFile.Width + chooseFile.Margin.Horizontal
                        : 0;
                    check.Width = Math.Max(140, width - indicator.Width -
                        indicator.Margin.Horizontal - pickerWidth - 2);
                }
            }
        };
        layout.Controls.Add(windowList, 0, 2);

        FlowLayoutPanel bottom = new()
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Margin = new Padding(0, 12, 0, 0)
        };
        Button save = CreateButton("Sets_Save", palette);
        Button cancel = CreateButton("Common_Cancel", palette);
        save.Click += (_, _) =>
        {
            if (SetName.Length == 0)
            {
                MessageBox.Show(this, LocalizationService.Get("Sets_NameRequired"), Text,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (SelectedWindows.Count == 0)
            {
                MessageBox.Show(this, LocalizationService.Get("Sets_NoSelection"), Text,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int unresolvedDocuments = SelectedWindows.Count(item => item.NeedsDocumentPath);
            if (unresolvedDocuments > 0
                && MessageBox.Show(this,
                    LocalizationService.Format("Sets_UnresolvedSaveConfirmation", unresolvedDocuments),
                    Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                // Keep the dialog open so missing paths can be linked or unchecked.
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        };
        cancel.DialogResult = DialogResult.Cancel;
        bottom.Controls.AddRange([save, cancel]);
        layout.Controls.Add(bottom, 0, 3);
        Controls.Add(layout);
        AcceptButton = save;
        CancelButton = cancel;
        ThemeManager.ApplyTitleBar(this, palette.IsDark);
        Shown += (_, _) =>
        {
            if (Owner is Form owner && !owner.IsDisposed)
            {
                Rectangle area = Screen.FromControl(owner).WorkingArea;
                int x = Math.Clamp(owner.Left + (owner.Width - Width) / 2,
                    area.Left, Math.Max(area.Left, area.Right - Width));
                int y = Math.Clamp(owner.Top + (owner.Height - Height) / 2,
                    area.Top, Math.Max(area.Top, area.Bottom - Height));
                Location = new Point(x, y);
            }

            BringToFront();
            Activate();
        };
    }

    private static Button CreateButton(string resource, ThemePalette palette)
    {
        Button button = new()
        {
            Text = LocalizationService.Get(resource),
            AutoSize = true,
            Padding = new Padding(8, 3, 8, 3)
        };
        ThemeManager.StyleButton(button, palette);
        return button;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            fileToolTip.Dispose();
            Icon?.Dispose();
        }
        base.Dispose(disposing);
    }
}
