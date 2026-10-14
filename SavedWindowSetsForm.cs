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
    private readonly TextBox details;
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

        details = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Dock = DockStyle.Fill,
            BackColor = palette.Surface,
            ForeColor = palette.Foreground,
            Margin = new Padding(12, 0, 0, 0)
        };
        panes.Controls.Add(details, 1, 0);
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
        if (selected is null)
        {
            details.Text = LocalizationService.Get("Sets_NoSets");
            return;
        }

        StringBuilder display = new();
        display.AppendLine(selected.Name);
        display.AppendLine(LocalizationService.Format("Sets_WindowsCount", selected.Windows.Count));
        display.AppendLine();
        foreach (SavedWindowEntry entry in selected.Windows)
        {
            display.Append(entry.ApplicationName).Append(" — ").Append(entry.WindowTitle);
            if (entry.Kind == SavedWindowKind.Unsupported)
            {
                display.Append(' ').Append(LocalizationService.Get("Sets_Unsupported"));
            }
            else if (entry.Kind == SavedWindowKind.Application)
            {
                display.Append(' ').Append(LocalizationService.Get("Sets_ProgramOnly"));
            }
            if (!string.IsNullOrWhiteSpace(entry.ResourcePath))
            {
                display.AppendLine();
                display.Append("    ").Append(entry.ResourcePath);
            }
            display.AppendLine();
        }

        details.Text = display.ToString();
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
                windows, LocalizationService.Get("Sets_DefaultName"), settings.Theme);
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
            List<SavedWindowEntry> windows = CaptureCurrent();
            if (windows.Count == 0)
            {
                Info("Sets_NoWindows");
                return;
            }

            using SavedWindowSetCaptureForm dialog = new(windows, current.Name, settings.Theme);
            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            if (NameExists(dialog.SetName, current.Id))
            {
                Info("Sets_NameExists");
                return;
            }

            if (!Confirm("Sets_ConfirmUpdate", current.Name)) return;
            SavedWindowSet updatedSet = new()
            {
                Id = current.Id,
                Name = dialog.SetName,
                SavedAtUtc = DateTime.UtcNow,
                Windows = dialog.SelectedWindows
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
            SavedWindowRestoreResult result = restore.Open(current, settings);
            string summary = LocalizationService.Format("Sets_RestoreSummary",
                result.Requested, result.AlreadyOpen, result.Unsupported, result.Missing, result.Failed);
            if (result.Notes.Count > 0)
            {
                summary += Environment.NewLine + Environment.NewLine
                    + string.Join(Environment.NewLine, result.Notes.Take(20));
            }
            MessageBox.Show(this, summary, Text, MessageBoxButtons.OK,
                result.Missing > 0 || result.Failed > 0
                    ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
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
        if (disposing) Icon?.Dispose();
        base.Dispose(disposing);
    }
}
