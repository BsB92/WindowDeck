using WindowDeck.Models;
using WindowDeck.Services;

namespace WindowDeck;

internal sealed class HelpForm : Form
{
    private readonly TableLayoutPanel content;
    private readonly List<Label> headings = [];
    private AppTheme theme;

    public HelpForm(AppTheme theme)
    {
        this.theme = theme;
        Text = "Help";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = false;
        MinimizeBox = false;
        MinimumSize = new Size(540, 560);
        ClientSize = new Size(620, 680);
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = new Font("Segoe UI", 9F);
        Icon = WindowDeckIcon.Load();

        content = new TableLayoutPanel
        {
            AutoScroll = true,
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            Padding = new Padding(24, 12, 24, 20)
        };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        Controls.Add(content);

        AddSection("Opening WindowDeck",
            "Use the global hotkey to show or hide WindowDeck. The default shortcut is Win + `. " +
            "You can change it in Settings. You can also left-click the tray icon to show or hide WindowDeck.");
        AddSection("Finding windows",
            "Type in Search windows to filter by application or visible window/document title. Windows can be " +
            "grouped by application. The Screen column shows which screen contains each window, for example " +
            "[ 1 ], [ 2 ], [ 3 ], and so on.");
        AddSection("Window actions",
            "Click a title or row to activate that exact window. The — button minimizes that window. The × button " +
            "sends a normal close request; the application may still show its usual Save, Don't Save, or Cancel dialog.");
        AddSection("Tray behavior",
            "WindowDeck's X button and Esc hide the flyout. WindowDeck keeps running in the tray. To shut down the " +
            "application, use Tray > Exit.");
        AddSection("Settings",
            "Settings includes Start with Windows, Start minimized to tray, the global hotkey, grouping by application, " +
            "application icons, screen numbers, minimized windows, and Appearance (System, Light, or Dark).");

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
            Icon?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void AddSection(string heading, string text)
    {
        Label headingLabel = new()
        {
            AutoSize = true,
            Font = new Font(Font, FontStyle.Bold),
            Margin = new Padding(0, 14, 0, 5),
            Text = heading
        };
        headings.Add(headingLabel);
        content.Controls.Add(headingLabel);
        content.Controls.Add(new Label
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 5),
            MaximumSize = new Size(540, 0),
            Text = text
        });
    }

    private void ApplyTheme()
    {
        ThemePalette palette = ThemeManager.Resolve(theme);
        BackColor = palette.Background;
        ForeColor = palette.Foreground;
        content.BackColor = palette.Background;
        content.ForeColor = palette.Foreground;
        foreach (Label heading in headings)
        {
            heading.ForeColor = palette.Foreground;
        }

        ThemeManager.ApplyTitleBar(this, palette.IsDark);
    }
}
