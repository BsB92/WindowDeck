using System.ComponentModel;
using System.Runtime.InteropServices;
using WindowDeck.Interop;
using WindowDeck.Models;
using WindowDeck.Services;

namespace WindowDeck;

public partial class Form1 : Form
{
    private const int DefaultPanelWidth = 680;

    private readonly WindowEnumerator windowEnumerator = new();
    private readonly WindowActivator windowActivator = new();
    private WindowEventMonitor? windowEventMonitor;
    private bool isClosing;
    private bool monitoringStarted;
    private bool allowApplicationExit;
    private int anchoredRight;

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

        if (keyData == Keys.Enter && windowListView.Focused)
        {
            ActivateSelectedWindow();
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);

        if (anchoredRight != 0 && WindowState == FormWindowState.Normal)
        {
            Left = anchoredRight - Width;
        }

        SizeWindowListColumns();
    }

    private void WindowListView_MouseClick(object sender, MouseEventArgs e)
    {
        ListViewItem? item = windowListView.GetItemAt(e.X, e.Y);
        if (e.Button == MouseButtons.Left && item is not null)
        {
            item.Selected = true;
            ActivateSelectedWindow();
        }
    }

    private void RefreshButton_Click(object sender, EventArgs e)
    {
        RefreshWindowList();
    }

    private void ActivateSelectedWindow()
    {
        if (windowListView.SelectedItems.Count != 1
            || windowListView.SelectedItems[0].Tag is not WindowInfo window)
        {
            statusLabel.Text = "Select one window to activate";
            return;
        }

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
        windowListView.BeginUpdate();

        try
        {
            windowListView.Items.Clear();

            foreach (WindowInfo window in windowEnumerator.Enumerate())
            {
                ListViewItem item = new(window.DisplayTitle)
                {
                    Tag = window
                };
                item.SubItems.Add(window.MonitorNumber is int monitorNumber
                    ? $"Monitor {monitorNumber}"
                    : "Monitor ?");
                windowListView.Items.Add(item);
            }

            statusLabel.Text = $"{windowListView.Items.Count} windows found";
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
        finally
        {
            windowListView.EndUpdate();
        }
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
        anchoredRight = workArea.Right;
        Bounds = new Rectangle(
            workArea.Right - width,
            workArea.Top,
            width,
            workArea.Bottom - workArea.Top);
    }

    private void SizeWindowListColumns()
    {
        const int monitorColumnWidth = 110;
        monitorColumn.Width = monitorColumnWidth;
        displayTitleColumn.Width = Math.Max(120, windowListView.ClientSize.Width - monitorColumnWidth - 4);
    }
}
