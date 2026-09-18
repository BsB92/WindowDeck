using WindowDeck.Interop;
using WindowDeck.Localization;
using WindowDeck.Models;
using WindowDeck.Services;

namespace WindowDeck;

internal sealed class SettingsForm : Form
{
    private sealed record SelectionItem<T>(T Value, string Text)
    {
        public override string ToString() => Text;
    }

    private readonly CheckBox startWithWindows = new() { AutoSize = true };
    private readonly CheckBox startMinimized = new() { AutoSize = true };
    private readonly CheckBox winModifier = new() { Text = "Win", AutoSize = true };
    private readonly CheckBox controlModifier = new() { Text = "Ctrl", AutoSize = true };
    private readonly CheckBox altModifier = new() { Text = "Alt", AutoSize = true };
    private readonly CheckBox shiftModifier = new() { Text = "Shift", AutoSize = true };
    private readonly TextBox hotkeyKey = new() { ReadOnly = true, Width = 110, TextAlign = HorizontalAlignment.Center };
    private readonly CheckBox groupByApplication = new() { AutoSize = true };
    private readonly CheckBox showApplicationIcons = new() { AutoSize = true };
    private readonly CheckBox showScreenNumber = new() { AutoSize = true };
    private readonly CheckBox showMinimizedWindows = new() { AutoSize = true };
    private readonly ComboBox theme = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
    private readonly ComboBox language = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
    private readonly Func<AppSettings, (bool Success, string? ErrorMessage, bool EffectiveStartupState)> applySettings;
    private readonly List<(Label Label, string ResourceKey)> sectionLabels = [];
    private readonly Label hotkeyHelp;
    private readonly TableLayoutPanel content;
    private readonly FlowLayoutPanel shortcut;
    private readonly FlowLayoutPanel buttons;
    private readonly Button cancel;
    private readonly Button ok;
    private uint virtualKey;
    private AppTheme selectedTheme;
    private ThemePalette palette;

    public SettingsForm(
        AppSettings currentSettings,
        bool effectiveStartupState,
        Func<AppSettings, (bool Success, string? ErrorMessage, bool EffectiveStartupState)> applySettings)
    {
        this.applySettings = applySettings;
        selectedTheme = currentSettings.Theme;
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(520, 650);
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = new Font("Segoe UI", 9F);
        Icon = WindowDeckIcon.Load();

        content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            ColumnCount = 1,
            Padding = new Padding(20, 12, 20, 16)
        };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        Controls.Add(content);

        content.Controls.Add(CreateSectionLabel("Settings_General"));
        content.Controls.Add(startWithWindows);
        content.Controls.Add(startMinimized);
        content.Controls.Add(CreateSectionLabel("Settings_Hotkey"));
        shortcut = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
        shortcut.Controls.AddRange([winModifier, controlModifier, altModifier, shiftModifier, hotkeyKey]);
        content.Controls.Add(shortcut);
        hotkeyHelp = new Label { AutoSize = true, MaximumSize = new Size(450, 0) };
        content.Controls.Add(hotkeyHelp);
        content.Controls.Add(CreateSectionLabel("Settings_WindowList"));
        content.Controls.Add(groupByApplication);
        content.Controls.Add(showApplicationIcons);
        content.Controls.Add(showScreenNumber);
        content.Controls.Add(showMinimizedWindows);
        content.Controls.Add(CreateSectionLabel("Settings_Appearance"));
        content.Controls.Add(theme);
        content.Controls.Add(CreateSectionLabel("Settings_Language"));
        content.Controls.Add(language);

        buttons = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 20, 0, 0)
        };
        cancel = new Button { DialogResult = DialogResult.Cancel, AutoSize = true, Padding = new Padding(10, 2, 10, 2) };
        ok = new Button { AutoSize = true, Padding = new Padding(10, 2, 10, 2) };
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
        PopulateSelections(currentSettings.Theme, currentSettings.Language);
        theme.SelectedIndexChanged += Theme_SelectedIndexChanged;
        ApplyLocalization();
        ApplyTheme();
    }

    public void RefreshTheme()
    {
        ThemePalette resolvedPalette = ThemeManager.Resolve(selectedTheme);
        if (resolvedPalette != palette)
        {
            ApplyTheme();
        }
    }

    public void Relocalize()
    {
        AppTheme themeValue = ((SelectionItem<AppTheme>)theme.SelectedItem!).Value;
        AppLanguage languageValue = ((SelectionItem<AppLanguage>)language.SelectedItem!).Value;
        PopulateSelections(themeValue, languageValue);
        ApplyLocalization();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) Icon?.Dispose();
        base.Dispose(disposing);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ThemeManager.ApplyTitleBar(this, ThemeManager.Resolve(selectedTheme).IsDark);
    }

    private Label CreateSectionLabel(string resourceKey)
    {
        Label label = new()
        {
            AutoSize = true,
            Font = new Font(Font, FontStyle.Bold),
            Margin = new Padding(0, 16, 0, 6)
        };
        sectionLabels.Add((label, resourceKey));
        return label;
    }

    private void PopulateSelections(AppTheme selectedThemeValue, AppLanguage selectedLanguageValue)
    {
        theme.Items.Clear();
        theme.Items.AddRange(
        [
            new SelectionItem<AppTheme>(AppTheme.System, LocalizationService.Get("Theme_System")),
            new SelectionItem<AppTheme>(AppTheme.Light, LocalizationService.Get("Theme_Light")),
            new SelectionItem<AppTheme>(AppTheme.Dark, LocalizationService.Get("Theme_Dark"))
        ]);
        theme.SelectedItem = theme.Items.Cast<SelectionItem<AppTheme>>()
            .First(item => item.Value == selectedThemeValue);

        language.Items.Clear();
        language.Items.AddRange(LocalizationService.SupportedLanguages
            .Select(definition => new SelectionItem<AppLanguage>(
                definition.Value,
                LocalizationService.Get(definition.DisplayNameResourceKey)))
            .ToArray());
        language.SelectedItem = language.Items.Cast<SelectionItem<AppLanguage>>()
            .First(item => item.Value == selectedLanguageValue);
    }

    private void ApplyLocalization()
    {
        Text = LocalizationService.Get("Settings_Title");
        startWithWindows.Text = LocalizationService.Get("Settings_StartWithWindows");
        startMinimized.Text = LocalizationService.Get("Settings_StartMinimized");
        hotkeyHelp.Text = LocalizationService.Get("Settings_HotkeyHelp");
        groupByApplication.Text = LocalizationService.Get("Settings_GroupByApplication");
        showApplicationIcons.Text = LocalizationService.Get("Settings_ShowApplicationIcons");
        showScreenNumber.Text = LocalizationService.Get("Settings_ShowScreenNumber");
        showMinimizedWindows.Text = LocalizationService.Get("Settings_ShowMinimizedWindows");
        cancel.Text = LocalizationService.Get("Common_Cancel");
        ok.Text = LocalizationService.Get("Common_OK");
        foreach ((Label label, string resourceKey) in sectionLabels)
        {
            label.Text = LocalizationService.Get(resourceKey);
        }
    }

    private void Theme_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (theme.SelectedItem is SelectionItem<AppTheme> item)
        {
            selectedTheme = item.Value;
            ApplyTheme();
        }
    }

    private void ApplyTheme()
    {
        palette = ThemeManager.Resolve(selectedTheme);
        BackColor = palette.Background;
        ForeColor = palette.Foreground;
        content.BackColor = palette.Background;
        content.ForeColor = palette.Foreground;
        shortcut.BackColor = palette.Background;
        buttons.BackColor = palette.Background;
        hotkeyHelp.ForeColor = palette.SecondaryForeground;
        foreach ((Label label, _) in sectionLabels) label.ForeColor = palette.Foreground;
        foreach (CheckBox checkBox in content.Controls.OfType<CheckBox>()
                     .Concat(shortcut.Controls.OfType<CheckBox>()))
        {
            checkBox.BackColor = palette.Background;
            checkBox.ForeColor = palette.Foreground;
        }

        hotkeyKey.BackColor = palette.Surface;
        hotkeyKey.ForeColor = palette.Foreground;
        theme.BackColor = palette.Surface;
        theme.ForeColor = palette.Foreground;
        language.BackColor = palette.Surface;
        language.ForeColor = palette.Foreground;
        ThemeManager.StyleButton(ok, palette);
        ThemeManager.StyleButton(cancel, palette);
        ThemeManager.ApplyTitleBar(this, palette.IsDark);
    }

    private void HotkeyKey_KeyDown(object? sender, KeyEventArgs e)
    {
        e.Handled = true;
        e.SuppressKeyPress = true;
        Keys key = e.KeyCode;
        if (key is Keys.ControlKey or Keys.ShiftKey or Keys.Menu or Keys.LWin or Keys.RWin) return;
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
            MessageBox.Show(this, LocalizationService.Get("Settings_ChooseShortcut"), Text,
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
            Theme = ((SelectionItem<AppTheme>)theme.SelectedItem!).Value,
            Language = ((SelectionItem<AppLanguage>)language.SelectedItem!).Value
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
