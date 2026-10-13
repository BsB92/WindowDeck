using WindowDeck.Localization;
using WindowDeck.Models;
using WindowDeck.Services;

namespace WindowDeck;

internal sealed class TrayIconCleanupForm : Form
{
    public TrayIconCleanupForm(IReadOnlyList<TrayIconHistoryEntry> entries, AppTheme theme)
    {
        ThemePalette palette = ThemeManager.Resolve(theme);
        Text = LocalizationService.Get("Settings_TrayCleanupDialogTitle");
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimumSize = new Size(520, 320);
        ClientSize = new Size(680, 390);
        Font = new Font("Segoe UI", 9F);
        AutoScaleMode = AutoScaleMode.Dpi;
        Icon = WindowDeckIcon.Load();
        BackColor = palette.Background;
        ForeColor = palette.Foreground;

        TableLayoutPanel layout = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(18, 14, 18, 14),
            BackColor = palette.Background
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        Label explanation = new()
        {
            AutoSize = true,
            MaximumSize = new Size(630, 0),
            Text = LocalizationService.Format("Settings_TrayCleanupConfirm", entries.Count),
            ForeColor = palette.Foreground,
            Margin = new Padding(0, 0, 0, 10)
        };
        layout.Controls.Add(explanation, 0, 0);

        TextBox paths = new()
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            WordWrap = false,
            ScrollBars = ScrollBars.Both,
            Font = new Font("Consolas", 9F),
            BackColor = palette.Surface,
            ForeColor = palette.Foreground,
            Text = string.Join(Environment.NewLine,
                entries.Select(entry => entry.ExecutablePath)),
            Margin = Padding.Empty
        };
        layout.Controls.Add(paths, 0, 1);

        FlowLayoutPanel actions = new()
        {
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 12, 0, 0)
        };
        Button remove = new()
        {
            Text = LocalizationService.Get("Settings_TrayCleanupRemove"),
            AutoSize = true,
            DialogResult = DialogResult.OK,
            Padding = new Padding(8, 2, 8, 2)
        };
        Button cancel = new()
        {
            Text = LocalizationService.Get("Common_Cancel"),
            AutoSize = true,
            DialogResult = DialogResult.Cancel,
            Padding = new Padding(8, 2, 8, 2)
        };
        ThemeManager.StyleButton(remove, palette);
        ThemeManager.StyleButton(cancel, palette);
        actions.Controls.AddRange([remove, cancel]);
        layout.Controls.Add(actions, 0, 2);
        Controls.Add(layout);
        AcceptButton = remove;
        CancelButton = cancel;
        ThemeManager.ApplyTitleBar(this, palette.IsDark);
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
