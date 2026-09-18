using WindowDeck.Models;
using WindowDeck.Services;
using WindowDeck.Localization;
using Microsoft.Win32;

namespace WindowDeck;

internal sealed class WindowDeckApplicationContext : ApplicationContext
{
    private readonly SettingsService settingsService = new();
    private readonly StartupManager startupManager = new();
    private readonly Form1 flyout;
    private readonly GlobalHotkeyManager hotkeyManager;
    private readonly ContextMenuStrip trayMenu;
    private readonly ToolStripMenuItem openWindowDeckItem;
    private readonly ToolStripMenuItem settingsItem;
    private readonly ToolStripMenuItem helpItem;
    private readonly ToolStripMenuItem aboutItem;
    private readonly ToolStripMenuItem startWithWindowsItem;
    private readonly ToolStripMenuItem exitItem;
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
        LocalizationService.Apply(settings.Language);
        applicationIcon = WindowDeckIcon.Load();
        bool effectiveStartupState = startupManager.IsEnabled();
        string? startupSynchronizationError = SynchronizeStartupState(effectiveStartupState);
        flyout = new Form1(settings);
        flyout.FormClosed += Flyout_FormClosed;

        trayMenu = new ContextMenuStrip();
        openWindowDeckItem = new ToolStripMenuItem(null, null, OpenWindowDeck_Click);
        settingsItem = new ToolStripMenuItem(null, null, Settings_Click);
        helpItem = new ToolStripMenuItem(null, null, Help_Click);
        aboutItem = new ToolStripMenuItem(null, null, About_Click);
        trayMenu.Items.AddRange([openWindowDeckItem, settingsItem, helpItem, aboutItem]);
        startWithWindowsItem = new ToolStripMenuItem
        {
            Checked = effectiveStartupState,
            CheckOnClick = false
        };
        startWithWindowsItem.Click += StartWithWindows_Click;
        trayMenu.Items.Add(startWithWindowsItem);
        trayMenu.Items.Add(new ToolStripSeparator());
        exitItem = new ToolStripMenuItem(null, null, Exit_Click);
        trayMenu.Items.Add(exitItem);
        trayMenu.Opening += TrayMenu_Opening;
        ApplyLocalization();

        trayIcon = new NotifyIcon
        {
            ContextMenuStrip = trayMenu,
            Icon = applicationIcon,
            Text = LocalizationService.Get("App_Title"),
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
                LocalizationService.Get("Message_InvalidSettings"),
                LocalizationService.Get("App_Title"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        if (startupSynchronizationError is not null)
        {
            MessageBox.Show(startupSynchronizationError, LocalizationService.Get("App_Title"),
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        if (!hotkeyManager.IsRegistered)
        {
            MessageBox.Show(
                flyout,
                LocalizationService.Format("Message_HotkeyUnavailable",
                    SettingsForm.FormatShortcut(settings.Hotkey)),
                LocalizationService.Get("App_Title"),
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
            MessageBox.Show(synchronizationError, LocalizationService.Get("App_Title"),
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
            MessageBox.Show(errorMessage, LocalizationService.Get("App_Title"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private (bool Success, string? ErrorMessage, bool EffectiveStartupState) ApplySettings(AppSettings candidate)
    {
        AppSettings previous = settings.Copy();
        bool previousStartupState = startupManager.IsEnabled();

        if (!hotkeyManager.TryChange(candidate.Hotkey.Modifiers, candidate.Hotkey.VirtualKey))
        {
            return (false, LocalizationService.Get("Message_ShortcutUnavailable"),
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
                : LocalizationService.Get("Message_HotkeyRollbackFailed");
            bool effectiveStartupState = startupManager.IsEnabled();
            startWithWindowsItem.Checked = effectiveStartupState;
            return (false, saveError + rollbackMessage, effectiveStartupState);
        }

        settings = candidate.Copy();
        LocalizationService.Apply(settings.Language);
        bool currentStartupState = startupManager.IsEnabled();
        settings.StartWithWindows = currentStartupState;
        startWithWindowsItem.Checked = currentStartupState;
        flyout.ApplySettings(settings);
        settingsForm?.Relocalize();
        helpForm?.SetTheme(settings.Theme);
        helpForm?.ApplyLocalization();
        aboutForm?.SetTheme(settings.Theme);
        aboutForm?.ApplyLocalization();
        ApplyLocalization();
        return (true, null, currentStartupState);
    }

    private void ApplyLocalization()
    {
        openWindowDeckItem.Text = LocalizationService.Get("Tray_Open");
        settingsItem.Text = LocalizationService.Get("Tray_Settings");
        helpItem.Text = LocalizationService.Get("Tray_Help");
        aboutItem.Text = LocalizationService.Get("Tray_About");
        startWithWindowsItem.Text = LocalizationService.Get("Tray_StartWithWindows");
        exitItem.Text = LocalizationService.Get("Tray_Exit");
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
