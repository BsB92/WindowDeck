using WindowDeck.Localization;
using WindowDeck.Models;
using WindowDeck.Services;

namespace WindowDeck;

internal sealed class PresentationConflictForm : Form
{
    private static Color ActiveButtonBackColor => SystemColors.Highlight;
    private static Color ActiveButtonForeColor => Color.Black;
    private const int MonitorButtonSize = 30;
    private readonly ThemePalette palette;
    private readonly IReadOnlyList<WindowInfo> windows;
    private readonly IReadOnlyList<MonitorDisplay> targetDisplays;
    private readonly FlowLayoutPanel rowsPanel;
    private readonly Dictionary<WindowIdentity, Button> addButtons = [];
    private readonly Dictionary<(WindowIdentity Window, int Monitor), Button> monitorButtons = [];

    public HashSet<WindowIdentity> AllowedWindows { get; } = [];
    public Dictionary<WindowIdentity, int> MoveTargets { get; } = [];
    public int? PreferredFallbackMonitorNumber { get; private set; }

    public PresentationConflictForm(
        IReadOnlyList<WindowInfo> windows,
        IReadOnlyList<MonitorDisplay> displays,
        int reservedMonitorNumber,
        AppTheme theme)
    {
        this.windows = windows;
        targetDisplays = displays
            .Where(display => display.Number.HasValue && display.Number.Value != reservedMonitorNumber)
            .ToArray();
        palette = ThemeManager.Resolve(theme);

        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        MaximizeBox = false;
        MinimumSize = new Size(680, 420);
        ClientSize = new Size(760, 520);
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = new Font("Segoe UI", 9F);
        Text = LocalizationService.Get("Flyout_PresentationConflictsTitle");
        Icon = WindowDeckIcon.Load();

        TableLayoutPanel root = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(16),
            BackColor = palette.Background
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(root);

        Label heading = CreateLabel(
            LocalizationService.Format("Flyout_PresentationConflictsHeading", reservedMonitorNumber),
            FontStyle.Bold);
        heading.Font = new Font(Font.FontFamily, 10F, FontStyle.Bold);
        heading.Margin = new Padding(0, 0, 0, 4);
        root.Controls.Add(heading, 0, 0);

        Label instruction = CreateLabel(
            LocalizationService.Get("Flyout_PresentationConflictsInstruction"),
            FontStyle.Regular);
        instruction.ForeColor = palette.SecondaryForeground;
        instruction.Margin = new Padding(0, 0, 0, 12);
        root.Controls.Add(instruction, 0, 1);

        rowsPanel = new FlowLayoutPanel
        {
            AutoScroll = true,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = palette.Background,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        root.Controls.Add(rowsPanel, 0, 2);

        foreach (WindowInfo window in windows)
        {
            rowsPanel.Controls.Add(CreateWindowRow(window));
        }

        TableLayoutPanel footer = new()
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 12, 0, 0)
        };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        root.Controls.Add(footer, 0, 3);

        FlowLayoutPanel moveAllPanel = new()
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Margin = Padding.Empty
        };
        Label moveAllLabel = CreateLabel(LocalizationService.Get("Flyout_MoveAllTo"), FontStyle.Regular);
        moveAllLabel.AutoSize = true;
        moveAllLabel.Margin = new Padding(0, 7, 8, 0);
        moveAllPanel.Controls.Add(moveAllLabel);
        foreach (MonitorDisplay display in targetDisplays)
        {
            int number = display.Number!.Value;
            Button button = CreateMonitorButton(number);
            button.Click += (_, _) =>
            {
                AllowedWindows.Clear();
                MoveTargets.Clear();
                foreach (WindowInfo window in windows)
                {
                    MoveTargets[WindowIdentity.From(window)] = number;
                }
                PreferredFallbackMonitorNumber = number;
                RefreshAllRowStyles();
            };
            moveAllPanel.Controls.Add(button);
        }
        footer.Controls.Add(moveAllPanel, 0, 0);

        FlowLayoutPanel commandPanel = new()
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = Padding.Empty
        };
        Button okButton = CreateCommandButton(LocalizationService.Get("Common_OK"));
        Button cancelButton = CreateCommandButton(LocalizationService.Get("Common_Cancel"));
        okButton.Click += (_, _) => AcceptSelection();
        cancelButton.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };
        commandPanel.Controls.Add(okButton);
        commandPanel.Controls.Add(cancelButton);
        footer.Controls.Add(commandPanel, 1, 0);

        CancelButton = cancelButton;
        Shown += (_, _) => SizeRows();
        Resize += (_, _) => SizeRows();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
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

    private Control CreateWindowRow(WindowInfo window)
    {
        WindowIdentity identity = WindowIdentity.From(window);
        TableLayoutPanel row = new()
        {
            ColumnCount = 3,
            Height = 42,
            Margin = new Padding(0, 0, 0, 6),
            BackColor = palette.RaisedSurface
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 38));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        Label title = new()
        {
            AutoEllipsis = true,
            Dock = DockStyle.Fill,
            ForeColor = palette.Foreground,
            Margin = new Padding(10, 0, 8, 0),
            Text = $"{window.ApplicationName} — {window.DisplayTitle}",
            TextAlign = ContentAlignment.MiddleLeft
        };
        row.Controls.Add(title, 0, 0);

        Button addButton = new()
        {
            AccessibleName = LocalizationService.Get("Flyout_AddToPresentation"),
            BackColor = palette.Surface,
            FlatStyle = FlatStyle.Flat,
            ForeColor = ActiveButtonBackColor,
            Font = new Font("Segoe UI", 11F, FontStyle.Bold),
            Margin = new Padding(3, 5, 3, 5),
            Size = new Size(32, 32),
            Text = "+",
            UseVisualStyleBackColor = false
        };
        addButton.FlatAppearance.BorderColor = ActiveButtonBackColor;
        addButton.FlatAppearance.BorderSize = 1;
        addButton.FlatAppearance.MouseOverBackColor = palette.Hover;
        addButton.Click += (_, _) =>
        {
            if (!AllowedWindows.Add(identity))
            {
                AllowedWindows.Remove(identity);
            }
            MoveTargets.Remove(identity);
            RefreshRowStyle(identity);
        };
        addButtons[identity] = addButton;
        row.Controls.Add(addButton, 1, 0);

        FlowLayoutPanel targets = new()
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(3, 4, 4, 4)
        };
        foreach (MonitorDisplay display in targetDisplays)
        {
            int number = display.Number!.Value;
            Button button = CreateMonitorButton(number);
            button.Click += (_, _) =>
            {
                AllowedWindows.Remove(identity);
                MoveTargets[identity] = number;
                PreferredFallbackMonitorNumber ??= number;
                RefreshRowStyle(identity);
            };
            monitorButtons[(identity, number)] = button;
            targets.Controls.Add(button);
        }
        row.Controls.Add(targets, 2, 0);
        return row;
    }

    private Button CreateMonitorButton(int number)
    {
        Button button = new()
        {
            AccessibleName = LocalizationService.Format("Flyout_MoveToScreen", number),
            BackColor = palette.Surface,
            FlatStyle = FlatStyle.Flat,
            ForeColor = palette.Foreground,
            Font = new Font(SystemFonts.MessageBoxFont.FontFamily, 8.5F, FontStyle.Bold),
            Margin = new Padding(0, 0, 4, 0),
            Size = new Size(MonitorButtonSize, MonitorButtonSize),
            Text = number.ToString(),
            UseVisualStyleBackColor = false
        };
        button.FlatAppearance.BorderColor = palette.Border;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.MouseOverBackColor = palette.Hover;
        button.FlatAppearance.MouseDownBackColor = palette.Pressed;
        return button;
    }

    private Button CreateCommandButton(string text)
    {
        Button button = new()
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(6, 0, 0, 0),
            MinimumSize = new Size(86, 32),
            Padding = new Padding(10, 2, 10, 2),
            Text = text,
            UseVisualStyleBackColor = false
        };
        ThemeManager.StyleButton(button, palette);
        return button;
    }

    private Label CreateLabel(string text, FontStyle style) => new()
    {
        AutoSize = true,
        Dock = DockStyle.Fill,
        Font = new Font(Font.FontFamily, 9F, style),
        ForeColor = palette.Foreground,
        Text = text,
        TextAlign = ContentAlignment.MiddleLeft
    };

    private void RefreshRowStyle(WindowIdentity identity)
    {
        bool allowed = AllowedWindows.Contains(identity);
        Button addButton = addButtons[identity];
        addButton.Text = allowed ? "✓" : "+";
        addButton.BackColor = allowed ? ActiveButtonBackColor : palette.Surface;
        addButton.ForeColor = allowed ? ActiveButtonForeColor : ActiveButtonBackColor;

        foreach (MonitorDisplay display in targetDisplays)
        {
            int number = display.Number!.Value;
            Button button = monitorButtons[(identity, number)];
            bool selected = MoveTargets.TryGetValue(identity, out int target) && target == number;
            button.BackColor = selected ? ActiveButtonBackColor : palette.Surface;
            button.ForeColor = selected ? ActiveButtonForeColor : palette.Foreground;
            button.FlatAppearance.BorderColor = selected ? ActiveButtonBackColor : palette.Border;
        }
    }

    private void RefreshAllRowStyles()
    {
        foreach (WindowInfo window in windows)
        {
            RefreshRowStyle(WindowIdentity.From(window));
        }
    }

    private void AcceptSelection()
    {
        bool unresolved = windows
            .Select(WindowIdentity.From)
            .Any(identity => !AllowedWindows.Contains(identity) && !MoveTargets.ContainsKey(identity));
        if (unresolved)
        {
            MessageBox.Show(
                this,
                LocalizationService.Get("Flyout_PresentationResolveAll"),
                LocalizationService.Get("App_Title"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        DialogResult = DialogResult.OK;
        Close();
    }

    private void SizeRows()
    {
        int width = Math.Max(520, rowsPanel.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 4);
        foreach (Control row in rowsPanel.Controls)
        {
            row.Width = width;
        }
    }
}
