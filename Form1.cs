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
            statusLabel.Text = "Automatic refresh unavailable; use Refresh";
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

    private void RefreshButton_Click(object sender, EventArgs e)
    {
        RefreshWindowList();
    }

    private void ActivateWindow(WindowInfo window)
    {
        WindowActivationResult result = windowActivator.Activate(window);
        switch (result)
        {
            case WindowActivationResult.Activated:
                statusLabel.Text = $"Activated {window.DisplayTitle}";
                break;
            case WindowActivationResult.WindowUnavailable:
                ShowActivationFailure(
                    "The selected window is no longer available. Refresh the list and try again.");
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
        statusLabel.Text = message;
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
            statusLabel.Text = "Window enumeration failed";
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
            statusLabel.Text = searchTextBox.TextLength == 0
                ? $"{visibleCount} windows found"
                : $"{visibleCount} matching windows";
        }
        finally
        {
            windowListPanel.ResumeLayout();
        }
    }

    private static Label CreateGroupHeader(string applicationName)
    {
        return new Label
        {
            AutoEllipsis = true,
            Font = new Font(SystemFonts.MessageBoxFont, FontStyle.Bold),
            Height = 28,
            Margin = new Padding(4, 8, 4, 0),
            Padding = new Padding(5, 0, 0, 0),
            Text = applicationName,
            TextAlign = ContentAlignment.MiddleLeft
        };
    }

    private Control CreateWindowRow(WindowInfo window)
    {
        TableLayoutPanel row = new()
        {
            ColumnCount = 4,
            Height = 36,
            Margin = new Padding(4, 0, 4, 2),
            RowCount = 1
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 36));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 36));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 48));

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

        Button minimizeButton = CreateActionButton("_", $"Minimize {window.DisplayTitle}");
        minimizeButton.Click += (_, _) =>
        {
            if (!windowActions.Minimize(window))
            {
                statusLabel.Text = "The window is no longer available";
            }
        };

        Button closeButton = CreateActionButton("×", $"Close {window.DisplayTitle}");
        closeButton.Click += (_, _) =>
        {
            if (!windowActions.RequestClose(window))
            {
                statusLabel.Text = "The window is no longer available";
            }
        };

        Label monitorLabel = new()
        {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Text = window.MonitorNumber is int monitorNumber ? $"[{monitorNumber}]" : "[?]",
            TextAlign = ContentAlignment.MiddleCenter
        };

        row.Controls.Add(titleButton, 0, 0);
        row.Controls.Add(minimizeButton, 1, 0);
        row.Controls.Add(closeButton, 2, 0);
        row.Controls.Add(monitorLabel, 3, 0);
        return row;
    }

    private static Button CreateActionButton(string text, string accessibleName)
    {
        return new Button
        {
            AccessibleName = accessibleName,
            Dock = DockStyle.Fill,
            FlatStyle = FlatStyle.System,
            Margin = new Padding(1),
            Padding = Padding.Empty,
            Text = text,
            UseVisualStyleBackColor = true
        };
    }

    private void PositionOnRelevantMonitor(int requestedWidth)
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
            return;
        }

        NativeMethods.MonitorInfoEx monitorInfo = new()
        {
            Size = (uint)Marshal.SizeOf<NativeMethods.MonitorInfoEx>()
        };

        if (!NativeMethods.GetMonitorInfo(monitorHandle, ref monitorInfo))
        {
            return;
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
