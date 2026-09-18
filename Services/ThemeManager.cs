using Microsoft.Win32;
using WindowDeck.Interop;
using WindowDeck.Models;

namespace WindowDeck.Services;

internal readonly record struct ThemePalette(
    bool IsDark,
    Color Background,
    Color Surface,
    Color RaisedSurface,
    Color Hover,
    Color Pressed,
    Color Border,
    Color Foreground,
    Color SecondaryForeground,
    Color Accent,
    Color CloseHover,
    Color ClosePressed)
{
    public static ThemePalette Light { get; } = new(
        false, Color.FromArgb(243, 243, 243), Color.White,
        Color.FromArgb(249, 249, 249), Color.FromArgb(234, 234, 234),
        Color.FromArgb(225, 225, 225), Color.FromArgb(218, 218, 218),
        Color.FromArgb(32, 32, 32), Color.FromArgb(96, 96, 96),
        Color.FromArgb(0, 95, 184), Color.FromArgb(232, 17, 35),
        Color.FromArgb(196, 43, 28));

    public static ThemePalette Dark { get; } = new(
        true, Color.FromArgb(32, 32, 32), Color.FromArgb(40, 40, 40),
        Color.FromArgb(45, 45, 45), Color.FromArgb(55, 55, 55),
        Color.FromArgb(63, 63, 63), Color.FromArgb(62, 62, 62),
        Color.FromArgb(245, 245, 245), Color.FromArgb(173, 173, 173),
        Color.FromArgb(96, 205, 255), Color.FromArgb(196, 43, 28),
        Color.FromArgb(145, 28, 18));
}

internal static class ThemeManager
{
    private const string PersonalizeKey =
        @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string AppsUseLightTheme = "AppsUseLightTheme";

    public static ThemePalette Resolve(AppTheme preference) => preference switch
    {
        AppTheme.Dark => ThemePalette.Dark,
        AppTheme.Light => ThemePalette.Light,
        _ => SystemUsesDarkTheme() ? ThemePalette.Dark : ThemePalette.Light
    };

    public static bool SystemUsesDarkTheme()
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(PersonalizeKey);
            return key?.GetValue(AppsUseLightTheme) is int value && value == 0;
        }
        catch (Exception exception) when (exception is UnauthorizedAccessException
            or IOException or System.Security.SecurityException)
        {
            return false;
        }
    }

    public static void ApplyTitleBar(Form form, bool dark)
    {
        if (!form.IsHandleCreated)
        {
            return;
        }

        int enabled = dark ? 1 : 0;
        _ = NativeMethods.DwmSetWindowAttribute(
            form.Handle,
            NativeMethods.DwmaUseImmersiveDarkMode,
            ref enabled,
            sizeof(int));
    }

    public static void StyleButton(Button button, ThemePalette palette)
    {
        button.BackColor = palette.RaisedSurface;
        button.ForeColor = palette.Foreground;
        button.FlatStyle = FlatStyle.Flat;
        button.UseVisualStyleBackColor = false;
        button.FlatAppearance.BorderColor = palette.Border;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.MouseOverBackColor = palette.Hover;
        button.FlatAppearance.MouseDownBackColor = palette.Pressed;
    }
}
