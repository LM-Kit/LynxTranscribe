using System.Text.Json;

namespace LynxTranscribe.Services;

/// <summary>
/// Service for persisting application settings across sessions.
/// </summary>
public class AppSettingsService
{
    /// <summary>
    /// Default values for all settings - single source of truth.
    /// </summary>
    public static class Defaults
    {
        public const double LeftPanelWidth = 340;
        public const double TranscriptFontSize = 14;
        public const double MinFontSize = 10;
        public const double MaxFontSize = 24;
        public const bool DarkMode = true;
        public const bool UseAccurateMode = true;
        public const bool EnableVoiceActivityDetection = true;
        public const bool EnableDictationFormatting = true;
        public const bool OpenFilesAfterExport = true;
        public const bool AutoTranscribeOnImport = false;
        public const double Volume = 0.8;
        public const double PlaybackSpeed = 1.0;
        public const string TranscriptionLanguage = "auto";
        public const int ResourceUsageLevel = 3; // 1=Light(25%), 2=Balanced(50%), 3=Performance(75%), 4=Maximum(100%)
    }

    private readonly string _settingsFilePath;
    private readonly string _defaultAppDataPath;
    private AppSettings _settings = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public AppSettingsService()
    {
        _defaultAppDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LynxTranscribe");

        Directory.CreateDirectory(_defaultAppDataPath);
        _settingsFilePath = Path.Combine(_defaultAppDataPath, "settings.json");

        Load();

        // Ensure directories exist
        EnsureDirectoriesExist();
    }

    /// <summary>
    /// Whether to use Accurate mode (true) or Turbo mode (false).
    /// </summary>
    public bool UseAccurateMode
    {
        get => _settings.UseAccurateMode;
        set
        {
            if (_settings.UseAccurateMode != value)
            {
                _settings.UseAccurateMode = value;
                Save();
            }
        }
    }

    /// <summary>
    /// The language code for transcription (e.g., "auto", "en", "fr", "de").
    /// </summary>
    public string TranscriptionLanguage
    {
        get => _settings.TranscriptionLanguage;
        set
        {
            if (_settings.TranscriptionLanguage != value)
            {
                _settings.TranscriptionLanguage = value;
                Save();
            }
        }
    }

    /// <summary>
    /// Whether Voice Activity Detection is enabled.
    /// </summary>
    public bool EnableVoiceActivityDetection
    {
        get => _settings.EnableVoiceActivityDetection;
        set
        {
            if (_settings.EnableVoiceActivityDetection != value)
            {
                _settings.EnableVoiceActivityDetection = value;
                Save();
            }
        }
    }

    /// <summary>
    /// Default export format: "txt", "docx", "rtf", "srt", "vtt"
    /// </summary>
    public string DefaultExportFormat
    {
        get => _settings.DefaultExportFormat;
        set
        {
            if (_settings.DefaultExportFormat != value)
            {
                _settings.DefaultExportFormat = value;
                Save();
            }
        }
    }

    /// <summary>
    /// ID of the last viewed history record.
    /// </summary>
    public string? LastViewedRecordId
    {
        get => _settings.LastViewedRecordId;
        set
        {
            if (_settings.LastViewedRecordId != value)
            {
                _settings.LastViewedRecordId = value;
                Save();
            }
        }
    }

    /// <summary>
    /// Audio playback volume (0.0 to 1.0).
    /// </summary>
    public float PlaybackVolume
    {
        get => _settings.PlaybackVolume;
        set
        {
            var clamped = Math.Clamp(value, 0f, 1f);
            if (Math.Abs(_settings.PlaybackVolume - clamped) > 0.001f)
            {
                _settings.PlaybackVolume = clamped;
                Save();
            }
        }
    }

    /// <summary>
    /// Audio playback speed (0.5 to 2.0).
    /// </summary>
    public double PlaybackSpeed
    {
        get => _settings.PlaybackSpeed;
        set
        {
            var clamped = Math.Clamp(value, 0.5, 2.0);
            if (Math.Abs(_settings.PlaybackSpeed - clamped) > 0.01)
            {
                _settings.PlaybackSpeed = clamped;
                Save();
            }
        }
    }

    /// <summary>
    /// Whether dark mode is enabled.
    /// </summary>
    public bool DarkMode
    {
        get => _settings.DarkMode;
        set
        {
            if (_settings.DarkMode != value)
            {
                _settings.DarkMode = value;
                Save();
            }
        }
    }

    /// <summary>
    /// Language code (e.g., "en", "fr").
    /// On first run, returns the system language if supported.
    /// </summary>
    public string Language
    {
        get
        {
            if (string.IsNullOrEmpty(_settings.Language))
            {
                // First run - use system language
                return Localization.LocalizationService.GetSystemLanguage();
            }
            return _settings.Language;
        }
        set
        {
            if (_settings.Language != value)
            {
                _settings.Language = value;
                Save();
            }
        }
    }

    /// <summary>
    /// Width of the left panel.
    /// </summary>
    public double LeftPanelWidth
    {
        get => _settings.LeftPanelWidth;
        set
        {
            if (Math.Abs(_settings.LeftPanelWidth - value) > 1)
            {
                _settings.LeftPanelWidth = value;
                Save();
            }
        }
    }

    /// <summary>
    /// Font size for transcript text.
    /// </summary>
    public double TranscriptFontSize
    {
        get => _settings.TranscriptFontSize;
        set
        {
            if (Math.Abs(_settings.TranscriptFontSize - value) > 0.1)
            {
                _settings.TranscriptFontSize = value;
                Save();
            }
        }
    }

    /// <summary>
    /// Audio input device ID for recording.
    /// </summary>
    public int InputDeviceId
    {
        get => _settings.InputDeviceId;
        set
        {
            if (_settings.InputDeviceId != value)
            {
                _settings.InputDeviceId = value;
                Save();
            }
        }
    }

    /// <summary>
    /// Directory where AI models are stored.
    /// </summary>
    public string ModelStorageDirectory
    {
        get => string.IsNullOrEmpty(_settings.ModelStorageDirectory)
            ? Path.Combine(_defaultAppDataPath, "Models")
            : _settings.ModelStorageDirectory;
        set
        {
            if (_settings.ModelStorageDirectory != value)
            {
                _settings.ModelStorageDirectory = value;
                Save();
                EnsureDirectoryExists(value);
            }
        }
    }

    /// <summary>
    /// Directory where audio recordings are stored.
    /// </summary>
    public string RecordingsDirectory
    {
        get => string.IsNullOrEmpty(_settings.RecordingsDirectory)
            ? Path.Combine(_defaultAppDataPath, "Recordings")
            : _settings.RecordingsDirectory;
        set
        {
            if (_settings.RecordingsDirectory != value)
            {
                _settings.RecordingsDirectory = value;
                Save();
                EnsureDirectoryExists(value);
            }
        }
    }

    /// <summary>
    /// Directory where transcription history is stored.
    /// </summary>
    public string HistoryDirectory
    {
        get => string.IsNullOrEmpty(_settings.HistoryDirectory)
            ? Path.Combine(_defaultAppDataPath, "History")
            : _settings.HistoryDirectory;
        set
        {
            if (_settings.HistoryDirectory != value)
            {
                _settings.HistoryDirectory = value;
                Save();
                EnsureDirectoryExists(value);
            }
        }
    }

    /// <summary>
    /// Whether to open files after export.
    /// </summary>
    public bool OpenFilesAfterExport
    {
        get => _settings.OpenFilesAfterExport;
        set
        {
            if (_settings.OpenFilesAfterExport != value)
            {
                _settings.OpenFilesAfterExport = value;
                Save();
            }
        }
    }

    /// <summary>
    /// Whether to apply dictation formatting (interpret spoken punctuation commands).
    /// </summary>
    public bool EnableDictationFormatting
    {
        get => _settings.EnableDictationFormatting;
        set
        {
            if (_settings.EnableDictationFormatting != value)
            {
                _settings.EnableDictationFormatting = value;
                Save();
            }
        }
    }

    /// <summary>
    /// Whether to automatically start transcription when audio is loaded or recorded.
    /// </summary>
    public bool AutoTranscribeOnImport
    {
        get => _settings.AutoTranscribeOnImport;
        set
        {
            if (_settings.AutoTranscribeOnImport != value)
            {
                _settings.AutoTranscribeOnImport = value;
                Save();
            }
        }
    }

    /// <summary>
    /// Resource usage level (1-4): 1=Light(25%), 2=Balanced(50%), 3=Performance(75%), 4=Maximum(100%)
    /// </summary>
    public int ResourceUsageLevel
    {
        get => _settings.ResourceUsageLevel;
        set
        {
            var clamped = Math.Clamp(value, 1, 4);
            if (_settings.ResourceUsageLevel != clamped)
            {
                _settings.ResourceUsageLevel = clamped;
                Save();
            }
        }
    }

    /// <summary>
    /// Gets the resource factor for the current level (0.25, 0.5, 0.75, or 1.0)
    /// </summary>
    public double GetResourceFactor()
    {
        return ResourceUsageLevel switch
        {
            1 => 0.25,
            2 => 0.5,
            3 => 0.75,
            4 => 1.0,
            _ => 0.75
        };
    }

    /// <summary>
    /// Gets the default app data path.
    /// </summary>
    public string DefaultAppDataPath => _defaultAppDataPath;

    /// <summary>
    /// Resets all settings to their default values.
    /// </summary>
    public void ResetToDefaults()
    {
        _settings = new AppSettings();
        Save();
        EnsureDirectoriesExist();
    }

    private void Load()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                var json = File.ReadAllText(_settingsFilePath);
                _settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
            }
        }
        catch
        {
            _settings = new AppSettings();
        }
    }

    private void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(_settings, JsonOptions);
            File.WriteAllText(_settingsFilePath, json);
        }
        catch
        {
            // Silently fail
        }
    }

    private void EnsureDirectoriesExist()
    {
        EnsureDirectoryExists(ModelStorageDirectory);
        EnsureDirectoryExists(RecordingsDirectory);
        EnsureDirectoryExists(HistoryDirectory);
    }

    private static void EnsureDirectoryExists(string path)
    {
        try
        {
            if (!string.IsNullOrEmpty(path) && !Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
        catch
        {
            // Silently fail
        }
    }

    private class AppSettings
    {
        public bool UseAccurateMode { get; set; } = Defaults.UseAccurateMode;
        public bool EnableVoiceActivityDetection { get; set; } = Defaults.EnableVoiceActivityDetection;
        public bool DarkMode { get; set; } = Defaults.DarkMode;
        public string? Language { get; set; } = null; // null = use system language on first run
        public string TranscriptionLanguage { get; set; } = Defaults.TranscriptionLanguage;
        public string DefaultExportFormat { get; set; } = "txt";
        public string? LastViewedRecordId { get; set; }
        public float PlaybackVolume { get; set; } = (float)Defaults.Volume;
        public double PlaybackSpeed { get; set; } = Defaults.PlaybackSpeed;
        public double LeftPanelWidth { get; set; } = Defaults.LeftPanelWidth;
        public double TranscriptFontSize { get; set; } = Defaults.TranscriptFontSize;
        public int InputDeviceId { get; set; } = 0;
        public string? ModelStorageDirectory { get; set; }
        public string? RecordingsDirectory { get; set; }
        public string? HistoryDirectory { get; set; }
        public bool OpenFilesAfterExport { get; set; } = Defaults.OpenFilesAfterExport;
        public bool EnableDictationFormatting { get; set; } = Defaults.EnableDictationFormatting;
        public bool AutoTranscribeOnImport { get; set; } = Defaults.AutoTranscribeOnImport;
        public int ResourceUsageLevel { get; set; } = Defaults.ResourceUsageLevel;
    }
}
