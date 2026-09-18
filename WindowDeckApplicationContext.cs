using WindowDeck.Models;
using WindowDeck.Services;
using Microsoft.Win32;

namespace WindowDeck;

internal sealed class WindowDeckApplicationContext : ApplicationContext
{
    private readonly SettingsService settingsService = new();
    private readonly StartupManager startupManager = new();
    private readonly Form1 flyout;
    private readonly GlobalHotkeyManager hotkeyManager;
    private readonly ContextMenuStrip trayMenu;
    private readonly ToolStripMenuItem startWithWindowsItem;
    private readonly NotifyIcon trayIcon;
    private readonly Icon applicationIcon;
    private AppSettings settings;
    private SettingsForm? settingsForm;
    private HelpForm? helpForm;
    private AboutForm? aboutForm;
    private bool isExiting;
    private bool trayResourcesDisposed;

    public WindowDeckApplicationContext()
    {
        settings = settingsService.Load(out bool invalidSettingsFile);
        applicationIcon = WindowDeckIcon.Load();
        bool effectiveStartupState = startupManager.IsEnabled();
        string? startupSynchronizationError = SynchronizeStartupState(effectiveStartupState);
        flyout = new Form1(settings);
        flyout.FormClosed += Flyout_FormClosed;

        trayMenu = new ContextMenuStrip();
        trayMenu.Items.Add("Open WindowDeck", null, OpenWindowDeck_Click);
        trayMenu.Items.Add("Settings", null, Settings_Click);
        trayMenu.Items.Add("Help", null, Help_Click);
        trayMenu.Items.Add("About WindowDeck", null, About_Click);
        startWithWindowsItem = new ToolStripMenuItem("Start with Windows")
        {
            Checked = effectiveStartupState,
            CheckOnClick = false
        };
        startWithWindowsItem.Click += StartWithWindows_Click;
        trayMenu.Items.Add(startWithWindowsItem);
        trayMenu.Items.Add(new ToolStripSeparator());
        trayMenu.Items.Add("Exit", null, Exit_Click);
        trayMenu.Opening += TrayMenu_Opening;

        trayIcon = new NotifyIcon
        {
            ContextMenuStrip = trayMenu,
            Icon = applicationIcon,
            Text = "WindowDeck",
            Visible = true
        };
        trayIcon.MouseClick += TrayIcon_MouseClick;

        hotkeyManager = new GlobalHotkeyManager(
            settings.Hotkey.Modifiers,
            settings.Hotkey.VirtualKey);
        hotkeyManager.HotkeyPressed += HotkeyManager_HotkeyPressed;
        SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;

        if (settings.StartMinimizedToTray)
        {
            flyout.InitializeWhileHidden();
        }
        else
        {
            flyout.ShowFlyout();
        }

        if (invalidSettingsFile)
        {
            MessageBox.Show(
                "WindowDeck could not read settings.json and is using safe defaults. " +
                "Saving Settings will replace the invalid file.",
                "WindowDeck", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        if (startupSynchronizationError is not null)
        {
            MessageBox.Show(startupSynchronizationError, "WindowDeck",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        if (!hotkeyManager.IsRegistered)
        {
            MessageBox.Show(
                flyout,
                $"WindowDeck could not register {SettingsForm.FormatShortcut(settings.Hotkey)} " +
                "because it is unavailable. The tray menu remains available.",
                "WindowDeck",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            settingsForm?.Dispose();
            helpForm?.Dispose();
            aboutForm?.Dispose();
            SystemEvents.UserPreferenceChanged -= SystemEvents_UserPreferenceChanged;
            hotkeyManager.Dispose();
            DisposeTrayResources();
            flyout.Dispose();
            applicationIcon.Dispose();
        }

        base.Dispose(disposing);
    }

    private void SystemEvents_UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (isExiting
            || settings.Theme != AppTheme.System
            || flyout.IsDisposed
            || e.Category is not (UserPreferenceCategory.Color
                or UserPreferenceCategory.VisualStyle
                or UserPreferenceCategory.General))
        {
            return;
        }

        void RefreshTheme()
        {
            if (isExiting)
            {
                return;
            }

            flyout.RefreshTheme();
            settingsForm?.RefreshTheme();
            helpForm?.RefreshTheme();
            aboutForm?.RefreshTheme();
        }

        try
        {
            if (flyout.InvokeRequired)
            {
                flyout.BeginInvoke((Action)RefreshTheme);
            }
            else
            {
                RefreshTheme();
            }
        }
        catch (InvalidOperationException) when (isExiting || flyout.IsDisposed)
        {
            // The flyout began shutting down between the state check and marshaling.
        }
    }

    private void HotkeyManager_HotkeyPressed(object? sender, EventArgs e)
    {
        if (isExiting) return;
        if (flyout.Visible) flyout.Hide();
        else flyout.ShowFlyoutOnCursorMonitor();
    }

    private void TrayIcon_MouseClick(object? sender, MouseEventArgs e)
    {
        if (isExiting || e.Button != MouseButtons.Left) return;
        if (flyout.Visible) flyout.Hide();
        else flyout.ShowFlyout();
    }

    private void OpenWindowDeck_Click(object? sender, EventArgs e)
    {
        if (!isExiting) flyout.ShowFlyout();
    }

    private void TrayMenu_Opening(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        bool effectiveStartupState = startupManager.IsEnabled();
        startWithWindowsItem.Checked = effectiveStartupState;
        SynchronizeStartupState(effectiveStartupState);
    }

    private void Settings_Click(object? sender, EventArgs e)
    {
        if (isExiting) return;
        if (settingsForm is { IsDisposed: false })
        {
            settingsForm.Show();
            settingsForm.Activate();
            return;
        }

        flyout.Hide();
        bool effectiveStartupState = startupManager.IsEnabled();
        string? synchronizationError = SynchronizeStartupState(effectiveStartupState);
        startWithWindowsItem.Checked = effectiveStartupState;
        if (synchronizationError is not null)
        {
            MessageBox.Show(synchronizationError, "WindowDeck",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        settingsForm = new SettingsForm(settings, effectiveStartupState, ApplySettings);
        settingsForm.FormClosed += (_, _) => settingsForm = null;
        settingsForm.Show();
        settingsForm.Activate();
    }

    private void Help_Click(object? sender, EventArgs e)
    {
        if (isExiting) return;
        if (helpForm is { IsDisposed: false })
        {
            helpForm.Show();
            helpForm.Activate();
            return;
        }

        helpForm = new HelpForm(settings.Theme);
        helpForm.FormClosed += (_, _) => helpForm = null;
        helpForm.Show();
        helpForm.Activate();
    }

    private void About_Click(object? sender, EventArgs e)
    {
        if (isExiting) return;
        if (aboutForm is { IsDisposed: false })
        {
            aboutForm.Show();
            aboutForm.Activate();
            return;
        }

        aboutForm = new AboutForm(settings.Theme);
        aboutForm.FormClosed += (_, _) => aboutForm = null;
        aboutForm.Show();
        aboutForm.Activate();
    }

    private void StartWithWindows_Click(object? sender, EventArgs e)
    {
        AppSettings candidate = settings.Copy();
        candidate.StartWithWindows = !startupManager.IsEnabled();
        (bool success, string? errorMessage, _) = ApplySettings(candidate);
        if (!success)
        {
            MessageBox.Show(errorMessage, "WindowDeck", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private (bool Success, string? ErrorMessage, bool EffectiveStartupState) ApplySettings(AppSettings candidate)
    {
        AppSettings previous = settings.Copy();
        bool previousStartupState = startupManager.IsEnabled();

        if (!hotkeyManager.TryChange(candidate.Hotkey.Modifiers, candidate.Hotkey.VirtualKey))
        {
            return (false, "That shortcut is unavailable. The previous shortcut remains active.",
                startupManager.IsEnabled());
        }

        if (!startupManager.TrySetEnabled(candidate.StartWithWindows, out string? startupError))
        {
            hotkeyManager.TryChange(previous.Hotkey.Modifiers, previous.Hotkey.VirtualKey);
            bool effectiveStartupState = startupManager.IsEnabled();
            startWithWindowsItem.Checked = effectiveStartupState;
            return (false, startupError, effectiveStartupState);
        }

        if (!settingsService.TrySave(candidate, out string? saveError))
        {
            startupManager.TrySetEnabled(previousStartupState, out _);
            bool hotkeyRestored = hotkeyManager.TryChange(
                previous.Hotkey.Modifiers, previous.Hotkey.VirtualKey);
            string rollbackMessage = hotkeyRestored
                ? string.Empty
                : " The previous hotkey could not be restored; use the tray menu to choose another shortcut.";
            bool effectiveStartupState = startupManager.IsEnabled();
            startWithWindowsItem.Checked = effectiveStartupState;
            return (false, saveError + rollbackMessage, effectiveStartupState);
        }

        settings = candidate.Copy();
        bool currentStartupState = startupManager.IsEnabled();
        settings.StartWithWindows = currentStartupState;
        startWithWindowsItem.Checked = currentStartupState;
        flyout.ApplySettings(settings);
        helpForm?.SetTheme(settings.Theme);
        aboutForm?.SetTheme(settings.Theme);
        return (true, null, currentStartupState);
    }

    private string? SynchronizeStartupState(bool effectiveStartupState)
    {
        if (settings.StartWithWindows == effectiveStartupState)
        {
            return null;
        }

        AppSettings synchronizedSettings = settings.Copy();
        synchronizedSettings.StartWithWindows = effectiveStartupState;
        if (!settingsService.TrySave(synchronizedSettings, out string? saveError))
        {
            return saveError;
        }

        settings = synchronizedSettings;
        return null;
    }

    private void Exit_Click(object? sender, EventArgs e) => ExitApplication();

    private void Flyout_FormClosed(object? sender, FormClosedEventArgs e) => ExitApplication();

    private void ExitApplication()
    {
        if (isExiting) return;
        isExiting = true;
        SystemEvents.UserPreferenceChanged -= SystemEvents_UserPreferenceChanged;
        settingsForm?.Close();
        helpForm?.Close();
        aboutForm?.Close();
        hotkeyManager.Dispose();
        if (!flyout.IsDisposed) flyout.ExitApplication();
        DisposeTrayResources();
        ExitThread();
    }

    private void DisposeTrayResources()
    {
        if (trayResourcesDisposed) return;
        trayResourcesDisposed = true;
        trayIcon.Visible = false;
        trayIcon.Dispose();
        trayMenu.Dispose();
    }
}
