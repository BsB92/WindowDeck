using System.Globalization;
using System.Resources;
using WindowDeck.Models;

namespace WindowDeck.Localization;

internal sealed record LanguageDefinition(AppLanguage Value, string? CultureName, string DisplayNameResourceKey);

internal static class LocalizationService
{
    private static readonly CultureInfo EnglishCulture = CultureInfo.GetCultureInfo("en");
    private static readonly ResourceManager ResourceManager =
        new("WindowDeck.Resources", typeof(LocalizationService).Assembly);

    internal static IReadOnlyList<LanguageDefinition> SupportedLanguages { get; } =
    [
        new(AppLanguage.System, null, "Language_System"),
        new(AppLanguage.English, "en", "Language_English"),
        new(AppLanguage.Polish, "pl", "Language_Polish")
    ];

    internal static AppLanguage SelectedLanguage { get; private set; } = AppLanguage.System;

    internal static CultureInfo EffectiveCulture { get; private set; } = EnglishCulture;

    internal static void Apply(AppLanguage language)
    {
        SelectedLanguage = SupportedLanguages.Any(item => item.Value == language)
            ? language
            : AppLanguage.System;
        EffectiveCulture = ResolveEffectiveCulture(SelectedLanguage, CultureInfo.CurrentUICulture);
    }

    internal static string Get(string key) =>
        ResourceManager.GetString(key, EffectiveCulture)
        ?? ResourceManager.GetString(key, EnglishCulture)
        ?? key;

    internal static string Format(string key, params object?[] arguments) =>
        string.Format(EffectiveCulture, Get(key), arguments);

    private static CultureInfo ResolveEffectiveCulture(AppLanguage language, CultureInfo systemCulture)
    {
        if (language != AppLanguage.System)
        {
            string? cultureName = SupportedLanguages
                .First(definition => definition.Value == language).CultureName;
            return cultureName is null ? EnglishCulture : CultureInfo.GetCultureInfo(cultureName);
        }

        return systemCulture.Name.StartsWith("pl", StringComparison.OrdinalIgnoreCase)
            ? CultureInfo.GetCultureInfo("pl")
            : EnglishCulture;
    }
}
