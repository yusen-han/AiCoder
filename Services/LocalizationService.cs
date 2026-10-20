using System.Globalization;

namespace AiCoder.Services;

public static class LocalizationService
{
    private const string LanguageKey = "app_language";

    public static IReadOnlyList<CultureInfo> SupportedCultures { get; } = new[]
    {
        new CultureInfo("en-US"),
        new CultureInfo("zh-Hans")
    };

    public static CultureInfo CurrentCulture => Thread.CurrentThread.CurrentUICulture;

    public static void Initialize()
    {
        var saved = Preferences.Get(LanguageKey, string.Empty);
        var cultureName = string.IsNullOrEmpty(saved)
            ? GetBestSupportedCultureName(CultureInfo.CurrentUICulture.Name)
            : saved;

        SetCulture(cultureName, persist: false);
    }

    public static void SetCulture(string cultureName, bool persist = true)
    {
        var supported = SupportedCultures.FirstOrDefault(c =>
            c.Name.Equals(cultureName, StringComparison.OrdinalIgnoreCase) ||
            c.TwoLetterISOLanguageName.Equals(cultureName, StringComparison.OrdinalIgnoreCase));

        if (supported is null)
            supported = SupportedCultures[0];

        Thread.CurrentThread.CurrentCulture = supported;
        Thread.CurrentThread.CurrentUICulture = supported;
        CultureInfo.DefaultThreadCurrentCulture = supported;
        CultureInfo.DefaultThreadCurrentUICulture = supported;

        if (persist)
            Preferences.Set(LanguageKey, supported.Name);
    }

    private static string GetBestSupportedCultureName(string systemCultureName)
    {
        if (SupportedCultures.Any(c => c.Name.Equals(systemCultureName, StringComparison.OrdinalIgnoreCase)))
            return systemCultureName;

        var twoLetter = systemCultureName.Split('-')[0];
        var match = SupportedCultures.FirstOrDefault(c =>
            c.TwoLetterISOLanguageName.Equals(twoLetter, StringComparison.OrdinalIgnoreCase));

        return match?.Name ?? SupportedCultures[0].Name;
    }
}
