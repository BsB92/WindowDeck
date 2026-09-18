using WindowDeck.Interop;
using WindowDeck.Models;

namespace WindowDeck;

internal sealed class SettingsForm : Form
{
    private readonly CheckBox startWithWindows = new() { Text = "Start WindowDeck with Windows", AutoSize = true };
    private readonly CheckBox startMinimized = new() { Text = "Start minimized to tray", AutoSize = true };
    private readonly CheckBox winModifier = new() { Text = "Win", AutoSize = true };
    private readonly CheckBox controlModifier = new() { Text = "Ctrl", AutoSize = true };
    private readonly CheckBox altModifier = new() { Text = "Alt", AutoSize = true };
    private readonly CheckBox shiftModifier = new() { Text = "Shift", AutoSize = true };
    private readonly TextBox hotkeyKey = new() { ReadOnly = true, Width = 110, TextAlign = HorizontalAlignment.Center };
    private readonly CheckBox groupByApplication = new() { Text = "Group windows by application", AutoSize = true };
    private readonly CheckBox showApplicationIcons = new() { Text = "Show application icons", AutoSize = true };
    private readonly CheckBox showScreenNumber = new() { Text = "Show screen number", AutoSize = true };
    private readonly CheckBox showMinimizedWindows = new() { Text = "Show minimized windows", AutoSize = true };
    private readonly ComboBox theme = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 160 };
    private readonly Func<AppSettings, (bool Success, string? ErrorMessage, bool EffectiveStartupState)> applySettings;
    private uint virtualKey;

    public SettingsForm(
        AppSettings currentSettings,
        bool effectiveStartupState,
        Func<AppSettings, (bool Success, string? ErrorMessage, bool EffectiveStartupState)> applySettings)
    {
        this.applySettings = applySettings;
        Text = "WindowDeck Settings";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(440, 520);

        TableLayoutPanel content = new()
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            ColumnCount = 1,
            Padding = new Padding(16)
        };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        Controls.Add(content);

        content.Controls.Add(CreateSectionLabel("General"));
        content.Controls.Add(startWithWindows);
        content.Controls.Add(startMinimized);
        content.Controls.Add(CreateSectionLabel("Hotkey"));
        FlowLayoutPanel shortcut = new() { AutoSize = true, WrapContents = false };
        shortcut.Controls.AddRange([winModifier, controlModifier, altModifier, shiftModifier, hotkeyKey]);
        content.Controls.Add(shortcut);
        content.Controls.Add(new Label
        {
            Text = "Select modifiers, then focus the key box and press one key.",
            AutoSize = true,
            ForeColor = SystemColors.GrayText
        });
        content.Controls.Add(CreateSectionLabel("Window list"));
        content.Controls.Add(groupByApplication);
        content.Controls.Add(showApplicationIcons);
        content.Controls.Add(showScreenNumber);
        content.Controls.Add(showMinimizedWindows);
        content.Controls.Add(CreateSectionLabel("Appearance"));
        theme.Items.AddRange(Enum.GetNames<AppTheme>());
        content.Controls.Add(theme);

        FlowLayoutPanel buttons = new()
        {
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 20, 0, 0)
        };
        Button cancel = new() { Text = "Cancel", DialogResult = DialogResult.Cancel, AutoSize = true };
        Button ok = new() { Text = "OK", AutoSize = true };
        ok.Click += Ok_Click;
        buttons.Controls.AddRange([cancel, ok]);
        content.Controls.Add(buttons);
        AcceptButton = ok;
        CancelButton = cancel;

        startWithWindows.Checked = effectiveStartupState;
        startMinimized.Checked = currentSettings.StartMinimizedToTray;
        winModifier.Checked = (currentSettings.Hotkey.Modifiers & NativeMethods.ModWin) != 0;
        controlModifier.Checked = (currentSettings.Hotkey.Modifiers & NativeMethods.ModControl) != 0;
        altModifier.Checked = (currentSettings.Hotkey.Modifiers & NativeMethods.ModAlt) != 0;
        shiftModifier.Checked = (currentSettings.Hotkey.Modifiers & NativeMethods.ModShift) != 0;
        virtualKey = currentSettings.Hotkey.VirtualKey;
        hotkeyKey.Text = FormatKey(virtualKey);
        hotkeyKey.KeyDown += HotkeyKey_KeyDown;
        groupByApplication.Checked = currentSettings.GroupByApplication;
        showApplicationIcons.Checked = currentSettings.ShowApplicationIcons;
        showScreenNumber.Checked = currentSettings.ShowScreenNumber;
        showMinimizedWindows.Checked = currentSettings.ShowMinimizedWindows;
        theme.SelectedItem = currentSettings.Theme.ToString();
    }

    private static Label CreateSectionLabel(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Font = new Font(SystemFonts.MessageBoxFont, FontStyle.Bold),
        Margin = new Padding(0, 14, 0, 6)
    };

    private void HotkeyKey_KeyDown(object? sender, KeyEventArgs e)
    {
        e.Handled = true;
        e.SuppressKeyPress = true;
        Keys key = e.KeyCode;
        if (key is Keys.ControlKey or Keys.ShiftKey or Keys.Menu or Keys.LWin or Keys.RWin)
        {
            return;
        }

        virtualKey = (uint)key;
        hotkeyKey.Text = FormatKey(virtualKey);
    }

    private void Ok_Click(object? sender, EventArgs e)
    {
        uint modifiers = 0;
        if (winModifier.Checked) modifiers |= NativeMethods.ModWin;
        if (controlModifier.Checked) modifiers |= NativeMethods.ModControl;
        if (altModifier.Checked) modifiers |= NativeMethods.ModAlt;
        if (shiftModifier.Checked) modifiers |= NativeMethods.ModShift;

        if (modifiers == 0 || virtualKey == 0)
        {
            MessageBox.Show(this, "Choose at least one modifier and one key.", Text,
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        AppSettings candidate = new()
        {
            StartWithWindows = startWithWindows.Checked,
            StartMinimizedToTray = startMinimized.Checked,
            Hotkey = new HotkeySettings { Modifiers = modifiers, VirtualKey = virtualKey },
            GroupByApplication = groupByApplication.Checked,
            ShowApplicationIcons = showApplicationIcons.Checked,
            ShowScreenNumber = showScreenNumber.Checked,
            ShowMinimizedWindows = showMinimizedWindows.Checked,
            Theme = Enum.Parse<AppTheme>((string)theme.SelectedItem!)
        };

        (bool success, string? errorMessage, bool effectiveStartupState) = applySettings(candidate);
        if (!success)
        {
            startWithWindows.Checked = effectiveStartupState;
            MessageBox.Show(this, errorMessage, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        DialogResult = DialogResult.OK;
        Close();
    }

    internal static string FormatShortcut(HotkeySettings hotkey)
    {
        List<string> parts = [];
        if ((hotkey.Modifiers & NativeMethods.ModWin) != 0) parts.Add("Win");
        if ((hotkey.Modifiers & NativeMethods.ModControl) != 0) parts.Add("Ctrl");
        if ((hotkey.Modifiers & NativeMethods.ModAlt) != 0) parts.Add("Alt");
        if ((hotkey.Modifiers & NativeMethods.ModShift) != 0) parts.Add("Shift");
        parts.Add(FormatKey(hotkey.VirtualKey));
        return string.Join(" + ", parts);
    }

    private static string FormatKey(uint key) => key == NativeMethods.VkOem3
        ? "`"
        : ((Keys)key).ToString();
}
