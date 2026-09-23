using WindowDeck.Interop;

namespace WindowDeck.Models;

internal enum AppTheme
{
    System,
    Light,
    Dark
}

internal enum AppLanguage
{
    System,
    English,
    Polish
}

internal sealed class HotkeySettings
{
    public uint Modifiers { get; set; } = NativeMethods.ModWin;

    public uint VirtualKey { get; set; } = NativeMethods.VkOem3;

    public HotkeySettings Copy() => new() { Modifiers = Modifiers, VirtualKey = VirtualKey };
}

internal sealed class AppSettings
{
    public bool StartWithWindows { get; set; } = false;

    public bool StartMinimizedToTray { get; set; } = true;

    public HotkeySettings Hotkey { get; set; } = new();

    public bool GroupByApplication { get; set; } = true;

    public bool ShowApplicationIcons { get; set; } = true;

    public bool ShowScreenNumber { get; set; } = true;

    public bool ShowMinimizedWindows { get; set; } = true;

    public List<string> CollapsedApplicationIds { get; set; } = [];

    public AppTheme Theme { get; set; } = AppTheme.System;

    public AppLanguage Language { get; set; } = AppLanguage.System;

    public AppSettings Copy() => new()
    {
        StartWithWindows = StartWithWindows,
        StartMinimizedToTray = StartMinimizedToTray,
        Hotkey = Hotkey.Copy(),
        GroupByApplication = GroupByApplication,
        ShowApplicationIcons = ShowApplicationIcons,
        ShowScreenNumber = ShowScreenNumber,
        ShowMinimizedWindows = ShowMinimizedWindows,
        CollapsedApplicationIds = [.. CollapsedApplicationIds],
        Theme = Theme,
        Language = Language
    };
}
