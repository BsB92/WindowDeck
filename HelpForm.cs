using WindowDeck.Localization;
using WindowDeck.Models;
using WindowDeck.Services;

namespace WindowDeck;

internal sealed class HelpForm : Form
{
    private static readonly (string Heading, string Text)[] Sections =
    [
        ("Help_OpeningHeading", "Help_OpeningText"),
        ("Help_FindingHeading", "Help_FindingText"),
        ("Help_ActionsHeading", "Help_ActionsText"),
        ("Help_PresentationHeading", "Help_PresentationText"),
        ("Help_TrayHeading", "Help_TrayText"),
        ("Help_SettingsHeading", "Help_SettingsText")
    ];

    private readonly Panel scrollHost;
    private readonly TableLayoutPanel content;
    private readonly List<(Label Heading, Label Text)> sectionControls = [];
    private AppTheme theme;

    public HelpForm(AppTheme theme)
    {
        this.theme = theme;
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = false;
        MinimizeBox = false;
        MinimumSize = new Size(600, 600);
        ClientSize = new Size(720, 760);
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = new Font("Segoe UI", 9F);
        Icon = WindowDeckIcon.Load();

        scrollHost = new Panel
        {
            AutoScroll = true,
            Dock = DockStyle.Fill
        };

        content = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            Dock = DockStyle.Top,
            Padding = new Padding(24, 12, 24, 20)
        };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        scrollHost.Controls.Add(content);
        Controls.Add(scrollHost);
        scrollHost.Resize += (_, _) => UpdateTextWidths();

        foreach ((string heading, string text) in Sections) AddSection(heading, text);
        UpdateTextWidths();
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
        Text = LocalizationService.Get("Help_Title");
        for (int index = 0; index < Sections.Length; index++)
        {
            sectionControls[index].Heading.Text = LocalizationService.Get(Sections[index].Heading);
            sectionControls[index].Text.Text = LocalizationService.Get(Sections[index].Text);
        }

        UpdateTextWidths();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ThemeManager.ApplyTitleBar(this, ThemeManager.Resolve(theme).IsDark);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) Icon?.Dispose();
        base.Dispose(disposing);
    }

    private void AddSection(string headingKey, string textKey)
    {
        Label heading = new()
        {
            AutoSize = true,
            Font = new Font(Font, FontStyle.Bold),
            Margin = new Padding(0, 14, 0, 5)
        };
        Label text = new()
        {
            AutoSize = true,
            Dock = DockStyle.Top,
            Margin = new Padding(0, 0, 0, 5),
            MaximumSize = new Size(640, 0)
        };
        sectionControls.Add((heading, text));
        content.Controls.Add(heading);
        content.Controls.Add(text);
    }

    private void UpdateTextWidths()
    {
        int availableWidth = Math.Max(
            360,
            scrollHost.ClientSize.Width
            - content.Padding.Horizontal
            - SystemInformation.VerticalScrollBarWidth
            - 8);

        foreach (var section in sectionControls)
        {
            section.Text.MaximumSize = new Size(availableWidth, 0);
        }
    }

    private void ApplyTheme()
    {
        ThemePalette palette = ThemeManager.Resolve(theme);
        BackColor = palette.Background;
        ForeColor = palette.Foreground;
        content.BackColor = palette.Background;
        content.ForeColor = palette.Foreground;
        foreach ((Label heading, _) in sectionControls) heading.ForeColor = palette.Foreground;
        ThemeManager.ApplyTitleBar(this, palette.IsDark);
    }
}
