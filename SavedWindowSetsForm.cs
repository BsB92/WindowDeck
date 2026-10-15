using System.Text;
using WindowDeck.Localization;
using WindowDeck.Models;
using WindowDeck.Services;

namespace WindowDeck;

internal sealed class SavedWindowSetsForm : Form
{
    private sealed record DisplayItem(SavedWindowSet Set)
    {
        public override string ToString() => Set.Name;
    }

    private readonly AppSettings settings;
    private readonly SavedWindowSetsStore store = new();
    private readonly SavedWindowRestoreService restore = new();
    private readonly ListBox setList;
    private readonly ListView details;
    private readonly Label detailsHeader;
    private readonly TextBox restoreReport;
    private readonly TableLayoutPanel detailsLayout;
    private readonly ImageList detailsRowHeight = new();
    private List<SavedWindowSet> savedSets;

    public SavedWindowSetsForm(AppSettings settings, Rectangle windowDeckBounds, Rectangle workingArea)
    {
        this.settings = settings.Copy();
        savedSets = store.Load();
        ThemePalette palette = ThemeManager.Resolve(settings.Theme);

        Text = LocalizationService.Get("Sets_Title");
        Icon = WindowDeckIcon.Load();
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
        TopMost = true;
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimumSize = new Size(560, 390);
        int availableLeft = Math.Max(0, windowDeckBounds.Left - workingArea.Left - 12);
        int targetWidth = Math.Clamp(availableLeft, 560, 900);
        ClientSize = new Size(targetWidth, Math.Min(565, Math.Max(310, workingArea.Height - 70)));
        Location = new Point(
            Math.Max(workingArea.Left, windowDeckBounds.Left - Width - 12),
            Math.Clamp(windowDeckBounds.Top, workingArea.Top,
                Math.Max(workingArea.Top, workingArea.Bottom - Height)));
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = new Font("Segoe UI", 9F);
        BackColor = palette.Background;
        ForeColor = palette.Foreground;

        TableLayoutPanel layout = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(15, 14, 15, 14),
            BackColor = palette.Background
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        Label intro = new()
        {
            AutoSize = true,
            Text = LocalizationService.Get("Sets_Introduction"),
            Margin = new Padding(0, 0, 0, 12)
        };
        layout.Controls.Add(intro, 0, 0);

        TableLayoutPanel panes = new() { Dock = DockStyle.Fill, ColumnCount = 2 };
        panes.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
        panes.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));

        setList = new ListBox
        {
            Dock = DockStyle.Fill,
            IntegralHeight = false,
            BackColor = palette.Surface,
            ForeColor = palette.Foreground
        };
        setList.SelectedIndexChanged += (_, _) => DisplaySelection();
        panes.Controls.Add(setList, 0, 0);

        detailsLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Margin = new Padding(12, 0, 0, 0),
            BackColor = palette.Background
        };
        detailsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        detailsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        detailsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        detailsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 0));

        detailsHeader = new Label
        {
            AutoSize = true,
            Font = new Font(Font, FontStyle.Bold),
            ForeColor = palette.Foreground,
            Margin = new Padding(0, 0, 0, 8)
        };
        detailsLayout.Controls.Add(detailsHeader, 0, 0);

        // Unlike a multiline TextBox, ListView gives each document exactly
        // one distinct row, column headers, grid separators and native
        // horizontal/vertical scrollbars for long paths.
        detailsRowHeight.ImageSize = new Size(1, 29);
        detailsRowHeight.ColorDepth = ColorDepth.Depth32Bit;
        using (Bitmap spacer = new(1, 29))
        {
            detailsRowHeight.Images.Add(spacer);
        }
        details = new ListView
        {
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            MultiSelect = false,
            HideSelection = false,
            HeaderStyle = ColumnHeaderStyle.Nonclickable,
            Scrollable = true,
            ShowItemToolTips = true,
            SmallImageList = detailsRowHeight,
            Dock = DockStyle.Fill,
            BackColor = palette.Surface,
            ForeColor = palette.Foreground
        };
        details.Columns.Add(LocalizationService.Get("Sets_ColumnApplication"), 145);
        details.Columns.Add(LocalizationService.Get("Sets_ColumnWindow"), 350);
        details.Columns.Add(LocalizationService.Get("Sets_ColumnPath"), 510);
        details.Columns.Add(LocalizationService.Get("Sets_ColumnStatus"), 185);
        detailsLayout.Controls.Add(details, 0, 1);

        restoreReport = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Dock = DockStyle.Fill,
            Visible = false,
            BackColor = palette.Surface,
            ForeColor = palette.Foreground,
            Margin = new Padding(0, 9, 0, 0)
        };
        detailsLayout.Controls.Add(restoreReport, 0, 2);
        panes.Controls.Add(detailsLayout, 1, 0);
        layout.Controls.Add(panes, 0, 1);

        FlowLayoutPanel actions = new()
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 14, 0, 0)
        };
        Button saveCurrent = MakeButton("Sets_SaveCurrent", palette);
        Button open = MakeButton("Sets_Open", palette);
        Button update = MakeButton("Sets_Update", palette);
        Button rename = MakeButton("Sets_Rename", palette);
        Button delete = MakeButton("Sets_Delete", palette);
        Button close = MakeButton("Sets_Close", palette);
        saveCurrent.Click += (_, _) => SaveCurrent();
        open.Click += (_, _) => OpenSelected();
        update.Click += (_, _) => UpdateSelected();
        rename.Click += (_, _) => RenameSelected();
        delete.Click += (_, _) => DeleteSelected();
        close.Click += (_, _) => Close();
        actions.Controls.AddRange([saveCurrent, open, update, rename, delete, close]);
        layout.Controls.Add(actions, 0, 2);
        Controls.Add(layout);
        CancelButton = close;
        ThemeManager.ApplyTitleBar(this, palette.IsDark);
        ReloadList();
    }

    private SavedWindowSet? SelectedSet => (setList.SelectedItem as DisplayItem)?.Set;

    private static Button MakeButton(string resourceKey, ThemePalette palette)
    {
        Button button = new()
        {
            Text = LocalizationService.Get(resourceKey),
            AutoSize = true,
            Padding = new Padding(7, 3, 7, 3)
        };
        ThemeManager.StyleButton(button, palette);
        return button;
    }

    private void ReloadList(Guid? preferredId = null)
    {
        Guid? id = preferredId ?? SelectedSet?.Id;
        setList.BeginUpdate();
        try
        {
            setList.Items.Clear();
            foreach (SavedWindowSet set in savedSets.OrderBy(set => set.Name,
                         StringComparer.CurrentCultureIgnoreCase))
            {
                setList.Items.Add(new DisplayItem(set));
            }

            for (int index = 0; index < setList.Items.Count; index++)
            {
                if (((DisplayItem)setList.Items[index]!).Set.Id == id)
                {
                    setList.SelectedIndex = index;
                    break;
                }
            }

            if (setList.SelectedIndex < 0 && setList.Items.Count > 0)
            {
                setList.SelectedIndex = 0;
            }
        }
        finally
        {
            setList.EndUpdate();
        }

        DisplaySelection();
    }

    private void DisplaySelection()
    {
        SavedWindowSet? selected = SelectedSet;
        details.BeginUpdate();
        try
        {
            details.Items.Clear();
            HideRestoreReport();
            if (selected is null)
            {
                detailsHeader.Text = LocalizationService.Get("Sets_NoSets");
                return;
            }

            detailsHeader.Text = selected.Name + "  —  " +
                LocalizationService.Format("Sets_WindowsCount", selected.Windows.Count);

            foreach (SavedWindowEntry entry in selected.Windows)
            {
                string target = entry.ResourcePath ?? entry.ExecutablePath ?? string.Empty;
                string status = entry.Kind switch
                {
                    SavedWindowKind.Unsupported when entry.NeedsDocumentPath =>
                        LocalizationService.Get("Sets_DocumentPathRequired"),
                    SavedWindowKind.Unsupported => LocalizationService.Get("Sets_Unsupported"),
                    SavedWindowKind.Application => LocalizationService.Get("Sets_ProgramOnly"),
                    _ => LocalizationService.Get("Sets_DocumentLinked")
                };

                ListViewItem item = new(
                [
                    entry.ApplicationName,
                    entry.WindowTitle,
                    target,
                    status
                ])
                {
                    ImageIndex = 0,
                    ToolTipText = target.Length > 0
                        ? entry.WindowTitle + Environment.NewLine + target
                        : entry.WindowTitle
                };
                details.Items.Add(item);
            }
        }
        finally
        {
            details.EndUpdate();
        }
    }

    private void HideRestoreReport()
    {
        restoreReport.Visible = false;
        restoreReport.Text = string.Empty;
        detailsLayout.RowStyles[2].Height = 0;
    }

    private List<SavedWindowEntry> CaptureCurrent()
    {
        WindowEnumerator enumerator = new();
        SavedWindowCaptureService capture = new();
        return enumerator.Enumerate(settings)
            .Select(capture.Capture)
            .OrderBy(entry => entry.ApplicationName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(entry => entry.WindowTitle, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    private void SaveCurrent()
    {
        try
        {
            List<SavedWindowEntry> windows = CaptureCurrent();
            if (windows.Count == 0)
            {
                Info("Sets_NoWindows");
                return;
            }

            using SavedWindowSetCaptureForm dialog = new(
                SavedWindowSetUpdatePlanner.ForNewSet(windows),
                LocalizationService.Get("Sets_DefaultName"), settings.Theme);
            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            if (NameExists(dialog.SetName))
            {
                Info("Sets_NameExists");
                return;
            }

            SavedWindowSet set = new() { Name = dialog.SetName, Windows = dialog.SelectedWindows };
            List<SavedWindowSet> updated = [.. savedSets, set];
            store.Save(updated);
            savedSets = updated;
            ReloadList(set.Id);
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            Failure(error);
        }
    }

    private void UpdateSelected()
    {
        SavedWindowSet? current = SelectedSet;
        if (current is null) return;

        try
        {
            // Keep all previously saved windows in the editor, even if they
            // are not running today. Closed entries remain checked. New live
            // windows are unchecked until the user explicitly adds them.
            IReadOnlyList<SavedWindowSelectionItem> choices =
                SavedWindowSetUpdatePlanner.ForUpdate(current.Windows, CaptureCurrent());

            using SavedWindowSetCaptureForm dialog = new(
                choices, current.Name, settings.Theme, updating: true);
            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            if (NameExists(dialog.SetName, current.Id))
            {
                Info("Sets_NameExists");
                return;
            }

            List<SavedWindowEntry> chosen = dialog.SelectedWindows;
            int removed = choices.Count(item => item.InSavedSet
                && !chosen.Contains(item.Entry));
            int added = choices.Count(item => !item.InSavedSet
                && chosen.Contains(item.Entry));

            if (MessageBox.Show(this, LocalizationService.Format(
                    "Sets_ConfirmUpdateChanges", current.Name, added, removed),
                    Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question)
                != DialogResult.Yes)
            {
                return;
            }

            SavedWindowSet updatedSet = new()
            {
                Id = current.Id,
                Name = dialog.SetName,
                SavedAtUtc = DateTime.UtcNow,
                Windows = chosen
            };
            List<SavedWindowSet> updated = savedSets
                .Select(set => set.Id == current.Id ? updatedSet : set).ToList();
            store.Save(updated);
            savedSets = updated;
            ReloadList(updatedSet.Id);
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            Failure(error);
        }
    }

    private void RenameSelected()
    {
        SavedWindowSet? current = SelectedSet;
        if (current is null) return;

        try
        {
            string? name = PromptName(current.Name);
            if (name is null || string.Equals(name, current.Name, StringComparison.Ordinal)) return;
            if (NameExists(name, current.Id))
            {
                Info("Sets_NameExists");
                return;
            }

            List<SavedWindowSet> updated = savedSets.Select(set => set.Id == current.Id
                ? new SavedWindowSet
                {
                    Id = current.Id,
                    Name = name,
                    SavedAtUtc = current.SavedAtUtc,
                    Windows = current.Windows
                }
                : set).ToList();
            store.Save(updated);
            savedSets = updated;
            ReloadList(current.Id);
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            Failure(error);
        }
    }

    private void DeleteSelected()
    {
        SavedWindowSet? current = SelectedSet;
        if (current is null || !Confirm("Sets_ConfirmDelete", current.Name)) return;

        try
        {
            List<SavedWindowSet> updated = savedSets.Where(set => set.Id != current.Id).ToList();
            store.Save(updated);
            savedSets = updated;
            ReloadList();
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            Failure(error);
        }
    }

    private void OpenSelected()
    {
        SavedWindowSet? current = SelectedSet;
        if (current is null) return;

        try
        {
            // Stop covering the very documents we are about to open. This also
            // lowers the owning WindowDeck flyout; it does not change any
            // other application's window flags or close an existing window.
            Form1.YieldTopmostForExternalWindows();
            SavedWindowRestoreResult result = restore.Open(current, settings);
            string summary = LocalizationService.Format("Sets_RestoreSummary",
                result.Requested, result.AlreadyOpen, result.Unsupported, result.Missing, result.Failed);
            if (result.Notes.Count > 0)
            {
                summary += Environment.NewLine + Environment.NewLine
                    + string.Join(Environment.NewLine, result.Notes.Take(20));
            }

            // A modal MessageBox here would jump ABOVE the just-opened files.
            // Keep the complete result visible in the manager for when the user
            // switches back, without stealing focus from the document windows.
            restoreReport.Text = summary;
            detailsLayout.RowStyles[2].Height = 115;
            restoreReport.Visible = true;
            restoreReport.SelectionStart = 0;
            restoreReport.ScrollToCaret();
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            Failure(error);
        }
    }

    private string? PromptName(string currentName)
    {
        ThemePalette palette = ThemeManager.Resolve(settings.Theme);
        using Form dialog = new()
        {
            Text = LocalizationService.Get("Sets_Rename"),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MinimizeBox = false,
            MaximizeBox = false,
            ClientSize = new Size(440, 115),
            Font = Font,
            BackColor = palette.Background,
            ForeColor = palette.Foreground
        };
        TextBox name = new()
        {
            Text = currentName,
            Location = new Point(15, 15),
            Width = 410,
            MaxLength = 120,
            BackColor = palette.Surface,
            ForeColor = palette.Foreground
        };
        Button ok = MakeButton("Common_OK", palette);
        ok.Location = new Point(245, 65);
        ok.DialogResult = DialogResult.OK;
        Button cancel = MakeButton("Common_Cancel", palette);
        cancel.Location = new Point(332, 65);
        cancel.DialogResult = DialogResult.Cancel;
        dialog.Controls.AddRange([name, ok, cancel]);
        dialog.AcceptButton = ok;
        dialog.CancelButton = cancel;

        if (dialog.ShowDialog(this) != DialogResult.OK) return null;
        if (string.IsNullOrWhiteSpace(name.Text))
        {
            Info("Sets_NameRequired");
            return null;
        }
        return name.Text.Trim();
    }

    private bool NameExists(string name, Guid? except = null) =>
        savedSets.Any(set => set.Id != except &&
            string.Equals(set.Name, name, StringComparison.OrdinalIgnoreCase));

    private bool Confirm(string key, string name) =>
        MessageBox.Show(this, LocalizationService.Format(key, name), Text,
            MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;

    private void Info(string key) =>
        MessageBox.Show(this, LocalizationService.Get(key), Text,
            MessageBoxButtons.OK, MessageBoxIcon.Information);

    private void Failure(Exception error) =>
        MessageBox.Show(this, LocalizationService.Format("Sets_Error", error.Message), Text,
            MessageBoxButtons.OK, MessageBoxIcon.Warning);

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            detailsRowHeight.Dispose();
            Icon?.Dispose();
        }
        base.Dispose(disposing);
    }
}
