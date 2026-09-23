using System.Text.Json;
using System.Text.Json.Serialization;
using WindowDeck.Models;
using WindowDeck.Localization;

namespace WindowDeck.Services;

internal sealed class SettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public SettingsService()
    {
        string directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WindowDeck");
        SettingsPath = Path.Combine(directory, "settings.json");
    }

    public string SettingsPath { get; }

    public AppSettings Load(out bool invalidFile)
    {
        invalidFile = false;
        if (!File.Exists(SettingsPath))
        {
            return new AppSettings();
        }

        try
        {
            AppSettings settings = JsonSerializer.Deserialize<AppSettings>(
                File.ReadAllText(SettingsPath), JsonOptions) ?? new AppSettings();
            settings.Hotkey ??= new HotkeySettings();
            settings.CollapsedApplicationIds ??= [];
            if (!Enum.IsDefined(settings.Theme)
                || !Enum.IsDefined(settings.Language)
                || settings.Hotkey.VirtualKey == 0
                || (settings.Hotkey.Modifiers & ~Interop.NativeMethods.SupportedHotkeyModifiers) != 0)
            {
                throw new JsonException("The settings file contains unsupported values.");
            }

            return settings;
        }
        catch (JsonException)
        {
            invalidFile = true;
            return new AppSettings();
        }
        catch (IOException)
        {
            invalidFile = true;
            return new AppSettings();
        }
        catch (UnauthorizedAccessException)
        {
            invalidFile = true;
            return new AppSettings();
        }
    }

    public bool TrySave(AppSettings settings, out string? errorMessage)
    {
        string? directory = Path.GetDirectoryName(SettingsPath);
        string temporaryPath = SettingsPath + ".tmp";
        try
        {
            Directory.CreateDirectory(directory!);
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(settings, JsonOptions));
            File.Move(temporaryPath, SettingsPath, true);
            errorMessage = null;
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            try { File.Delete(temporaryPath); } catch (IOException) { }
            errorMessage = LocalizationService.Format("Message_SaveSettingsFailed", exception.Message);
            return false;
        }
    }
}
