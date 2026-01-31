namespace LynxTranscribe;

/// <summary>
/// Application-wide constants. Single source of truth for version, timing, and UI values.
/// </summary>
public static class AppConstants
{
    public const string Version = "2026.1.6";
    public const string AppName = "LynxTranscribe";
    public static string VersionString => $"{AppName} v{Version}";

    /// <summary>
    /// Audio playback constants
    /// </summary>
    public static class Playback
    {
        public const int SeekSeconds = 5;
        public const int TimerIntervalMs = 100;
        public const int SegmentSeekOffsetMs = 1;
    }

    /// <summary>
    /// UI timing constants (milliseconds)
    /// </summary>
    public static class Timing
    {
        public const int ToastDurationMs = 3000;
        public const int UiSettleDelayMs = 50;
        public const int AnimationDurationMs = 300;
        public const int RecordingDotBlinkMs = 500;
        public const int CountdownStepDelayMs = 100;
        public const int WaveformAnimationIntervalMs = 10000;
    }

    /// <summary>
    /// Panel and layout constants
    /// </summary>
    public static class Layout
    {
        public const double MinPanelWidth = 260;
        public const double MaxPanelWidth = 500;
        public const double AudioLevelBarMaxWidth = 200;
    }

    /// <summary>
    /// Recording constants
    /// </summary>
    public static class Recording
    {
        public const int CountdownSeconds = 3;
        public const int CountdownCheckIntervals = 7;
    }

    /// <summary>
    /// Supported media file extensions (audio and video containers)
    /// Video formats use Windows Media Foundation for audio extraction.
    /// </summary>
    public static readonly string[] SupportedMediaExtensions =
    {
        ".wav", ".mp3", ".flac", ".ogg", ".m4a", ".wma",
        ".mp4", ".aac", ".avi", ".mov"
    };

    /// <summary>
    /// Gets a formatted string of supported formats for display
    /// </summary>
    public static string SupportedFormatsDisplay =>
        string.Join(", ", SupportedMediaExtensions.Select(e => e.TrimStart('.').ToUpperInvariant()));

    public static bool IsSupportedMediaFile(string filePath)
    {
        var ext = Path.GetExtension(filePath)?.ToLowerInvariant();
        return ext != null && Array.Exists(SupportedMediaExtensions, e => e == ext);
    }
}
