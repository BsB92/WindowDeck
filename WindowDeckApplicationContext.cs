using WindowDeck.Services;

namespace WindowDeck;

internal sealed class WindowDeckApplicationContext : ApplicationContext
{
    private readonly Form1 flyout;
    private readonly GlobalHotkeyManager hotkeyManager;
    private readonly ContextMenuStrip trayMenu;
    private readonly NotifyIcon trayIcon;
    private bool isExiting;
    private bool trayResourcesDisposed;

    public WindowDeckApplicationContext()
    {
        flyout = new Form1();
        flyout.FormClosed += Flyout_FormClosed;

        trayMenu = new ContextMenuStrip();
        trayMenu.Items.Add("Open WindowDeck", null, OpenWindowDeck_Click);
        trayMenu.Items.Add(new ToolStripSeparator());
        trayMenu.Items.Add("Exit", null, Exit_Click);

        trayIcon = new NotifyIcon
        {
            ContextMenuStrip = trayMenu,
            Icon = SystemIcons.Application,
            Text = "WindowDeck",
            Visible = true
        };
        trayIcon.MouseClick += TrayIcon_MouseClick;

        hotkeyManager = new GlobalHotkeyManager();
        hotkeyManager.HotkeyPressed += HotkeyManager_HotkeyPressed;

        flyout.ShowFlyout();

        if (!hotkeyManager.IsRegistered)
        {
            MessageBox.Show(
                flyout,
                "WindowDeck could not register the Win + ` shortcut because it is unavailable.",
                "WindowDeck",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            hotkeyManager.Dispose();
            DisposeTrayResources();
            flyout.Dispose();
        }

        base.Dispose(disposing);
    }

    private void HotkeyManager_HotkeyPressed(object? sender, EventArgs e)
    {
        if (isExiting)
        {
            return;
        }

        if (flyout.Visible)
        {
            flyout.Hide();
        }
        else
        {
            flyout.ShowFlyout();
        }
    }

    private void TrayIcon_MouseClick(object? sender, MouseEventArgs e)
    {
        if (isExiting || e.Button != MouseButtons.Left)
        {
            return;
        }

        if (flyout.Visible)
        {
            flyout.Hide();
        }
        else
        {
            flyout.ShowFlyout();
        }
    }

    private void OpenWindowDeck_Click(object? sender, EventArgs e)
    {
        if (!isExiting)
        {
            flyout.ShowFlyout();
        }
    }

    private void Exit_Click(object? sender, EventArgs e)
    {
        ExitApplication();
    }

    private void Flyout_FormClosed(object? sender, FormClosedEventArgs e)
    {
        ExitApplication();
    }

    private void ExitApplication()
    {
        if (isExiting)
        {
            return;
        }

        isExiting = true;
        hotkeyManager.Dispose();

        if (!flyout.IsDisposed)
        {
            flyout.ExitApplication();
        }

        DisposeTrayResources();
        ExitThread();
    }

    private void DisposeTrayResources()
    {
        if (trayResourcesDisposed)
        {
            return;
        }

        trayResourcesDisposed = true;
        trayIcon.Visible = false;
        trayIcon.Dispose();
        trayMenu.Dispose();
    }
}
