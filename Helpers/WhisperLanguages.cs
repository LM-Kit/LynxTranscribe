namespace LynxTranscribe.Helpers;

/// <summary>
/// Whisper 3 supported languages with ISO 639-1 codes and flag emojis
/// </summary>
public static class WhisperLanguages
{
    /// <summary>
    /// Language info: code -> (display name, flag emoji, country code for display)
    /// </summary>
    public static readonly Dictionary<string, (string Name, string Flag, string CountryCode)> Languages = new()
    {
        { "auto", ("Auto-detect", "🌐", "AUTO") },
        { "af", ("Afrikaans", "🇿🇦", "ZA") },
        { "sq", ("Albanian", "🇦🇱", "AL") },
        { "am", ("Amharic", "🇪🇹", "ET") },
        { "ar", ("Arabic", "🇸🇦", "AR") },
        { "hy", ("Armenian", "🇦🇲", "AM") },
        { "as", ("Assamese", "🇮🇳", "IN") },
        { "az", ("Azerbaijani", "🇦🇿", "AZ") },
        { "ba", ("Bashkir", "🇷🇺", "RU") },
        { "eu", ("Basque", "🇪🇸", "ES") },
        { "be", ("Belarusian", "🇧🇾", "BY") },
        { "bn", ("Bengali", "🇧🇩", "BD") },
        { "bs", ("Bosnian", "🇧🇦", "BA") },
        { "br", ("Breton", "🇫🇷", "FR") },
        { "bg", ("Bulgarian", "🇧🇬", "BG") },
        { "my", ("Burmese", "🇲🇲", "MM") },
        { "ca", ("Catalan", "🇪🇸", "ES") },
        { "zh", ("Chinese", "🇨🇳", "CN") },
        { "hr", ("Croatian", "🇭🇷", "HR") },
        { "cs", ("Czech", "🇨🇿", "CZ") },
        { "da", ("Danish", "🇩🇰", "DK") },
        { "nl", ("Dutch", "🇳🇱", "NL") },
        { "en", ("English", "🇬🇧", "GB") },
        { "et", ("Estonian", "🇪🇪", "EE") },
        { "fo", ("Faroese", "🇫🇴", "FO") },
        { "fi", ("Finnish", "🇫🇮", "FI") },
        { "fr", ("French", "🇫🇷", "FR") },
        { "gl", ("Galician", "🇪🇸", "ES") },
        { "ka", ("Georgian", "🇬🇪", "GE") },
        { "de", ("German", "🇩🇪", "DE") },
        { "el", ("Greek", "🇬🇷", "GR") },
        { "gu", ("Gujarati", "🇮🇳", "IN") },
        { "ht", ("Haitian Creole", "🇭🇹", "HT") },
        { "ha", ("Hausa", "🇳🇬", "NG") },
        { "haw", ("Hawaiian", "🇺🇸", "US") },
        { "he", ("Hebrew", "🇮🇱", "IL") },
        { "hi", ("Hindi", "🇮🇳", "IN") },
        { "hu", ("Hungarian", "🇭🇺", "HU") },
        { "is", ("Icelandic", "🇮🇸", "IS") },
        { "id", ("Indonesian", "🇮🇩", "ID") },
        { "it", ("Italian", "🇮🇹", "IT") },
        { "ja", ("Japanese", "🇯🇵", "JP") },
        { "jw", ("Javanese", "🇮🇩", "ID") },
        { "kn", ("Kannada", "🇮🇳", "IN") },
        { "kk", ("Kazakh", "🇰🇿", "KZ") },
        { "km", ("Khmer", "🇰🇭", "KH") },
        { "ko", ("Korean", "🇰🇷", "KR") },
        { "lo", ("Lao", "🇱🇦", "LA") },
        { "la", ("Latin", "🇻🇦", "VA") },
        { "lv", ("Latvian", "🇱🇻", "LV") },
        { "ln", ("Lingala", "🇨🇩", "CD") },
        { "lt", ("Lithuanian", "🇱🇹", "LT") },
        { "lb", ("Luxembourgish", "🇱🇺", "LU") },
        { "mk", ("Macedonian", "🇲🇰", "MK") },
        { "mg", ("Malagasy", "🇲🇬", "MG") },
        { "ms", ("Malay", "🇲🇾", "MY") },
        { "ml", ("Malayalam", "🇮🇳", "IN") },
        { "mt", ("Maltese", "🇲🇹", "MT") },
        { "mi", ("Maori", "🇳🇿", "NZ") },
        { "mr", ("Marathi", "🇮🇳", "IN") },
        { "mn", ("Mongolian", "🇲🇳", "MN") },
        { "ne", ("Nepali", "🇳🇵", "NP") },
        { "no", ("Norwegian", "🇳🇴", "NO") },
        { "nn", ("Nynorsk", "🇳🇴", "NO") },
        { "oc", ("Occitan", "🇫🇷", "FR") },
        { "ps", ("Pashto", "🇦🇫", "AF") },
        { "fa", ("Persian", "🇮🇷", "IR") },
        { "pl", ("Polish", "🇵🇱", "PL") },
        { "pt", ("Portuguese", "🇵🇹", "PT") },
        { "pa", ("Punjabi", "🇮🇳", "IN") },
        { "ro", ("Romanian", "🇷🇴", "RO") },
        { "ru", ("Russian", "🇷🇺", "RU") },
        { "sa", ("Sanskrit", "🇮🇳", "IN") },
        { "sr", ("Serbian", "🇷🇸", "RS") },
        { "sn", ("Shona", "🇿🇼", "ZW") },
        { "sd", ("Sindhi", "🇵🇰", "PK") },
        { "si", ("Sinhala", "🇱🇰", "LK") },
        { "sk", ("Slovak", "🇸🇰", "SK") },
        { "sl", ("Slovenian", "🇸🇮", "SI") },
        { "so", ("Somali", "🇸🇴", "SO") },
        { "es", ("Spanish", "🇪🇸", "ES") },
        { "su", ("Sundanese", "🇮🇩", "ID") },
        { "sw", ("Swahili", "🇰🇪", "KE") },
        { "sv", ("Swedish", "🇸🇪", "SE") },
        { "tl", ("Tagalog", "🇵🇭", "PH") },
        { "tg", ("Tajik", "🇹🇯", "TJ") },
        { "ta", ("Tamil", "🇮🇳", "IN") },
        { "tt", ("Tatar", "🇷🇺", "RU") },
        { "te", ("Telugu", "🇮🇳", "IN") },
        { "th", ("Thai", "🇹🇭", "TH") },
        { "bo", ("Tibetan", "🇨🇳", "CN") },
        { "tk", ("Turkmen", "🇹🇲", "TM") },
        { "tr", ("Turkish", "🇹🇷", "TR") },
        { "uk", ("Ukrainian", "🇺🇦", "UA") },
        { "ur", ("Urdu", "🇵🇰", "PK") },
        { "uz", ("Uzbek", "🇺🇿", "UZ") },
        { "vi", ("Vietnamese", "🇻🇳", "VN") },
        { "cy", ("Welsh", "🏴󠁧󠁢󠁷󠁬󠁳󠁿", "GB") },
        { "yi", ("Yiddish", "🇮🇱", "IL") },
        { "yo", ("Yoruba", "🇳🇬", "NG") },
    };

    /// <summary>
    /// Get display names for picker (sorted with Auto-detect first)
    /// Format: "Auto-detect", "English", "French", etc.
    /// </summary>
    public static List<string> GetDisplayNames()
    {
        var names = new List<string> { Languages["auto"].Name };
        names.AddRange(Languages
            .Where(kvp => kvp.Key != "auto")
            .OrderBy(kvp => kvp.Value.Name)
            .Select(kvp => kvp.Value.Name));
        return names;
    }

    /// <summary>
    /// Get language code from display name
    /// </summary>
    public static string GetCodeFromDisplayName(string displayName)
    {
        return Languages.FirstOrDefault(kvp => kvp.Value.Name == displayName).Key ?? "auto";
    }

    /// <summary>
    /// Get display name from language code (without flag/code)
    /// </summary>
    public static string GetDisplayNameFromCode(string? code)
    {
        if (string.IsNullOrEmpty(code))
        {
            code = "auto";
        }

        return Languages.TryGetValue(code, out var info) ? info.Name : Languages["auto"].Name;
    }

    /// <summary>
    /// Get flag emoji from language code
    /// </summary>
    public static string GetFlagFromCode(string? code)
    {
        if (string.IsNullOrEmpty(code))
        {
            code = "auto";
        }

        return Languages.TryGetValue(code, out var info) ? info.Flag : Languages["auto"].Flag;
    }

    /// <summary>
    /// Get country code from language code
    /// </summary>
    public static string GetCountryCodeFromCode(string? code)
    {
        if (string.IsNullOrEmpty(code))
        {
            code = "auto";
        }

        return Languages.TryGetValue(code, out var info) ? info.CountryCode : Languages["auto"].CountryCode;
    }

    /// <summary>
    /// Get full display string from language code (just the name for picker)
    /// </summary>
    public static string GetFullDisplayFromCode(string code)
    {
        if (Languages.TryGetValue(code, out var info))
        {
            return info.Name;
        }
        return Languages["auto"].Name;
    }
}
