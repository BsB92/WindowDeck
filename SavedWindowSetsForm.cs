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
    private readonly SavedWindowSetCloseService closer = new();
    private readonly ListBox setList;
    private readonly SavedSetGroupedView details;
    private readonly Label detailsHeader;
    private readonly TextBox restoreReport;
    private readonly TableLayoutPanel detailsLayout;
    private readonly Button saveCurrentButton;
    private readonly Button updateButton;
    private bool captureRunning;
    private Task<List<SavedWindowEntry>>? pendingCaptureTask;
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
        panes.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32));
        panes.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 68));

        setList = new ListBox
        {
            Dock = DockStyle.Fill,
            IntegralHeight = false,
            BorderStyle = BorderStyle.None,
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

        // Grouped, borderless document cards replace the former spreadsheet-like
        // ListView. Window titles and shortened paths are in separate lines,
        // grouped under collapsible application headings.
        details = new SavedSetGroupedView(palette)
        {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty
        };
        detailsLayout.Controls.Add(details, 0, 1);

        restoreReport = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Dock = DockStyle.Fill,
            Visible = false,
            BorderStyle = BorderStyle.None,
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
        saveCurrentButton = MakeButton("Sets_SaveCurrent", palette);
        Button open = MakeButton("Sets_Open", palette);
        Button closeSet = MakeButton("Sets_CloseSet", palette);
        updateButton = MakeButton("Sets_Update", palette);
        Button delete = MakeButton("Sets_Delete", palette);
        Button close = MakeButton("Sets_Close", palette);
        saveCurrentButton.Click += (_, _) => SaveCurrent();
        open.Click += (_, _) => OpenSelected();
        closeSet.Click += (_, _) => CloseSelectedSet();
        updateButton.Click += (_, _) => UpdateSelected();
        delete.Click += (_, _) => DeleteSelected();
        close.Click += (_, _) => Close();
        actions.Controls.AddRange([saveCurrentButton, open, closeSet, updateButton, delete, close]);
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
        HideRestoreReport();
        if (selected is null)
        {
            detailsHeader.Text = LocalizationService.Get("Sets_NoSets");
            details.ShowEntries([]);
            return;
        }

        detailsHeader.Text = selected.Name + "  —  " +
            LocalizationService.Format("Sets_WindowsCount", selected.Windows.Count);
        details.ShowEntries(selected.Windows);
    }

    private void HideRestoreReport()
    {
        restoreReport.Visible = false;
        restoreReport.Text = string.Empty;
        detailsLayout.RowStyles[2].Height = 0;
    }

    // Office/Shell COM discovery and cloud file checks can block for seconds.
    // Keep all discovery on a dedicated STA worker rather than the WinForms thread.
    private Task<List<SavedWindowEntry>> CaptureCurrentAsync()
    {
        // A timed-out Office COM request may still be running in its STA.
        // Reuse that worker instead of starting another competing Office scan.
        if (pendingCaptureTask is { IsCompleted: false })
        {
            return pendingCaptureTask;
        }

        AppSettings snapshot = settings.Copy();
        TaskCompletionSource<List<SavedWindowEntry>> completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        Thread worker = new(() =>
        {
            try
            {
                WindowEnumerator enumerator = new();
                SavedWindowCaptureService capture = new();
                List<SavedWindowEntry> windows = enumerator.Enumerate(snapshot)
                    .Select(capture.Capture)
                    .OrderBy(entry => entry.ApplicationName,
                        StringComparer.CurrentCultureIgnoreCase)
                    .ThenBy(entry => entry.WindowTitle,
                        StringComparer.CurrentCultureIgnoreCase)
                    .ToList();
                completion.TrySetResult(windows);
            }
            catch (Exception error)
            {
                completion.TrySetException(error);
            }
        })
        {
            IsBackground = true,
            Name = "WindowDeck saved-set discovery"
        };
        worker.SetApartmentState(ApartmentState.STA);
        worker.Start();
        pendingCaptureTask = completion.Task;
        return completion.Task;
    }

    private async Task<IReadOnlyList<SavedWindowSelectionItem>> PrepareChoicesAsync(
        IReadOnlyList<SavedWindowEntry>? existing = null)
    {
        List<SavedWindowEntry> windows = await CaptureCurrentAsync()
            .WaitAsync(TimeSpan.FromSeconds(20));

        // File.Exists can be unexpectedly slow for OneDrive/SharePoint placeholders.
        // Evaluate readiness before constructing hundreds of UI controls.
        return await Task.Run(() =>
        {
            IReadOnlyList<SavedWindowSelectionItem> choices = existing is null
                ? SavedWindowSetUpdatePlanner.ForNewSet(windows)
                : SavedWindowSetUpdatePlanner.ForUpdate(existing, windows);
            return (IReadOnlyList<SavedWindowSelectionItem>)choices
                .Select(item => item with
                {
                    IsReady = SavedWindowReadiness.IsReady(item.Entry)
                })
                .ToArray();
        }).WaitAsync(TimeSpan.FromSeconds(20));
    }

    private void SetCaptureRunning(bool running)
    {
        captureRunning = running;
        if (IsDisposed || Disposing)
        {
            return;
        }

        saveCurrentButton.Enabled = !running;
        updateButton.Enabled = !running;
        saveCurrentButton.Text = LocalizationService.Get(
            running ? "Sets_ScanningWindows" : "Sets_SaveCurrent");
        updateButton.Text = LocalizationService.Get(
            running ? "Sets_ScanningWindows" : "Sets_Update");
    }

    private async void SaveCurrent()
    {
        if (captureRunning) return;

        SetCaptureRunning(true);
        try
        {
            IReadOnlyList<SavedWindowSelectionItem> choices = await PrepareChoicesAsync();
            if (IsDisposed || Disposing) return;
            if (choices.Count == 0)
            {
                Info("Sets_NoWindows");
                return;
            }

            using SavedWindowSetCaptureForm dialog = new(
                choices, LocalizationService.Get("Sets_DefaultName"), settings.Theme);
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
        catch (TimeoutException)
        {
            if (!IsDisposed) Info("Sets_CaptureTimeout");
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            if (!IsDisposed) Failure(error);
        }
        finally
        {
            SetCaptureRunning(false);
        }
    }

    private async void UpdateSelected()
    {
        if (captureRunning) return;
        SavedWindowSet? current = SelectedSet;
        if (current is null) return;

        SetCaptureRunning(true);
        try
        {
            // Keep previously saved (even closed) resources in the editor.
            IReadOnlyList<SavedWindowSelectionItem> choices =
                await PrepareChoicesAsync(current.Windows);
            if (IsDisposed || Disposing) return;

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
        catch (TimeoutException)
        {
            if (!IsDisposed) Info("Sets_CaptureTimeout");
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            if (!IsDisposed) Failure(error);
        }
        finally
        {
            SetCaptureRunning(false);
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
            ShowInlineReport(summary);
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            Failure(error);
        }
    }

    private void CloseSelectedSet()
    {
        SavedWindowSet? set = SelectedSet;
        if (set is null)
        {
            return;
        }

        try
        {
            SavedSetClosePlan plan = closer.FindOpenWindows(set, settings);
            if (plan.Targets.Count == 0)
            {
                ShowInlineReport(LocalizationService.Format(
                    "Sets_CloseSetNoMatches", plan.SkippedSavedEntries));
                return;
            }

            string windowNames = string.Join(Environment.NewLine,
                plan.Targets.Take(8).Select(target => "• " + target.Window.DisplayTitle));
            if (plan.Targets.Count > 8)
            {
                windowNames += Environment.NewLine
                    + LocalizationService.Format("Sets_CloseSetMore", plan.Targets.Count - 8);
            }

            string confirmation = LocalizationService.Format(
                "Sets_CloseSetConfirm", set.Name, plan.Targets.Count,
                plan.SkippedSavedEntries) + Environment.NewLine
                + Environment.NewLine + windowNames;

            if (MessageBox.Show(this, confirmation, Text,
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question)
                != DialogResult.Yes)
            {
                return;
            }

            // Lower our own panels so the target apps' native prompts about
            // unsaved work can appear in front. Never force-close a process.
            Form1.YieldTopmostForExternalWindows();
            SavedSetCloseResult result = closer.RequestClose(plan, settings);

            ShowInlineReport(LocalizationService.Format(
                "Sets_CloseSetSummary", result.Requested, result.Failed,
                result.NoLongerMatching, plan.SkippedSavedEntries));
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            ShowInlineReport(LocalizationService.Format("Sets_Error", error.Message));
        }
    }

    private void ShowInlineReport(string summary)
    {
        restoreReport.Text = summary;
        detailsLayout.RowStyles[2].Height = 115;
        restoreReport.Visible = true;
        restoreReport.SelectionStart = 0;
        restoreReport.ScrollToCaret();
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

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        // ApplyTitleBar called by the constructor has no effect before the
        // HWND exists. Set DWM title-bar colors once the native window is ready.
        ThemeManager.ApplyTitleBar(this, ThemeManager.Resolve(settings.Theme).IsDark);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Icon?.Dispose();
        }
        base.Dispose(disposing);
    }
}
