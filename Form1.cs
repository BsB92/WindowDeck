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
    private const int ActionColumnWidth = 42;
    private const int PresentationActionColumnWidth = 46;
    private const int CompactActionButtonSize = 26;
    private const int MonitorButtonSize = 26;
    private const int MonitorButtonGap = 3;
    private const int MonitorActionGap = 10;
    private static Color ActiveButtonBackColor => SystemColors.Highlight;
    private static Color ActiveButtonForeColor => Color.Black;
    private readonly WindowEnumerator windowEnumerator = new();
    private readonly WindowActivator windowActivator = new();
    private readonly WindowActions windowActions = new();
    private readonly MonitorDetector monitorDetector = new();
    private readonly ApplicationIconProvider applicationIconProvider = new();
    private readonly HashSet<string> collapsedApplicationIds = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<WindowIdentity> presentationWindowIds = [];
    private readonly Dictionary<WindowIdentity, WindowRowCacheEntry> windowRowCache = [];
    private ContextMenuStrip groupContextMenu = null!;
    private ToolStripMenuItem collapseAllItem = null!;
    private ToolStripMenuItem expandAllItem = null!;
    private bool presentationModeEnabled;
    private bool presentationLocked;
    private int? presentationMonitorNumber;
    private int? presentationFallbackMonitorNumber;
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

    private sealed record WindowRowCacheEntry(
        WindowInfo Snapshot,
        bool PresentationMember,
        Control Control);

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
        presentationModeButton.Click += PresentationModeButton_Click;
        InitializeGroupContextMenu();
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
        bool rowAppearanceChanged = languageChanged
            || settings.ShowApplicationIcons != updatedSettings.ShowApplicationIcons
            || settings.ShowScreenNumber != updatedSettings.ShowScreenNumber
            || settings.Theme != updatedSettings.Theme;
        settings = updatedSettings.Copy();
        if (rowAppearanceChanged)
        {
            ClearWindowRowCache();
        }

        ApplyLocalization();
        ApplyTheme();
        if (languageChanged)
        {
            windowEnumerator.ResetApplicationMetadataCache();
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

        ClearWindowRowCache();
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
        ClearWindowRowCache();
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

    private void RefreshWindowList(bool force = false)
    {
        try
        {
            IReadOnlyList<WindowInfo> updatedSnapshot = windowEnumerator.Enumerate();
            updatedSnapshot = EnforcePresentationReservation(updatedSnapshot);
            PrunePresentationWindows(updatedSnapshot);
            if (!force && currentSnapshotInitialized && currentSnapshot.SequenceEqual(updatedSnapshot))
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
        IReadOnlyList<MonitorDisplay> updatedDisplays = monitorDetector.GetDisplays();
        if (!displays.SequenceEqual(updatedDisplays))
        {
            displays = updatedDisplays;
            ClearWindowRowCache();
        }
        else
        {
            displays = updatedDisplays;
        }

        PruneWindowRowCache();
        IReadOnlyList<WindowInfo> presentationWindows = presentationModeEnabled
            ? currentSnapshot
                .Where(window => presentationWindowIds.Contains(WindowIdentity.From(window)))
                .OrderBy(window => window.ApplicationName, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(window => window.DisplayTitle, StringComparer.CurrentCultureIgnoreCase)
                .ToArray()
            : [];
        IReadOnlyList<WindowInfo> regularSnapshot = presentationModeEnabled
            ? currentSnapshot
                .Where(window => !presentationWindowIds.Contains(WindowIdentity.From(window)))
                .ToArray()
            : currentSnapshot;
        IReadOnlyList<IGrouping<string, WindowInfo>> groups =
            WindowListPresentation.Create(
                regularSnapshot,
                searchTextBox.Text,
                settings.GroupByApplication,
                settings.ShowMinimizedWindows);
        int visibleCount = groups.Sum(group => group.Count()) + presentationWindows.Count;
        bool searchActive = searchTextBox.Text.Trim().Length > 0;

        windowListPanel.BeginUpdate();
        try
        {
            HashSet<Control> cachedRows = windowRowCache.Values
                .Select(entry => entry.Control)
                .ToHashSet<Control>(ReferenceEqualityComparer.Instance);
            foreach (Control control in windowListPanel.Controls.Cast<Control>().ToArray())
            {
                if (!cachedRows.Contains(control))
                {
                    control.Dispose();
                }
            }

            windowListPanel.Controls.Clear();

            if (presentationModeEnabled)
            {
                windowListPanel.Controls.Add(CreatePresentationGroupHeader(presentationWindows));
                foreach (WindowInfo presentationWindow in presentationWindows)
                {
                    windowListPanel.Controls.Add(GetOrCreateWindowRow(presentationWindow, presentationMember: true));
                }
            }

            windowListPanel.Controls.Add(CreateColumnHeader());

            if (visibleCount == 0 && !presentationModeEnabled)
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
                        windowListPanel.Controls.Add(GetOrCreateWindowRow(window, presentationMember: false));
                    }
                }
            }

        }
        finally
        {
            windowListPanel.EndUpdate();
        }

        SizeWindowRows();
    }

    private Control GetOrCreateWindowRow(WindowInfo window, bool presentationMember)
    {
        WindowIdentity identity = WindowIdentity.From(window);
        if (windowRowCache.TryGetValue(identity, out WindowRowCacheEntry? cached))
        {
            if (cached.Snapshot == window
                && cached.PresentationMember == presentationMember
                && !cached.Control.IsDisposed)
            {
                return cached.Control;
            }

            cached.Control.Dispose();
            windowRowCache.Remove(identity);
        }

        Control row = CreateWindowRow(window, presentationMember);
        windowRowCache[identity] = new WindowRowCacheEntry(window, presentationMember, row);
        return row;
    }

    private void PruneWindowRowCache()
    {
        HashSet<WindowIdentity> activeWindows = currentSnapshot
            .Select(WindowIdentity.From)
            .ToHashSet();
        foreach (WindowIdentity identity in windowRowCache.Keys
                     .Where(identity => !activeWindows.Contains(identity))
                     .ToArray())
        {
            windowRowCache[identity].Control.Dispose();
            windowRowCache.Remove(identity);
        }
    }

    private void ClearWindowRowCache()
    {
        foreach (WindowRowCacheEntry entry in windowRowCache.Values)
        {
            entry.Control.Dispose();
        }

        windowRowCache.Clear();
    }

    private Control CreateColumnHeader()
    {
        TableLayoutPanel header = CreateListGrid(24, new Padding(4, 1, 4, 0));

        if (presentationModeEnabled)
        {
            Label presentationHeader = new()
            {
                Dock = DockStyle.Fill,
                Font = new Font(SystemFonts.MessageBoxFont.FontFamily, 8F, FontStyle.Bold),
                ForeColor = palette.SecondaryForeground,
                Margin = Padding.Empty,
                Text = LocalizationService.Get("Flyout_PresentationColumnHeader"),
                TextAlign = ContentAlignment.MiddleCenter
            };
            header.Controls.Add(presentationHeader, 2, 0);
        }

        if (settings.ShowScreenNumber)
        {
            Label screenHeader = new()
            {
                Dock = DockStyle.Fill,
                Font = new Font(SystemFonts.MessageBoxFont.FontFamily, 8.5F, FontStyle.Bold),
                ForeColor = palette.Foreground,
                Margin = Padding.Empty,
                Text = LocalizationService.Get("Flyout_Screen"),
                TextAlign = ContentAlignment.MiddleCenter
            };
            header.Controls.Add(screenHeader, 5, 0);
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
            ColumnCount = 5,
            ContextMenuStrip = groupContextMenu,
            Dock = DockStyle.Top,
            Height = 31,
            Margin = new Padding(4, 8, 4, 2),
            BackColor = palette.RaisedSurface,
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 32));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, ActionColumnWidth));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, ActionColumnWidth));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, ActionColumnWidth));

        Button collapseButton = CreateActionButton(
            visuallyCollapsed ? "▶" : "▼",
            LocalizationService.Get(visuallyCollapsed ? "Flyout_ExpandGroup" : "Flyout_CollapseGroup"),
            false);
        StyleCompactActionButton(collapseButton);

        Label nameLabel = new()
        {
            AutoEllipsis = true,
            Dock = DockStyle.Fill,
            Font = new Font(SystemFonts.MessageBoxFont, FontStyle.Bold),
            ForeColor = palette.Foreground,
            Margin = new Padding(2, 0, 6, 0),
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

        Button restoreButton = CreateActionButton(
            "□",
            LocalizationService.Get("Flyout_RestoreAllTooltip"),
            false);
        StyleCompactActionButton(restoreButton);
        StyleBlueActionButton(restoreButton);
        restoreButton.Click += (_, _) => RunForGroup(windows, windowActions.Restore);

        Button minimizeButton = CreateActionButton(
            "—",
            LocalizationService.Get("Flyout_MinimizeAllTooltip"),
            false);
        StyleCompactActionButton(minimizeButton);
        SetCenteredActionGlyph(minimizeButton, CompactGlyph.Minus);
        StyleBlueActionButton(minimizeButton);
        minimizeButton.Click += (_, _) => RunForGroup(windows, windowActions.Minimize);

        Button closeButton = CreateActionButton(
            "×",
            LocalizationService.Get("Flyout_CloseAllTooltip"),
            true);
        StyleCompactActionButton(closeButton);
        SetCenteredActionGlyph(closeButton, CompactGlyph.Close);
        StyleCloseActionButton(closeButton);
        closeButton.Click += (_, _) => ConfirmAndCloseGroup(windows);

        toolTip.SetToolTip(collapseButton, collapseButton.AccessibleName);
        toolTip.SetToolTip(restoreButton, restoreButton.AccessibleName);
        toolTip.SetToolTip(minimizeButton, minimizeButton.AccessibleName);
        toolTip.SetToolTip(closeButton, closeButton.AccessibleName);

        header.Controls.Add(collapseButton, 0, 0);
        header.Controls.Add(nameLabel, 1, 0);
        header.Controls.Add(restoreButton, 2, 0);
        header.Controls.Add(minimizeButton, 3, 0);
        header.Controls.Add(closeButton, 4, 0);
        return header;
    }

    private Control CreateWindowRow(WindowInfo window, bool presentationMember)
    {
        int monitorRows = displays.Count > 1 ? (displays.Count + 3) / 4 : 1;
        int monitorButtonPitch = MonitorButtonSize + MonitorButtonGap;
        TableLayoutPanel row = CreateListGrid(
            Math.Max(32, monitorRows * monitorButtonPitch),
            new Padding(4, 1, 4, 1));
        row.BackColor = palette.Surface;
        row.ContextMenuStrip = groupContextMenu;

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

        Button presentationButton = CreateActionButton(
            presentationMember ? "−" : "+",
            LocalizationService.Get(presentationMember
                ? "Flyout_RemoveFromPresentation"
                : "Flyout_AddToPresentation"),
            isCloseButton: false);
        StyleCompactActionButton(presentationButton, fontSize: 12F);
        SetCenteredActionGlyph(
            presentationButton,
            presentationMember ? CompactGlyph.Minus : CompactGlyph.Plus);
        StyleBlueActionButton(presentationButton);
        presentationButton.Click += (_, _) =>
        {
            WindowIdentity identity = WindowIdentity.From(window);
            if (presentationMember)
            {
                presentationWindowIds.Remove(identity);
            }
            else
            {
                presentationWindowIds.Add(identity);
            }

            RenderWindowList();
            if (presentationLocked)
            {
                RefreshWindowList(force: true);
            }
        };
        toolTip.SetToolTip(presentationButton, presentationButton.AccessibleName);

        Button minimizeButton = CreateActionButton(
            "—",
            LocalizationService.Format("Flyout_MinimizeAccessible", window.DisplayTitle),
            isCloseButton: false);
        StyleCompactActionButton(minimizeButton);
        SetCenteredActionGlyph(minimizeButton, CompactGlyph.Minus);
        StyleBlueActionButton(minimizeButton);

        Button closeButton = CreateActionButton(
            "×",
            LocalizationService.Format("Flyout_CloseAccessible", window.DisplayTitle),
            isCloseButton: true);
        StyleCompactActionButton(closeButton);
        SetCenteredActionGlyph(closeButton, CompactGlyph.Close);
        StyleCloseActionButton(closeButton);

        minimizeButton.Click += (_, _) =>
        {
            if (!windowActions.Minimize(window))
            {
                ShowWindowActionFailure();
            }
        };

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
        if (presentationModeEnabled)
        {
            row.Controls.Add(presentationButton, 2, 0);
        }

        row.Controls.Add(minimizeButton, 3, 0);
        row.Controls.Add(closeButton, 4, 0);

        if (settings.ShowScreenNumber && displays.Count > 1)
        {
            row.Controls.Add(CreateMonitorButtons(window), 5, 0);
        }

        return row;
    }

    private Control CreateMonitorButtons(WindowInfo window)
    {
        FlowLayoutPanel panel = new()
        {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = new Padding(MonitorActionGap, 0, 0, 0),
            WrapContents = true
        };

        foreach (MonitorDisplay display in displays)
        {
            bool active = display.Number == window.MonitorNumber;
            Button button = new()
            {
                AccessibleName = LocalizationService.Format("Flyout_MoveToScreen", display.Number),
                BackColor = active ? ActiveButtonBackColor : palette.RaisedSurface,
                FlatStyle = FlatStyle.Flat,
                Font = new Font(SystemFonts.MessageBoxFont.FontFamily, 8.5F, FontStyle.Bold),
                ForeColor = active ? Color.Black : palette.Foreground,
                Margin = new Padding(0, 0, MonitorButtonGap, MonitorButtonGap),
                Size = new Size(MonitorButtonSize, MonitorButtonSize),
                TabStop = !active,
                Text = display.Number?.ToString(),
                UseVisualStyleBackColor = false
            };
            button.FlatAppearance.BorderSize = active ? 2 : 1;
            button.FlatAppearance.BorderColor = active ? ActiveButtonBackColor : palette.Border;
            button.FlatAppearance.MouseOverBackColor = ActiveButtonBackColor;
            button.FlatAppearance.MouseDownBackColor = palette.Pressed;
            button.MouseEnter += (_, _) => button.ForeColor = Color.Black;
            button.MouseLeave += (_, _) => button.ForeColor = active ? Color.Black : palette.Foreground;
            button.Click += (_, _) =>
            {
                if (active)
                {
                    return;
                }

                if (!windowActions.MoveToMonitor(window, display))
                {
                    ShowWindowActionFailure();
                    return;
                }

                RefreshWindowList(force: true);
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
            ColumnCount = 6,
            GrowStyle = TableLayoutPanelGrowStyle.FixedSize,
            Height = height,
            Margin = margin,
            RowCount = 1,
            Width = Math.Max(120, windowListPanel.ClientSize.Width
                - SystemInformation.VerticalScrollBarWidth - 10)
        };
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, settings.ShowApplicationIcons ? 24 : 0));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        grid.ColumnStyles.Add(new ColumnStyle(
            SizeType.Absolute,
            presentationModeEnabled ? PresentationActionColumnWidth : 0));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, ActionColumnWidth));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, ActionColumnWidth));
        int monitorWidth = settings.ShowScreenNumber && displays.Count > 1
            ? MonitorActionGap + (MonitorButtonSize + MonitorButtonGap) * Math.Min(4, displays.Count)
            : 0;
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, monitorWidth));
        return grid;
    }

    private Control CreatePresentationGroupHeader(IReadOnlyList<WindowInfo> windows)
    {
        int monitorWidth = displays.Count > 1
            ? MonitorActionGap + (MonitorButtonSize + MonitorButtonGap) * Math.Min(4, displays.Count)
            : 0;

        TableLayoutPanel header = new()
        {
            ColumnCount = 6,
            ContextMenuStrip = groupContextMenu,
            RowCount = 3,
            Height = 88,
            Margin = new Padding(4, 8, 4, 6),
            BackColor = palette.RaisedSurface,
            Width = Math.Max(120, windowListPanel.ClientSize.Width
                - SystemInformation.VerticalScrollBarWidth - 10)
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, settings.ShowApplicationIcons ? 24 : 0));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, ActionColumnWidth));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, ActionColumnWidth));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, monitorWidth));
        header.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        header.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        header.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));

        Label titleLabel = new()
        {
            AutoEllipsis = true,
            Dock = DockStyle.Fill,
            Font = new Font(SystemFonts.MessageBoxFont.FontFamily, 9F, FontStyle.Bold),
            ForeColor = palette.Foreground,
            Margin = new Padding(8, 0, 6, 0),
            Text = LocalizationService.Get("Flyout_PresentationGroupHeading"),
            TextAlign = ContentAlignment.MiddleLeft
        };
        header.Controls.Add(titleLabel, 0, 0);
        header.SetColumnSpan(titleLabel, 6);

        Label CreatePresentationLabel(string resourceKey)
        {
            return new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font(SystemFonts.MessageBoxFont.FontFamily, 7.5F, FontStyle.Bold),
                ForeColor = palette.SecondaryForeground,
                Margin = Padding.Empty,
                Text = LocalizationService.Get(resourceKey),
                TextAlign = ContentAlignment.MiddleCenter
            };
        }

        header.Controls.Add(CreatePresentationLabel("Flyout_PresentationProtection"), 2, 1);
        header.Controls.Add(CreatePresentationLabel("Flyout_MinimizeGroupShort"), 3, 1);
        header.Controls.Add(CreatePresentationLabel("Flyout_CloseGroupShort"), 4, 1);
        header.Controls.Add(CreatePresentationLabel("Flyout_PresentationScreen"), 5, 1);

        Button protectionButton = CreateActionButton(
            LocalizationService.Get(presentationLocked
                ? "Flyout_DisableProtectionShort"
                : "Flyout_EnableProtectionShort"),
            LocalizationService.Get(presentationLocked
                ? "Flyout_PresentationUnlock"
                : "Flyout_PresentationLock"),
            false);
        protectionButton.Dock = DockStyle.Fill;
        protectionButton.Font = new Font(SystemFonts.MessageBoxFont.FontFamily, 8F, FontStyle.Bold);
        protectionButton.Margin = new Padding(6, 2, 6, 2);
        protectionButton.ForeColor = presentationLocked
            ? ActiveButtonForeColor
            : Color.Goldenrod;
        protectionButton.BackColor = presentationLocked
            ? ActiveButtonBackColor
            : palette.Surface;
        protectionButton.FlatAppearance.BorderColor = presentationLocked
            ? ActiveButtonBackColor
            : Color.Goldenrod;
        protectionButton.FlatAppearance.MouseOverBackColor = presentationLocked
            ? ActiveButtonBackColor
            : palette.Hover;
        protectionButton.Click += (_, _) =>
        {
            if (presentationLocked)
            {
                presentationLocked = false;
                RenderWindowList();
            }
            else
            {
                TryActivatePresentationProtection();
            }
        };
        toolTip.SetToolTip(protectionButton, protectionButton.AccessibleName);
        header.Controls.Add(protectionButton, 2, 2);

        Button minimizeButton = CreateActionButton(
            "—",
            LocalizationService.Get("Flyout_MinimizeAllTooltip"),
            false);
        StyleCompactActionButton(minimizeButton);
        SetCenteredActionGlyph(minimizeButton, CompactGlyph.Minus);
        StyleBlueActionButton(minimizeButton);
        minimizeButton.Click += (_, _) =>
        {
            if (windows.Count > 0)
            {
                RunForGroup(windows, windowActions.Minimize);
            }
        };
        toolTip.SetToolTip(minimizeButton, minimizeButton.AccessibleName);
        header.Controls.Add(minimizeButton, 3, 2);

        Button closeButton = CreateActionButton(
            "×",
            LocalizationService.Get("Flyout_CloseAllTooltip"),
            true);
        StyleCompactActionButton(closeButton);
        SetCenteredActionGlyph(closeButton, CompactGlyph.Close);
        StyleCloseActionButton(closeButton);
        closeButton.Click += (_, _) =>
        {
            if (windows.Count > 0)
            {
                ConfirmAndCloseGroup(windows);
            }
        };
        toolTip.SetToolTip(closeButton, closeButton.AccessibleName);
        header.Controls.Add(closeButton, 4, 2);

        FlowLayoutPanel screenButtons = new()
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = Padding.Empty,
            Padding = new Padding(MonitorActionGap, 0, 0, 0),
            WrapContents = false
        };

        if (displays.Count > 1)
        {
            foreach (MonitorDisplay display in displays)
            {
                int number = display.Number!.Value;
                bool selected = number == presentationMonitorNumber;
                Button monitorButton = new()
                {
                    AccessibleName = LocalizationService.Format("Flyout_MoveToScreen", number),
                    BackColor = selected ? ActiveButtonBackColor : palette.Surface,
                    Enabled = !presentationLocked,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font(SystemFonts.MessageBoxFont.FontFamily, 8.5F, FontStyle.Bold),
                    ForeColor = selected ? Color.Black : palette.Foreground,
                    Margin = new Padding(0, 0, MonitorButtonGap, 0),
                    Size = new Size(MonitorButtonSize, MonitorButtonSize),
                    Text = number.ToString(),
                    UseVisualStyleBackColor = false
                };
                monitorButton.FlatAppearance.BorderColor = selected ? ActiveButtonBackColor : palette.Border;
                monitorButton.FlatAppearance.BorderSize = selected ? 2 : 1;
                monitorButton.FlatAppearance.MouseOverBackColor = ActiveButtonBackColor;
                monitorButton.Click += (_, _) =>
                {
                    presentationMonitorNumber = number;
                    presentationFallbackMonitorNumber = displays
                        .FirstOrDefault(candidate => candidate.Number != number)?.Number;
                    RenderWindowList();
                };
                toolTip.SetToolTip(monitorButton, monitorButton.AccessibleName);
                screenButtons.Controls.Add(monitorButton);
            }
        }

        header.Controls.Add(screenButtons, 5, 2);
        return header;
    }

    private void PresentationModeButton_Click(object? sender, EventArgs e)
    {
        displays = monitorDetector.GetDisplays();
        if (!presentationModeEnabled && displays.Count < 2)
        {
            MessageBox.Show(
                this,
                LocalizationService.Get("Flyout_PresentationRequiresTwoScreens"),
                LocalizationService.Get("App_Title"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        presentationModeEnabled = !presentationModeEnabled;
        ClearWindowRowCache();
        if (presentationModeEnabled)
        {
            string currentDeviceName = Screen.FromControl(this).DeviceName;
            presentationMonitorNumber = displays
                .FirstOrDefault(display =>
                    string.Equals(display.DeviceName, currentDeviceName, StringComparison.OrdinalIgnoreCase))
                ?.Number
                ?? displays.First().Number;
            presentationFallbackMonitorNumber = displays
                .FirstOrDefault(display => display.Number != presentationMonitorNumber)?.Number;
        }
        else
        {
            presentationLocked = false;
            presentationMonitorNumber = null;
            presentationFallbackMonitorNumber = null;
            presentationWindowIds.Clear();
        }

        UpdatePresentationModeVisual();
        RenderWindowList();
    }

    private void InitializeGroupContextMenu()
    {
        groupContextMenu = new ContextMenuStrip(components);
        collapseAllItem = new ToolStripMenuItem();
        expandAllItem = new ToolStripMenuItem();
        collapseAllItem.Click += (_, _) =>
        {
            collapsedApplicationIds.UnionWith(
                currentSnapshot.Select(window => window.ApplicationId));
            RenderWindowList();
        };
        expandAllItem.Click += (_, _) =>
        {
            collapsedApplicationIds.Clear();
            RenderWindowList();
        };
        groupContextMenu.Items.AddRange([collapseAllItem, expandAllItem]);
        groupContextMenu.Opening += (_, e) =>
        {
            e.Cancel = !settings.GroupByApplication;
            collapseAllItem.Enabled = currentSnapshot.Count > 0;
            expandAllItem.Enabled = collapsedApplicationIds.Count > 0;
        };
        windowListPanel.ContextMenuStrip = groupContextMenu;
    }

    private void UpdatePresentationModeVisual()
    {
        if (presentationModeButton is null)
        {
            return;
        }

        presentationModeButton.BackColor = presentationModeEnabled
            ? ActiveButtonBackColor
            : palette.RaisedSurface;
        presentationModeButton.ForeColor = presentationModeEnabled
            ? ActiveButtonForeColor
            : palette.Accent;
        presentationModeButton.FlatAppearance.BorderColor = presentationModeEnabled
            ? ActiveButtonBackColor
            : palette.Accent;
        presentationModeButton.FlatAppearance.BorderSize = 1;
        presentationModeButton.FlatAppearance.MouseOverBackColor = presentationModeEnabled
            ? ActiveButtonBackColor
            : palette.Hover;
        presentationModeButton.FlatAppearance.MouseDownBackColor = palette.Pressed;
    }

    private void TryActivatePresentationProtection()
    {
        if (!presentationModeEnabled || presentationMonitorNumber is not int reservedMonitor)
        {
            return;
        }

        displays = monitorDetector.GetDisplays();
        if (displays.Count < 2
            || displays.All(display => display.Number != reservedMonitor))
        {
            MessageBox.Show(
                this,
                LocalizationService.Get("Flyout_PresentationRequiresTwoScreens"),
                LocalizationService.Get("App_Title"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        WindowInfo[] foreignWindows = currentSnapshot
            .Where(window => !window.IsMinimized
                && window.MonitorNumber == reservedMonitor
                && !presentationWindowIds.Contains(WindowIdentity.From(window)))
            .ToArray();

        presentationFallbackMonitorNumber ??= displays
            .FirstOrDefault(display => display.Number != reservedMonitor)?.Number;

        if (foreignWindows.Length > 0)
        {
            using PresentationConflictForm dialog = new(
                foreignWindows,
                displays,
                reservedMonitor,
                settings.Theme);
            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            presentationWindowIds.UnionWith(dialog.AllowedWindows);
            presentationFallbackMonitorNumber = dialog.PreferredFallbackMonitorNumber
                ?? presentationFallbackMonitorNumber;

            bool success = true;
            foreach ((WindowIdentity identity, int targetNumber) in dialog.MoveTargets)
            {
                WindowInfo? window = currentSnapshot.FirstOrDefault(candidate =>
                    WindowIdentity.From(candidate) == identity);
                MonitorDisplay? target = displays.FirstOrDefault(display =>
                    display.Number == targetNumber);
                if (window is null || target is null || !windowActions.MoveToMonitor(window, target))
                {
                    success = false;
                }
            }

            if (!success)
            {
                ShowWindowActionFailure();
                RenderWindowList();
                return;
            }
        }

        presentationLocked = true;
        RefreshWindowList(force: true);
    }

    private IReadOnlyList<WindowInfo> EnforcePresentationReservation(
        IReadOnlyList<WindowInfo> snapshot)
    {
        if (!presentationLocked || presentationMonitorNumber is not int reservedMonitor)
        {
            return snapshot;
        }

        displays = monitorDetector.GetDisplays();
        MonitorDisplay? fallback = displays.FirstOrDefault(display =>
            display.Number == presentationFallbackMonitorNumber
            && display.Number != reservedMonitor)
            ?? displays.FirstOrDefault(display => display.Number != reservedMonitor);
        if (fallback is null
            || displays.All(display => display.Number != reservedMonitor))
        {
            DisablePresentationProtection(showMessage: true);
            return snapshot;
        }

        bool movedAny = false;
        foreach (WindowInfo window in snapshot.Where(window =>
                     !window.IsMinimized
                     && window.MonitorNumber == reservedMonitor
                     && !presentationWindowIds.Contains(WindowIdentity.From(window))))
        {
            if (!windowActions.MoveToMonitor(window, fallback))
            {
                DisablePresentationProtection(showMessage: true);
                return snapshot;
            }

            movedAny = true;
        }

        return movedAny ? windowEnumerator.Enumerate() : snapshot;
    }

    private void DisablePresentationProtection(bool showMessage)
    {
        presentationLocked = false;
        if (showMessage && Visible)
        {
            MessageBox.Show(
                this,
                LocalizationService.Get("Flyout_PresentationProtectionFailed"),
                LocalizationService.Get("App_Title"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
    }

    private void PrunePresentationWindows(IReadOnlyList<WindowInfo> snapshot)
    {
        HashSet<WindowIdentity> current = snapshot
            .Select(WindowIdentity.From)
            .ToHashSet();
        presentationWindowIds.RemoveWhere(identity => !current.Contains(identity));
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
        topBar.BackColor = palette.Background;
        presentationModeButton.BackColor = palette.RaisedSurface;
        helpButton.BackColor = palette.RaisedSurface;
        settingsButton.BackColor = palette.RaisedSurface;
        presentationModeButton.ForeColor = palette.SecondaryForeground;
        helpButton.ForeColor = palette.Foreground;
        settingsButton.ForeColor = palette.Foreground;
        presentationModeButton.FlatAppearance.BorderColor = palette.Border;
        presentationModeButton.FlatAppearance.BorderSize = 1;
        UpdatePresentationModeVisual();
        helpButton.FlatAppearance.BorderColor = palette.SecondaryForeground;
        settingsButton.FlatAppearance.BorderColor = palette.SecondaryForeground;
        helpButton.FlatAppearance.BorderSize = 1;
        settingsButton.FlatAppearance.BorderSize = 1;
        helpButton.FlatAppearance.MouseOverBackColor = palette.Hover;
        settingsButton.FlatAppearance.MouseOverBackColor = palette.Hover;
        helpButton.FlatAppearance.MouseDownBackColor = palette.Pressed;
        settingsButton.FlatAppearance.MouseDownBackColor = palette.Pressed;
        ThemeManager.ApplyTitleBar(this, palette.IsDark);
    }

    private void ApplyLocalization()
    {
        Text = LocalizationService.Get("App_Title");
        searchTextBox.PlaceholderText = LocalizationService.Get("Flyout_Search");
        presentationModeButton.Text = LocalizationService.Get("Flyout_PresentationMode");
        helpButton.Text = "?";
        settingsButton.Text = "\uE713";
        helpButton.AccessibleName = LocalizationService.Get("Tray_Help");
        settingsButton.AccessibleName = LocalizationService.Get("Tray_Settings");
        toolTip.SetToolTip(helpButton, helpButton.AccessibleName);
        toolTip.SetToolTip(settingsButton, settingsButton.AccessibleName);
        toolTip.SetToolTip(presentationModeButton, LocalizationService.Get("Flyout_PresentationMode"));
        collapseAllItem.Text = LocalizationService.Get("Flyout_CollapseAll");
        expandAllItem.Text = LocalizationService.Get("Flyout_ExpandAll");
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
            Margin = new Padding(2, 2, 2, 2),
            Padding = Padding.Empty,
            Text = text,
            UseVisualStyleBackColor = false
        };
        button.FlatAppearance.BorderColor = palette.Border;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.MouseDownBackColor = isCloseButton
            ? palette.ClosePressed
            : palette.Pressed;
        button.FlatAppearance.MouseOverBackColor = isCloseButton
            ? palette.CloseHover
            : palette.Hover;

        return button;
    }

    private enum CompactGlyph
    {
        Plus,
        Minus,
        Close
    }

    private static void SetCenteredActionGlyph(Button button, CompactGlyph glyph)
    {
        button.Text = string.Empty;
        button.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            float centerX = (button.ClientSize.Width - 1) / 2F;
            float centerY = (button.ClientSize.Height - 1) / 2F;
            float halfLength = glyph == CompactGlyph.Close ? 4.5F : 5F;

            using Pen pen = new(button.ForeColor, 1.6F)
            {
                StartCap = System.Drawing.Drawing2D.LineCap.Round,
                EndCap = System.Drawing.Drawing2D.LineCap.Round
            };

            switch (glyph)
            {
                case CompactGlyph.Plus:
                    e.Graphics.DrawLine(
                        pen,
                        centerX - halfLength,
                        centerY,
                        centerX + halfLength,
                        centerY);
                    e.Graphics.DrawLine(
                        pen,
                        centerX,
                        centerY - halfLength,
                        centerX,
                        centerY + halfLength);
                    break;

                case CompactGlyph.Minus:
                    e.Graphics.DrawLine(
                        pen,
                        centerX - halfLength,
                        centerY,
                        centerX + halfLength,
                        centerY);
                    break;

                case CompactGlyph.Close:
                    e.Graphics.DrawLine(
                        pen,
                        centerX - halfLength,
                        centerY - halfLength,
                        centerX + halfLength,
                        centerY + halfLength);
                    e.Graphics.DrawLine(
                        pen,
                        centerX + halfLength,
                        centerY - halfLength,
                        centerX - halfLength,
                        centerY + halfLength);
                    break;
            }
        };
    }

    private void StyleBlueActionButton(Button button)
    {
        button.BackColor = palette.Surface;
        button.ForeColor = ActiveButtonBackColor;
        button.FlatAppearance.BorderColor = ActiveButtonBackColor;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.MouseOverBackColor = ActiveButtonBackColor;
        button.FlatAppearance.MouseDownBackColor = ActiveButtonBackColor;
        button.MouseEnter += (_, _) =>
        {
            button.ForeColor = ActiveButtonForeColor;
            button.Invalidate();
        };
        button.MouseLeave += (_, _) =>
        {
            button.ForeColor = ActiveButtonBackColor;
            button.Invalidate();
        };
    }

    private void StyleCloseActionButton(Button button)
    {
        button.BackColor = palette.Surface;
        button.ForeColor = palette.CloseHover;
        button.FlatAppearance.BorderColor = palette.CloseHover;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.MouseOverBackColor = palette.CloseHover;
        button.FlatAppearance.MouseDownBackColor = palette.ClosePressed;
        button.MouseEnter += (_, _) =>
        {
            button.ForeColor = Color.Black;
            button.Invalidate();
        };
        button.MouseLeave += (_, _) =>
        {
            button.ForeColor = palette.CloseHover;
            button.Invalidate();
        };
    }

    private static void StyleCompactActionButton(Button button, float fontSize = 10F)
    {
        button.Anchor = AnchorStyles.None;
        button.Dock = DockStyle.None;
        button.Font = new Font("Segoe UI Symbol", fontSize, FontStyle.Regular);
        button.Margin = Padding.Empty;
        button.Padding = Padding.Empty;
        button.Size = new Size(CompactActionButtonSize, CompactActionButtonSize);
        button.TextAlign = ContentAlignment.MiddleCenter;
    }

    private void StyleGroupActionButton(Button button, bool isCloseButton)
    {
        if (isCloseButton)
        {
            StyleCloseActionButton(button);
        }
        else
        {
            StyleBlueActionButton(button);
        }
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
