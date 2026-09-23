using System.ComponentModel;
using System.Runtime.InteropServices;
using WindowDeck.Interop;
using WindowDeck.Localization;
using WindowDeck.Models;
using WindowDeck.Services;

namespace WindowDeck;

internal partial class Form1 : Form
{
    private const int DefaultPanelWidth = 680;
    private const int HtCaption = 2;
    private const int HtClient = 1;
    private const int HtLeft = 10;
    private const int HtBottomRight = 17;
    private const int WmNcHitTest = 0x0084;
    private const int WmWindowPositionChanging = 0x0046;
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoMove = 0x0002;
    private const int ActionColumnWidth = 32;
    private const int MonitorButtonSize = 26;
    private readonly WindowEnumerator windowEnumerator = new();
    private readonly WindowActivator windowActivator = new();
    private readonly WindowActions windowActions = new();
    private readonly MonitorDetector monitorDetector = new();
    private readonly ApplicationIconProvider applicationIconProvider = new();
    private readonly HashSet<string> collapsedApplicationIds = new(StringComparer.OrdinalIgnoreCase);
    private AppSettings settings;
    private IReadOnlyList<WindowInfo> currentSnapshot = [];
    private bool currentSnapshotInitialized;
    private WindowEventMonitor? windowEventMonitor;
    private bool isClosing;
    private bool monitoringStarted;
    private bool allowApplicationExit;
    private int anchoredOuterRight;
    private int anchoredOuterTop;
    private int anchoredOuterHeight;
    private int maximumPanelWidth;
    private ThemePalette palette;
    private IReadOnlyList<MonitorDisplay> displays = [];

    public event EventHandler? SettingsRequested;
    public event EventHandler? HelpRequested;

    public Form1(AppSettings settings)
    {
        this.settings = settings.Copy();
        InitializeComponent();
        Icon = WindowDeckIcon.Load();
        searchTextBox.Enter += (_, _) => searchTextBox.BackColor = palette.RaisedSurface;
        searchTextBox.Leave += (_, _) => searchTextBox.BackColor = palette.Surface;
        settingsButton.Click += (_, _) => SettingsRequested?.Invoke(this, EventArgs.Empty);
        helpButton.Click += (_, _) => HelpRequested?.Invoke(this, EventArgs.Empty);
        ApplyLocalization();
        ApplyTheme();
        PositionOnRelevantMonitor(DefaultPanelWidth);
    }

    public void InitializeWhileHidden()
    {
        _ = Handle;
        RefreshWindowList();
        EnsureMonitoringStarted();
    }

    public void ApplySettings(AppSettings updatedSettings)
    {
        bool languageChanged = settings.Language != updatedSettings.Language;
        settings = updatedSettings.Copy();
        ApplyLocalization();
        ApplyTheme();
        if (languageChanged)
        {
            currentSnapshotInitialized = false;
            RefreshWindowList();
        }
        else
        {
            RenderWindowList();
        }
    }

    public void RefreshTheme()
    {
        ThemePalette resolvedPalette = ThemeManager.Resolve(settings.Theme);
        if (resolvedPalette == palette)
        {
            return;
        }

        ApplyTheme();
        RenderWindowList();
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        RefreshWindowList();
        EnsureMonitoringStarted();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ThemeManager.ApplyTitleBar(this, palette.IsDark);
    }

    private void EnsureMonitoringStarted()
    {
        if (monitoringStarted)
        {
            return;
        }

        monitoringStarted = true;
        if (!WindowEventMonitor.TryStart(
                RequestAutomaticRefresh,
                out windowEventMonitor,
                out string? errorMessage))
        {
            MessageBox.Show(
                this,
                errorMessage,
                LocalizationService.Get("App_Title"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!allowApplicationExit && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
            return;
        }

        isClosing = true;
        windowEventMonitor?.Dispose();
        windowEventMonitor = null;
        base.OnFormClosing(e);
    }

    public void ShowFlyout()
    {
        PositionOnRelevantMonitor(Width);
        ShowPositionedFlyout();
    }

    public void ShowFlyoutOnCursorMonitor()
    {
        if (!PositionOnCursorMonitor(Width))
        {
            PositionOnRelevantMonitor(Width);
        }

        ShowPositionedFlyout();
    }

    private void ShowPositionedFlyout()
    {
        RefreshWindowList();
        Show();
        Activate();
    }

    public void ExitApplication()
    {
        allowApplicationExit = true;
        Close();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.Escape)
        {
            Hide();
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        SizeWindowRows();
    }

    protected override void SetBoundsCore(
        int x,
        int y,
        int width,
        int height,
        BoundsSpecified specified)
    {
        if (anchoredOuterRight != 0 && WindowState == FormWindowState.Normal)
        {
            int minimumWidth = Math.Min(MinimumSize.Width, maximumPanelWidth);
            width = Math.Clamp(width, minimumWidth, maximumPanelWidth);
            x = anchoredOuterRight - width;
            y = anchoredOuterTop;
            height = anchoredOuterHeight;
            specified = BoundsSpecified.All;
        }

        base.SetBoundsCore(x, y, width, height, specified);
    }

    protected override void WndProc(ref Message message)
    {
        if (message.Msg == WmWindowPositionChanging && anchoredOuterRight != 0)
        {
            NativeMethods.WindowPosition position =
                Marshal.PtrToStructure<NativeMethods.WindowPosition>(message.LParam);
            int width = (position.Flags & SwpNoSize) != 0 ? Width : position.Width;
            int minimumWidth = Math.Min(MinimumSize.Width, maximumPanelWidth);
            position.Width = Math.Clamp(width, minimumWidth, maximumPanelWidth);
            position.X = anchoredOuterRight - position.Width;
            position.Y = anchoredOuterTop;
            position.Height = anchoredOuterHeight;
            position.Flags &= ~(SwpNoSize | SwpNoMove);
            Marshal.StructureToPtr(position, message.LParam, false);
        }

        base.WndProc(ref message);

        if (message.Msg != WmNcHitTest)
        {
            return;
        }

        int hitTest = (int)message.Result;
        if (hitTest == HtCaption || (hitTest > HtLeft && hitTest <= HtBottomRight))
        {
            message.Result = HtClient;
        }
    }

    private void SearchTextBox_TextChanged(object sender, EventArgs e)
    {
        RenderWindowList();
    }

    private void ActivateWindow(WindowInfo window)
    {
        WindowActivationResult result = windowActivator.Activate(window);
        switch (result)
        {
            case WindowActivationResult.Activated:
                break;
            case WindowActivationResult.WindowUnavailable:
                ShowActivationFailure(
                    LocalizationService.Get("Window_UnavailableRetry"));
                break;
            case WindowActivationResult.RestorationFailed:
                ShowActivationFailure(LocalizationService.Get("Window_RestoreFailed"));
                break;
            case WindowActivationResult.ForegroundActivationFailed:
                ShowActivationFailure(
                    LocalizationService.Get("Window_ActivateFailed"));
                break;
        }
    }

    private void RequestAutomaticRefresh()
    {
        if (isClosing || IsDisposed || Disposing || !IsHandleCreated)
        {
            return;
        }

        try
        {
            BeginInvoke((Action)(() =>
            {
                if (!isClosing && !IsDisposed && !Disposing)
                {
                    RefreshWindowList();
                }
            }));
        }
        catch (InvalidOperationException) when (isClosing || IsDisposed || Disposing)
        {
            // The form began shutting down between the state check and BeginInvoke.
        }
    }

    private void ShowActivationFailure(string message)
    {
        MessageBox.Show(
            this,
            message,
            LocalizationService.Get("App_Title"),
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private void RefreshWindowList()
    {
        try
        {
            IReadOnlyList<WindowInfo> updatedSnapshot = windowEnumerator.Enumerate();
            if (currentSnapshotInitialized && currentSnapshot.SequenceEqual(updatedSnapshot))
            {
                return;
            }

            currentSnapshot = updatedSnapshot;
            currentSnapshotInitialized = true;
            RenderWindowList();
            applicationIconProvider.RetainIconsFor(
                currentSnapshot.Select(window => window.ProcessId));
        }
        catch (Win32Exception exception)
        {
            MessageBox.Show(
                this,
                exception.Message,
                LocalizationService.Get("App_Title"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void RenderWindowList()
    {
        displays = monitorDetector.GetDisplays();
        IReadOnlyList<IGrouping<string, WindowInfo>> groups =
            WindowListPresentation.Create(
                currentSnapshot,
                searchTextBox.Text,
                settings.GroupByApplication,
                settings.ShowMinimizedWindows);
        int visibleCount = groups.Sum(group => group.Count());
        bool searchActive = searchTextBox.Text.Trim().Length > 0;

        windowListPanel.SuspendLayout();
        try
        {
            foreach (Control control in windowListPanel.Controls.Cast<Control>().ToArray())
            {
                control.Dispose();
            }

            windowListPanel.Controls.Clear();

            windowListPanel.Controls.Add(CreateColumnHeader());

            if (visibleCount == 0)
            {
                Label emptyLabel = new()
                {
                    AutoSize = false,
                    Height = 44,
                    Margin = new Padding(8),
                    Text = searchTextBox.TextLength == 0
                        ? LocalizationService.Get("Flyout_NoWindows")
                        : LocalizationService.Get("Flyout_NoMatches"),
                    TextAlign = ContentAlignment.MiddleCenter
                };
                emptyLabel.ForeColor = palette.SecondaryForeground;
                windowListPanel.Controls.Add(emptyLabel);
            }
            else
            {
                foreach (IGrouping<string, WindowInfo> group in groups)
                {
                    if (settings.GroupByApplication)
                    {
                        bool collapsed = collapsedApplicationIds.Contains(group.Key);
                        windowListPanel.Controls.Add(CreateGroupHeader(
                            group.Key,
                            group.First().ApplicationName,
                            collapsed && !searchActive,
                            group.ToArray()));
                        if (collapsed && !searchActive)
                        {
                            continue;
                        }
                    }
                    foreach (WindowInfo window in group)
                    {
                        windowListPanel.Controls.Add(CreateWindowRow(window));
                    }
                }
            }

            SizeWindowRows();
        }
        finally
        {
            windowListPanel.ResumeLayout();
        }
    }

    private Control CreateColumnHeader()
    {
        TableLayoutPanel header = CreateListGrid(24, new Padding(4, 1, 4, 0));
        Label screenHeader = new()
        {
            Dock = DockStyle.Fill,
            Font = new Font(SystemFonts.MessageBoxFont.FontFamily, 8.5F, FontStyle.Regular),
            ForeColor = palette.SecondaryForeground,
            Margin = Padding.Empty,
            Text = LocalizationService.Get("Flyout_Screen"),
            TextAlign = ContentAlignment.MiddleCenter
        };

        if (settings.ShowScreenNumber)
        {
            header.Controls.Add(screenHeader, 4, 0);
        }
        return header;
    }

    private Control CreateGroupHeader(
        string applicationId,
        string applicationName,
        bool visuallyCollapsed,
        IReadOnlyList<WindowInfo> windows)
    {
        TableLayoutPanel header = new()
        {
            ColumnCount = 4,
            Dock = DockStyle.Top,
            Height = 25,
            Margin = new Padding(4, 6, 4, 0),
            BackColor = palette.RaisedSurface,
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 32));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, ActionColumnWidth));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, ActionColumnWidth));

        Button collapseButton = CreateActionButton(visuallyCollapsed ? "▶" : "▼",
            LocalizationService.Get(visuallyCollapsed ? "Flyout_ExpandGroup" : "Flyout_CollapseGroup"), false);
        Label nameLabel = new()
        {
            AutoEllipsis = true,
            Dock = DockStyle.Fill,
            Font = new Font(SystemFonts.MessageBoxFont, FontStyle.Bold),
            ForeColor = palette.Foreground,
            Text = applicationName,
            TextAlign = ContentAlignment.MiddleLeft
        };
        void ToggleGroup(object? sender, EventArgs e)
        {
            if (!collapsedApplicationIds.Remove(applicationId))
            {
                collapsedApplicationIds.Add(applicationId);
            }
            RenderWindowList();
        }
        collapseButton.Click += ToggleGroup;
        nameLabel.Click += ToggleGroup;

        Button minimizeButton = CreateActionButton("—", LocalizationService.Get("Flyout_MinimizeAllTooltip"), false);
        minimizeButton.Click += (_, _) => RunForGroup(windows, windowActions.Minimize);
        Button closeButton = CreateActionButton("×", LocalizationService.Get("Flyout_CloseAllTooltip"), true);
        closeButton.Click += (_, _) => ConfirmAndCloseGroup(windows);
        toolTip.SetToolTip(collapseButton, collapseButton.AccessibleName);
        toolTip.SetToolTip(minimizeButton, minimizeButton.AccessibleName);
        toolTip.SetToolTip(closeButton, closeButton.AccessibleName);
        header.Controls.Add(collapseButton, 0, 0);
        header.Controls.Add(nameLabel, 1, 0);
        header.Controls.Add(minimizeButton, 2, 0);
        header.Controls.Add(closeButton, 3, 0);
        return header;
    }

    private Control CreateWindowRow(WindowInfo window)
    {
        int monitorRows = displays.Count > 1 ? (displays.Count + 3) / 4 : 1;
        TableLayoutPanel row = CreateListGrid(Math.Max(32, monitorRows * MonitorButtonSize), new Padding(4, 0, 4, 1));
        row.BackColor = palette.Surface;

        Button titleButton = new()
        {
            AutoEllipsis = true,
            Dock = DockStyle.Fill,
            FlatStyle = FlatStyle.Flat,
            Margin = Padding.Empty,
            Padding = new Padding(6, 0, 3, 0),
            Text = window.DisplayTitle,
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = palette.Surface,
            ForeColor = palette.Foreground,
            UseVisualStyleBackColor = false
        };
        titleButton.FlatAppearance.BorderSize = 0;
        titleButton.FlatAppearance.MouseOverBackColor = palette.Hover;
        titleButton.FlatAppearance.MouseDownBackColor = palette.Pressed;
        titleButton.Click += (_, _) => ActivateWindow(window);

        Button minimizeButton = CreateActionButton(
            "—",
            LocalizationService.Format("Flyout_MinimizeAccessible", window.DisplayTitle),
            isCloseButton: false);
        minimizeButton.Click += (_, _) =>
        {
            if (!windowActions.Minimize(window))
            {
                ShowWindowActionFailure();
            }
        };

        Button closeButton = CreateActionButton(
            "×",
            LocalizationService.Format("Flyout_CloseAccessible", window.DisplayTitle),
            isCloseButton: true);
        closeButton.Click += (_, _) =>
        {
            if (!windowActions.RequestClose(window))
            {
                ShowWindowActionFailure();
            }
        };

        if (settings.ShowApplicationIcons)
        {
            PictureBox applicationIcon = new()
            {
                Anchor = AnchorStyles.None,
                Image = applicationIconProvider.GetIcon(window),
                Margin = new Padding(4),
                Size = new Size(16, 16),
                SizeMode = PictureBoxSizeMode.Zoom
            };
            row.Controls.Add(applicationIcon, 0, 0);
        }
        row.Controls.Add(titleButton, 1, 0);
        row.Controls.Add(minimizeButton, 2, 0);
        row.Controls.Add(closeButton, 3, 0);
        if (settings.ShowScreenNumber && displays.Count > 1)
        {
            row.Controls.Add(CreateMonitorButtons(window), 4, 0);
        }
        return row;
    }

    private Control CreateMonitorButtons(WindowInfo window)
    {
        FlowLayoutPanel panel = new()
        {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            WrapContents = true
        };
        foreach (MonitorDisplay display in displays)
        {
            bool active = display.Number == window.MonitorNumber;
            Button button = new()
            {
                AccessibleName = LocalizationService.Format("Flyout_MoveToScreen", display.Number),
                BackColor = active ? palette.Pressed : palette.Surface,
                Enabled = !active,
                FlatStyle = FlatStyle.Flat,
                Margin = Padding.Empty,
                Size = new Size(MonitorButtonSize, MonitorButtonSize),
                Text = display.Number?.ToString(),
                UseVisualStyleBackColor = false
            };
            button.FlatAppearance.BorderSize = active ? 2 : 1;
            button.Click += (_, _) =>
            {
                if (!windowActions.MoveToMonitor(window, display)) ShowWindowActionFailure();
            };
            toolTip.SetToolTip(button, button.AccessibleName);
            panel.Controls.Add(button);
        }
        return panel;
    }

    private TableLayoutPanel CreateListGrid(int height, Padding margin)
    {
        TableLayoutPanel grid = new()
        {
            ColumnCount = 5,
            Height = height,
            Margin = margin,
            RowCount = 1
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, settings.ShowApplicationIcons ? 24 : 0));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, ActionColumnWidth));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, ActionColumnWidth));
        int monitorWidth = settings.ShowScreenNumber && displays.Count > 1
            ? MonitorButtonSize * Math.Min(4, displays.Count)
            : 0;
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, monitorWidth));
        return grid;
    }

    private void RunForGroup(IEnumerable<WindowInfo> windows, Func<WindowInfo, bool> action)
    {
        bool success = true;
        foreach (WindowInfo window in windows) success &= action(window);
        if (!success) ShowWindowActionFailure();
    }

    private void ConfirmAndCloseGroup(IReadOnlyList<WindowInfo> windows)
    {
        TaskDialogButton closeAll = new(LocalizationService.Get("Flyout_CloseAll"));
        TaskDialogButton cancel = new(LocalizationService.Get("Common_Cancel"));
        TaskDialogPage page = new()
        {
            Caption = LocalizationService.Get("App_Title"),
            Heading = LocalizationService.Get("Flyout_CloseAllConfirmation"),
            Text = LocalizationService.Format("Flyout_CloseAllCount", windows.Count),
            Icon = TaskDialogIcon.Warning,
            AllowCancel = true,
            DefaultButton = cancel
        };
        page.Buttons.Add(closeAll);
        page.Buttons.Add(cancel);
        if (TaskDialog.ShowDialog(this, page) == closeAll)
        {
            RunForGroup(windows, windowActions.RequestClose);
        }
    }

    private void ApplyTheme()
    {
        palette = ThemeManager.Resolve(settings.Theme);
        BackColor = palette.Background;
        ForeColor = palette.Foreground;
        searchTextBox.BackColor = palette.Surface;
        searchTextBox.ForeColor = palette.Foreground;
        windowListPanel.BackColor = palette.Background;
        windowListPanel.ForeColor = palette.Foreground;
        bottomBar.BackColor = palette.RaisedSurface;
        presentationModeButton.BackColor = palette.RaisedSurface;
        helpButton.BackColor = palette.RaisedSurface;
        settingsButton.BackColor = palette.RaisedSurface;
        presentationModeButton.ForeColor = palette.SecondaryForeground;
        helpButton.ForeColor = palette.Foreground;
        settingsButton.ForeColor = palette.Foreground;
        ThemeManager.ApplyTitleBar(this, palette.IsDark);
    }

    private void ApplyLocalization()
    {
        Text = LocalizationService.Get("App_Title");
        searchTextBox.PlaceholderText = LocalizationService.Get("Flyout_Search");
        presentationModeButton.Text = LocalizationService.Get("Flyout_PresentationMode");
        helpButton.Text = LocalizationService.Get("Tray_Help");
        settingsButton.Text = LocalizationService.Get("Tray_Settings");
        toolTip.SetToolTip(presentationModeButton, LocalizationService.Get("Flyout_ComingSoon"));
    }

    private Button CreateActionButton(
        string text,
        string accessibleName,
        bool isCloseButton)
    {
        Button button = new()
        {
            AccessibleName = accessibleName,
            BackColor = palette.Surface,
            Dock = DockStyle.Fill,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Symbol", 10F, FontStyle.Regular),
            ForeColor = palette.Foreground,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            Text = text,
            UseVisualStyleBackColor = false
        };
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseDownBackColor = isCloseButton
            ? palette.ClosePressed
            : palette.Pressed;
        button.FlatAppearance.MouseOverBackColor = isCloseButton
            ? palette.CloseHover
            : palette.Hover;

        if (isCloseButton)
        {
            button.MouseEnter += (_, _) => button.ForeColor = Color.White;
            button.MouseLeave += (_, _) => button.ForeColor = palette.Foreground;
        }

        return button;
    }

    private void ShowWindowActionFailure()
    {
        MessageBox.Show(
            this,
            LocalizationService.Get("Window_Unavailable"),
            LocalizationService.Get("App_Title"),
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private bool PositionOnCursorMonitor(int requestedWidth)
    {
        if (!NativeMethods.GetCursorPos(out NativeMethods.NativePoint cursor))
        {
            return false;
        }

        nint monitorHandle = NativeMethods.MonitorFromPoint(
            cursor,
            NativeMethods.MonitorDefaultToNull);
        return PositionOnMonitor(monitorHandle, requestedWidth);
    }

    private bool PositionOnRelevantMonitor(int requestedWidth)
    {
        nint monitorHandle = 0;
        nint foregroundWindow = NativeMethods.GetForegroundWindow();
        if (foregroundWindow != 0)
        {
            NativeMethods.GetWindowThreadProcessId(foregroundWindow, out uint processId);
            if (processId != (uint)Environment.ProcessId)
            {
                monitorHandle = NativeMethods.MonitorFromWindow(
                    foregroundWindow,
                    NativeMethods.MonitorDefaultToNull);
            }
        }

        if (monitorHandle == 0 && NativeMethods.GetCursorPos(out NativeMethods.NativePoint cursor))
        {
            monitorHandle = NativeMethods.MonitorFromPoint(
                cursor,
                NativeMethods.MonitorDefaultToNearest);
        }

        if (monitorHandle == 0)
        {
            return false;
        }

        return PositionOnMonitor(monitorHandle, requestedWidth);
    }

    private bool PositionOnMonitor(nint monitorHandle, int requestedWidth)
    {
        if (monitorHandle == 0)
        {
            return false;
        }

        NativeMethods.MonitorInfoEx monitorInfo = new()
        {
            Size = (uint)Marshal.SizeOf<NativeMethods.MonitorInfoEx>()
        };

        if (!NativeMethods.GetMonitorInfo(monitorHandle, ref monitorInfo))
        {
            return false;
        }

        NativeMethods.Rect workArea = monitorInfo.WorkArea;
        int width = Math.Min(Math.Max(requestedWidth, MinimumSize.Width), workArea.Right - workArea.Left);

        // Start with the work area so DWM can report the actual visible frame in
        // relation to the outer Win32 bounds, including invisible resize borders.
        anchoredOuterRight = 0;
        Bounds = new Rectangle(
            workArea.Right - width,
            workArea.Top,
            width,
            workArea.Bottom - workArea.Top);

        NativeMethods.Rect outerBounds = new()
        {
            Left = Left,
            Top = Top,
            Right = Right,
            Bottom = Bottom
        };
        int frameResult = NativeMethods.DwmGetWindowAttribute(
            Handle,
            NativeMethods.DwmaExtendedFrameBounds,
            out NativeMethods.Rect visibleFrame,
            Marshal.SizeOf<NativeMethods.Rect>());
        if (frameResult != 0)
        {
            visibleFrame = outerBounds;
        }

        int rightInvisibleBorder = outerBounds.Right - visibleFrame.Right;
        int topInvisibleBorder = visibleFrame.Top - outerBounds.Top;
        int bottomInvisibleBorder = outerBounds.Bottom - visibleFrame.Bottom;

        anchoredOuterRight = workArea.Right + rightInvisibleBorder;
        anchoredOuterTop = workArea.Top - topInvisibleBorder;
        anchoredOuterHeight = workArea.Bottom - workArea.Top
            + topInvisibleBorder
            + bottomInvisibleBorder;
        maximumPanelWidth = workArea.Right - workArea.Left;
        Bounds = new Rectangle(
            anchoredOuterRight - width,
            anchoredOuterTop,
            width,
            anchoredOuterHeight);
        return true;
    }

    private void SizeWindowRows()
    {
        int width = Math.Max(
            120,
            windowListPanel.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 10);
        foreach (Control control in windowListPanel.Controls)
        {
            control.Width = width;
        }
    }
}
