using WindowDeck.Localization;
using WindowDeck.Models;
using WindowDeck.Services;

namespace WindowDeck;

// Allows linking a document by its path without opening or locking it.
// The system file picker is optional: cloud-synced Office files may reject a
// native selection while the workbook is open in another application.
internal sealed class SavedWindowFilePathForm : Form
{
    private readonly TextBox pathBox;
    private readonly AppTheme appearance;

    public string SelectedPath => pathBox.Text.Trim().Trim('"');

    public SavedWindowFilePathForm(AppTheme theme, string? currentPath)
    {
        appearance = theme;
        ThemePalette palette = ThemeManager.Resolve(theme);
        Text = LocalizationService.Get("Sets_ChooseFile");
        Font = new Font("Segoe UI", 9F);
        AutoScaleMode = AutoScaleMode.Dpi;
        Icon = WindowDeckIcon.Load();
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        TopMost = true;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(690, 204);
        BackColor = palette.Background;
        ForeColor = palette.Foreground;

        TableLayoutPanel layout = new()
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(15),
            ColumnCount = 1,
            RowCount = 4
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        Label instruction = new()
        {
            AutoSize = true,
            MaximumSize = new Size(635, 0),
            Text = LocalizationService.Get("Sets_PastePathHelp"),
            ForeColor = palette.SecondaryForeground,
            Margin = new Padding(0, 0, 0, 10)
        };
        layout.Controls.Add(instruction, 0, 0);

        pathBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Text = currentPath ?? string.Empty,
            BackColor = palette.Surface,
            ForeColor = palette.Foreground,
            Margin = new Padding(0, 0, 0, 9)
        };
        layout.Controls.Add(pathBox, 0, 1);

        Button browse = new()
        {
            Text = LocalizationService.Get("Sets_BrowseFile"),
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 8)
        };
        ThemeManager.StyleButton(browse, palette);
        browse.Click += (_, _) =>
        {
            using OpenFileDialog picker = new()
            {
                Title = LocalizationService.Get("Sets_ChooseFile"),
                Filter = LocalizationService.Get("Sets_AllFilesFilter"),
                // Only select a path. Do not require the native dialog to open
                // an already-locked OneDrive/SharePoint document.
                CheckFileExists = false,
                Multiselect = false
            };
            if (picker.ShowDialog(this) == DialogResult.OK)
            {
                pathBox.Text = picker.FileName;
            }
        };
        layout.Controls.Add(browse, 0, 2);

        FlowLayoutPanel bottom = new()
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft
        };
        Button confirm = new() { Text = LocalizationService.Get("Common_OK"), AutoSize = true };
        Button cancel = new() { Text = LocalizationService.Get("Common_Cancel"), AutoSize = true };
        ThemeManager.StyleButton(confirm, palette);
        ThemeManager.StyleButton(cancel, palette);
        confirm.Click += (_, _) =>
        {
            if (!SavedWindowCaptureService.IsSupportedDocument(
                SavedWindowKind.DocumentFile, SelectedPath))
            {
                MessageBox.Show(this, LocalizationService.Get("Sets_UnsafeFile"),
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Do not open the file or check its sharing flags here.
            // Readiness will be checked by a worker after the dialog closes.
            DialogResult = DialogResult.OK;
            Close();
        };
        cancel.DialogResult = DialogResult.Cancel;
        bottom.Controls.Add(confirm);
        bottom.Controls.Add(cancel);
        layout.Controls.Add(bottom, 0, 3);

        Controls.Add(layout);
        AcceptButton = confirm;
        CancelButton = cancel;
        ThemeManager.ApplyTitleBar(this, palette.IsDark);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ThemeManager.ApplyTitleBar(this, ThemeManager.Resolve(appearance).IsDark);
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
