using System.ComponentModel;
using System.Globalization;
using System.Resources;

namespace LynxTranscribe.Localization;

/// <summary>
/// Represents a language option with flag and display name
/// </summary>
public class LanguageOption
{
    public string Code { get; set; } = "";
    public string Flag { get; set; } = "";
    public string Name { get; set; } = "";
    public string DisplayName => $"{Name} ({Code.ToUpper()})";

    public override string ToString() => DisplayName;
}

/// <summary>
/// Manages application localization with support for dynamic language switching.
/// </summary>
public class LocalizationService : INotifyPropertyChanged
{
    private static LocalizationService? _instance;
    private static readonly object _lock = new();

    public static LocalizationService Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new LocalizationService();
                }
            }
            return _instance;
        }
    }

    private readonly ResourceManager _resourceManager;
    private CultureInfo _currentCulture;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? LanguageChanged;

    /// <summary>
    /// Supported languages with their display names and flags
    /// </summary>
    public static readonly List<LanguageOption> SupportedLanguages = new()
    {
        new LanguageOption { Code = "en", Flag = "🇬🇧", Name = "English" },
        new LanguageOption { Code = "fr", Flag = "🇫🇷", Name = "Français" }
    };

    /// <summary>
    /// Current language code (e.g., "en", "fr")
    /// </summary>
    public string CurrentLanguage => _currentCulture.TwoLetterISOLanguageName;

    /// <summary>
    /// Current culture info
    /// </summary>
    public CultureInfo CurrentCulture => _currentCulture;

    private LocalizationService()
    {
        _resourceManager = new ResourceManager(
            "LynxTranscribe.Resources.Strings.AppStrings",
            typeof(LocalizationService).Assembly);

        // Default to English
        _currentCulture = new CultureInfo("en");
    }

    /// <summary>
    /// Detects the system language and returns a supported language code
    /// </summary>
    public static string GetSystemLanguage()
    {
        var systemLang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLower();

        // Check if system language is supported
        if (SupportedLanguages.Any(l => l.Code == systemLang))
        {
            return systemLang;
        }

        // Default to English
        return "en";
    }

    /// <summary>
    /// Set the current language by language code
    /// </summary>
    public void SetLanguage(string languageCode)
    {
        if (!SupportedLanguages.Any(l => l.Code == languageCode))
        {
            languageCode = "en"; // Fallback to English
        }

        _currentCulture = new CultureInfo(languageCode);

        // Update thread culture for proper formatting
        CultureInfo.CurrentUICulture = _currentCulture;

        // Notify all subscribers
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Get a localized string by key
    /// </summary>
    public string Get(string key)
    {
        try
        {
            var value = _resourceManager.GetString(key, _currentCulture);
            return value ?? $"[{key}]"; // Return key in brackets if not found
        }
        catch
        {
            return $"[{key}]";
        }
    }

    /// <summary>
    /// Get a localized string with format parameters
    /// </summary>
    public string Get(string key, params object[] args)
    {
        var format = Get(key);
        try
        {
            return string.Format(format, args);
        }
        catch
        {
            return format;
        }
    }

    /// <summary>
    /// Indexer for easy access: LocalizationService.Instance["Key"]
    /// </summary>
    public string this[string key] => Get(key);

    /// <summary>
    /// Static shortcut for getting localized strings
    /// </summary>
    public static string Localize(string key) => Instance.Get(key);

    /// <summary>
    /// Static shortcut for getting localized strings with parameters
    /// </summary>
    public static string Localize(string key, params object[] args) => Instance.Get(key, args);
}
