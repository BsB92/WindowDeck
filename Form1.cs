using System.ComponentModel;
using System.Runtime.InteropServices;
using WindowDeck.Interop;
using WindowDeck.Models;
using WindowDeck.Services;

namespace WindowDeck;

public partial class Form1 : Form
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
    private const int ScreenColumnWidth = 56;

    private readonly WindowEnumerator windowEnumerator = new();
    private readonly WindowActivator windowActivator = new();
    private readonly WindowActions windowActions = new();
    private IReadOnlyList<WindowInfo> currentSnapshot = [];
    private WindowEventMonitor? windowEventMonitor;
    private bool isClosing;
    private bool monitoringStarted;
    private bool allowApplicationExit;
    private int anchoredOuterRight;
    private int anchoredOuterTop;
    private int anchoredOuterHeight;
    private int maximumPanelWidth;

    public Form1()
    {
        InitializeComponent();
        PositionOnRelevantMonitor(DefaultPanelWidth);
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        RefreshWindowList();

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
                "WindowDeck",
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
                    "The selected window is no longer available. Try again.");
                break;
            case WindowActivationResult.RestorationFailed:
                ShowActivationFailure("The selected window could not be restored.");
                break;
            case WindowActivationResult.ForegroundActivationFailed:
                ShowActivationFailure(
                    "Windows did not allow or complete activation of the selected window.");
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
            "WindowDeck",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private void RefreshWindowList()
    {
        try
        {
            currentSnapshot = windowEnumerator.Enumerate();
            RenderWindowList();
        }
        catch (Win32Exception exception)
        {
            MessageBox.Show(
                this,
                exception.Message,
                "WindowDeck",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void RenderWindowList()
    {
        IReadOnlyList<IGrouping<string, WindowInfo>> groups =
            WindowListPresentation.Create(currentSnapshot, searchTextBox.Text);
        int visibleCount = groups.Sum(group => group.Count());

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
                        ? "No windows found"
                        : "No matching windows",
                    TextAlign = ContentAlignment.MiddleCenter
                };
                windowListPanel.Controls.Add(emptyLabel);
            }
            else
            {
                foreach (IGrouping<string, WindowInfo> group in groups)
                {
                    windowListPanel.Controls.Add(CreateGroupHeader(group.Key));
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

    private static Control CreateColumnHeader()
    {
        TableLayoutPanel header = CreateListGrid(24, new Padding(4, 1, 4, 0));
        Label screenHeader = new()
        {
            Dock = DockStyle.Fill,
            Font = new Font(SystemFonts.MessageBoxFont.FontFamily, 8.5F, FontStyle.Regular),
            ForeColor = SystemColors.GrayText,
            Margin = Padding.Empty,
            Text = "Screen",
            TextAlign = ContentAlignment.MiddleCenter
        };

        header.Controls.Add(screenHeader, 3, 0);
        return header;
    }

    private static Label CreateGroupHeader(string applicationName)
    {
        return new Label
        {
            AutoEllipsis = true,
            Font = new Font(SystemFonts.MessageBoxFont, FontStyle.Bold),
            Height = 25,
            Margin = new Padding(4, 6, 4, 0),
            Padding = new Padding(5, 0, 0, 0),
            Text = applicationName,
            TextAlign = ContentAlignment.MiddleLeft
        };
    }

    private Control CreateWindowRow(WindowInfo window)
    {
        TableLayoutPanel row = CreateListGrid(32, new Padding(4, 0, 4, 1));

        Button titleButton = new()
        {
            AutoEllipsis = true,
            Dock = DockStyle.Fill,
            FlatStyle = FlatStyle.Flat,
            Margin = Padding.Empty,
            Padding = new Padding(6, 0, 3, 0),
            Text = window.DisplayTitle,
            TextAlign = ContentAlignment.MiddleLeft,
            UseVisualStyleBackColor = true
        };
        titleButton.FlatAppearance.BorderSize = 0;
        titleButton.Click += (_, _) => ActivateWindow(window);

        Button minimizeButton = CreateActionButton(
            "—",
            $"Minimize {window.DisplayTitle}",
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
            $"Close {window.DisplayTitle}",
            isCloseButton: true);
        closeButton.Click += (_, _) =>
        {
            if (!windowActions.RequestClose(window))
            {
                ShowWindowActionFailure();
            }
        };

        Label monitorLabel = new()
        {
            Dock = DockStyle.Fill,
            Font = new Font(SystemFonts.MessageBoxFont.FontFamily, 10F, FontStyle.Regular),
            ForeColor = SystemColors.GrayText,
            Margin = Padding.Empty,
            Text = window.MonitorNumber is int monitorNumber ? $"[ {monitorNumber} ]" : "[ ? ]",
            TextAlign = ContentAlignment.MiddleCenter
        };

        row.Controls.Add(titleButton, 0, 0);
        row.Controls.Add(minimizeButton, 1, 0);
        row.Controls.Add(closeButton, 2, 0);
        row.Controls.Add(monitorLabel, 3, 0);
        return row;
    }

    private static TableLayoutPanel CreateListGrid(int height, Padding margin)
    {
        TableLayoutPanel grid = new()
        {
            ColumnCount = 4,
            Height = height,
            Margin = margin,
            RowCount = 1
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, ActionColumnWidth));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, ActionColumnWidth));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, ScreenColumnWidth));
        return grid;
    }

    private static Button CreateActionButton(
        string text,
        string accessibleName,
        bool isCloseButton)
    {
        Button button = new()
        {
            AccessibleName = accessibleName,
            BackColor = SystemColors.Control,
            Dock = DockStyle.Fill,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Symbol", 10F, FontStyle.Regular),
            ForeColor = SystemColors.ControlText,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            Text = text,
            UseVisualStyleBackColor = false
        };
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseDownBackColor = isCloseButton
            ? Color.FromArgb(196, 43, 28)
            : SystemColors.ControlDark;
        button.FlatAppearance.MouseOverBackColor = isCloseButton
            ? Color.FromArgb(232, 17, 35)
            : SystemColors.ControlLight;

        if (isCloseButton)
        {
            button.MouseEnter += (_, _) => button.ForeColor = Color.White;
            button.MouseLeave += (_, _) => button.ForeColor = SystemColors.ControlText;
        }

        return button;
    }

    private void ShowWindowActionFailure()
    {
        MessageBox.Show(
            this,
            "The selected window is no longer available.",
            "WindowDeck",
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
