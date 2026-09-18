using System.Reflection;
using WindowDeck.Localization;
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
    private readonly Label designedFor;
    private readonly Label sourceAvailability;
    private AppTheme theme;

    public AboutForm(AppTheme theme)
    {
        this.theme = theme;
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(520, 315);
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = new Font("Segoe UI", 9F);
        Icon applicationIcon = WindowDeckIcon.Load();
        Icon = applicationIcon;

        content = new TableLayoutPanel
        {
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            Padding = new Padding(28, 22, 28, 22),
            RowCount = 7
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
        applicationName = CreateLabel(LocalizationService.Get("App_Title"), 16F, FontStyle.Bold);
        version = CreateLabel(string.Empty, 9F, FontStyle.Regular);
        description = CreateLabel(string.Empty, 9F, FontStyle.Regular);
        author = CreateLabel(string.Empty, 9F, FontStyle.Regular);
        designedFor = CreateLabel(string.Empty, 9F, FontStyle.Regular);
        sourceAvailability = CreateLabel(string.Empty, 9F, FontStyle.Regular);
        content.Controls.Add(iconView);
        content.Controls.Add(applicationName);
        content.Controls.Add(version);
        content.Controls.Add(description);
        content.Controls.Add(author);
        content.Controls.Add(designedFor);
        content.Controls.Add(sourceAvailability);

        ApplyLocalization();
        ApplyTheme();
    }

    public void SetTheme(AppTheme updatedTheme)
    {
        theme = updatedTheme;
        ApplyTheme();
    }

    public void RefreshTheme() => ApplyTheme();

    public void ApplyLocalization()
    {
        Text = LocalizationService.Get("About_Title");
        applicationName.Text = LocalizationService.Get("App_Title");
        version.Text = LocalizationService.Format("About_Version", GetApplicationVersion());
        description.Text = LocalizationService.Get("About_Description");
        author.Text = LocalizationService.Get("About_Author");
        designedFor.Text = LocalizationService.Get("About_DesignedFor");
        sourceAvailability.Text = LocalizationService.Get("About_Source");
    }

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
        Assembly.GetEntryAssembly()?.GetName().Version?.ToString()
        ?? LocalizationService.Get("About_UnknownVersion");

    private Label CreateLabel(string text, float size, FontStyle style) => new()
    {
        AutoSize = true,
        Dock = DockStyle.Fill,
        Font = new Font(Font.FontFamily, size, style),
        Margin = new Padding(0, 3, 0, 3),
        MaximumSize = new Size(450, 0),
        Text = text,
        TextAlign = ContentAlignment.MiddleCenter
    };

    private void ApplyTheme()
    {
        ThemePalette palette = ThemeManager.Resolve(theme);
        BackColor = palette.Background;
        ForeColor = palette.Foreground;
        content.BackColor = palette.Background;
        foreach (Label label in new[] { applicationName, version, description, author, designedFor, sourceAvailability })
        {
            label.BackColor = palette.Background;
            label.ForeColor = label == version || label == sourceAvailability
                ? palette.SecondaryForeground
                : palette.Foreground;
        }
        ThemeManager.ApplyTitleBar(this, palette.IsDark);
    }
}
