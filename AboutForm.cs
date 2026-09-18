using System.Reflection;
using WindowDeck.Models;
using WindowDeck.Services;

namespace WindowDeck;

internal sealed class AboutForm : Form
{
    private readonly TableLayoutPanel content;
    private readonly PictureBox iconView;
    private readonly Label applicationName;
    private readonly Label version;
    private readonly Label description;
    private readonly Label author;
    private readonly Label sourceAvailability;
    private AppTheme theme;

    public AboutForm(AppTheme theme)
    {
        this.theme = theme;
        Text = "About WindowDeck";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(500, 270);
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = new Font("Segoe UI", 9F);
        Icon applicationIcon = WindowDeckIcon.Load();
        Icon = applicationIcon;

        content = new TableLayoutPanel
        {
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            Padding = new Padding(28, 22, 28, 22),
            RowCount = 6
        };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        Controls.Add(content);

        iconView = new PictureBox
        {
            Anchor = AnchorStyles.None,
            Image = applicationIcon.ToBitmap(),
            Margin = new Padding(0, 0, 0, 10),
            Size = new Size(48, 48),
            SizeMode = PictureBoxSizeMode.Zoom
        };
        applicationName = CreateLabel("WindowDeck", 16F, FontStyle.Bold);
        version = CreateLabel($"Version {GetApplicationVersion()}", 9F, FontStyle.Regular);
        description = CreateLabel(
            "A lightweight Windows utility for quickly finding and switching between open windows.",
            9F, FontStyle.Regular);
        author = CreateLabel("Created by ::BsB!::", 9F, FontStyle.Regular);
        sourceAvailability = CreateLabel(
            "Source code is included in the WindowDeck repository.",
            9F, FontStyle.Regular);
        content.Controls.Add(iconView);
        content.Controls.Add(applicationName);
        content.Controls.Add(version);
        content.Controls.Add(description);
        content.Controls.Add(author);
        content.Controls.Add(sourceAvailability);

        ApplyTheme();
    }

    public void SetTheme(AppTheme updatedTheme)
    {
        theme = updatedTheme;
        ApplyTheme();
    }

    public void RefreshTheme() => ApplyTheme();

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ThemeManager.ApplyTitleBar(this, ThemeManager.Resolve(theme).IsDark);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            iconView.Image?.Dispose();
            Icon?.Dispose();
        }

        base.Dispose(disposing);
    }

    private static string GetApplicationVersion() =>
        Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "Unknown";

    private Label CreateLabel(string text, float size, FontStyle style) => new()
    {
        AutoSize = true,
        Dock = DockStyle.Fill,
        Font = new Font(Font.FontFamily, size, style),
        Margin = new Padding(0, 3, 0, 3),
        MaximumSize = new Size(430, 0),
        Text = text,
        TextAlign = ContentAlignment.MiddleCenter
    };

    private void ApplyTheme()
    {
        ThemePalette palette = ThemeManager.Resolve(theme);
        BackColor = palette.Background;
        ForeColor = palette.Foreground;
        content.BackColor = palette.Background;
        foreach (Label label in new[] { applicationName, version, description, author, sourceAvailability })
        {
            label.BackColor = palette.Background;
            label.ForeColor = label == version || label == sourceAvailability
                ? palette.SecondaryForeground
                : palette.Foreground;
        }

        ThemeManager.ApplyTitleBar(this, palette.IsDark);
    }
}
