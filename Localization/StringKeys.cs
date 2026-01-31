namespace LynxTranscribe.Localization;

/// <summary>
/// Compile-time safe keys for all localizable strings.
/// Use with LocalizationService.Get(StringKeys.XXX)
/// </summary>
public static class StringKeys
{
    // App name and branding
    public const string AppName = "AppName";
    public const string AppVersion = "AppVersion";
    public const string PoweredBy = "PoweredBy";

    // Main navigation
    public const string Audio = "Audio";
    public const string History = "History";

    // Transcription modes
    public const string Turbo = "Turbo";
    public const string Accurate = "Accurate";

    // Main actions
    public const string Transcribe = "Transcribe";
    public const string Cancel = "Cancel";
    public const string Cancelling = "Cancelling";
    public const string Browse = "Browse";
    public const string Record = "Record";
    public const string Clear = "Clear";
    public const string ClearAll = "ClearAll";
    public const string Search = "Search";
    public const string Edit = "Edit";
    public const string Editing = "Editing";
    public const string Copy = "Copy";
    public const string Stop = "Stop";
    public const string Open = "Open";
    public const string Change = "Change";
    public const string OK = "OK";
    public const string Export = "Export";
    public const string Delete = "Delete";

    // View modes
    public const string ViewModeSegments = "ViewModeSegments";
    public const string ViewModeDocument = "ViewModeDocument";
    public const string ShowDictationCommands = "ShowDictationCommands";

    // Dialogs
    public const string DeleteConfirmTitle = "DeleteConfirmTitle";
    public const string DeleteConfirmMessage = "DeleteConfirmMessage";
    public const string ClearHistoryTitle = "ClearHistoryTitle";
    public const string ClearHistoryMessage = "ClearHistoryMessage";
    public const string RenameTitle = "RenameTitle";
    public const string RenameMessage = "RenameMessage";

    // History tooltips
    public const string DoubleClickToPlay = "DoubleClickToPlay";

    // File drop zone
    public const string DropAudioFileHere = "DropAudioFileHere";
    public const string OrBrowseFilesRecordAudio = "OrBrowseFilesRecordAudio";
    public const string SupportedFormats = "SupportedFormats";

    // Recording
    public const string GetReady = "GetReady";
    public const string ClickToSkip = "ClickToSkip";
    public const string ClickToSkipCountdown = "ClickToSkipCountdown";
    public const string Recording = "Recording";
    public const string StopRecording = "StopRecording";
    public const string UsingMicrophone = "UsingMicrophone";
    public const string NoMic = "NoMic";
    public const string NoDevice = "NoDevice";
    public const string RecordingStarted = "RecordingStarted";
    public const string RecordingCancelled = "RecordingCancelled";

    // History panel
    public const string NoHistoryYet = "NoHistoryYet";
    public const string TranscriptionsWillAppearHere = "TranscriptionsWillAppearHere";
    public const string OpenFileLocation = "OpenFileLocation";

    // Transcript panel
    public const string Transcript = "Transcript";
    public const string SearchInTranscript = "SearchInTranscript";
    public const string FindInTranscript = "FindInTranscript";
    public const string SearchHistory = "SearchHistory";
    public const string ReadyToTranscribe = "ReadyToTranscribe";
    public const string ConvertSpeechToText = "ConvertSpeechToText";
    public const string SelectOrDropAudioFile = "SelectOrDropAudioFile";
    public const string ClickTranscribeToStart = "ClickTranscribeToStart";
    public const string Words = "Words";
    public const string WordsPartial = "WordsPartial";
    public const string Segments = "Segments";
    public const string ProcessedIn = "ProcessedIn";
    public const string ProcessedInMinutes = "ProcessedInMinutes";
    public const string Elapsed = "Elapsed";
    public const string ElapsedMinutes = "ElapsedMinutes";
    public const string Remaining = "Remaining";
    public const string RemainingMinutes = "RemainingMinutes";

    // Playback
    public const string PlaybackSpeed = "PlaybackSpeed";
    public const string SpeedSlower = "SpeedSlower";
    public const string SpeedFaster = "SpeedFaster";
    public const string SpeedReset = "SpeedReset";

    // Transcription status
    public const string Transcribing = "Transcribing";
    public const string DownloadingModel = "DownloadingModel";
    public const string DownloadingModelProgress = "DownloadingModelProgress";
    public const string LoadingModel = "LoadingModel";
    public const string Calculating = "Calculating";
    public const string MBRequired = "MBRequired";

    // Settings panel
    public const string Settings = "Settings";
    public const string GeneralSettings = "GeneralSettings";
    public const string TranscriptionSettings = "TranscriptionSettings";
    public const string PressEscToClose = "PressEscToClose";
    public const string DarkMode = "DarkMode";
    public const string DarkModeDescription = "DarkModeDescription";
    public const string Language = "Language";
    public const string LanguageDescription = "LanguageDescription";
    public const string VoiceActivityDetection = "VoiceActivityDetection";
    public const string VadDescription = "VadDescription";
    public const string VadTooltip = "VadTooltip";
    public const string AutoTranscribeOnImport = "AutoTranscribeOnImport";
    public const string AutoTranscribeOnImportDescription = "AutoTranscribeOnImportDescription";
    public const string TranscriptionLanguageSetting = "TranscriptionLanguageSetting";
    public const string TranscriptionLanguageSettingDescription = "TranscriptionLanguageSettingDescription";
    public const string TranscriptionModeSetting = "TranscriptionModeSetting";
    public const string TranscriptionModeSettingDescription = "TranscriptionModeSettingDescription";
    public const string OpenFilesAfterExport = "OpenFilesAfterExport";
    public const string OpenFilesAfterExportDescription = "OpenFilesAfterExportDescription";
    public const string AudioInput = "AudioInput";
    public const string AudioInputDescription = "AudioInputDescription";
    public const string StorageLocations = "StorageLocations";
    public const string ModelDirectory = "ModelDirectory";
    public const string ModelDirectoryDescription = "ModelDirectoryDescription";
    public const string RecordingsDirectory = "RecordingsDirectory";
    public const string RecordingsDirectoryDescription = "RecordingsDirectoryDescription";
    public const string HistoryDirectory = "HistoryDirectory";
    public const string HistoryDirectoryDescription = "HistoryDirectoryDescription";
    public const string ResetToDefaults = "ResetToDefaults";
    public const string ResetConfirmMessage = "ResetConfirmMessage";
    public const string SettingsResetToDefaults = "SettingsResetToDefaults";
    public const string ModelDirectoryUpdated = "ModelDirectoryUpdated";
    public const string RecordingsDirectoryUpdated = "RecordingsDirectoryUpdated";
    public const string HistoryDirectoryUpdated = "HistoryDirectoryUpdated";
    public const string CouldNotOpenFolder = "CouldNotOpenFolder";
    public const string Error = "Error";

    // Export panel
    public const string ExportTranscript = "ExportTranscript";
    public const string PlainText = "PlainText";
    public const string PlainTextDescription = "PlainTextDescription";
    public const string WordDocument = "WordDocument";
    public const string WordDocumentDescription = "WordDocumentDescription";
    public const string RichText = "RichText";
    public const string RichTextDescription = "RichTextDescription";
    public const string SrtSubtitles = "SrtSubtitles";
    public const string SrtSubtitlesDescription = "SrtSubtitlesDescription";
    public const string WebVttSubtitles = "WebVttSubtitles";
    public const string WebVttSubtitlesDescription = "WebVttSubtitlesDescription";
    public const string OpenFileAfterExport = "OpenFileAfterExport";

    // Segment details
    public const string Duration = "Duration";
    public const string Confidence = "Confidence";
    public const string SegmentLanguage = "SegmentLanguage";
    public const string TimestampsCount = "TimestampsCount";

    // Status bar
    public const string VAD = "VAD";
    public const string On = "On";
    public const string Off = "Off";
    public const string Backend = "Backend";
    public const string Default = "Default";
    public const string VadEnabled = "VadEnabled";
    public const string VadDisabled = "VadDisabled";
    public const string NoAudioDevice = "NoAudioDevice";
    public const string NoAudioDeviceAvailable = "NoAudioDeviceAvailable";
    public const string CouldNotDetectAudioDevice = "CouldNotDetectAudioDevice";
    public const string LMKitBackend = "LMKitBackend";
    public const string CouldNotDetectBackend = "CouldNotDetectBackend";

    // Language selection
    public const string TranscriptionLanguage = "TranscriptionLanguage";
    public const string SelectLanguage = "SelectLanguage";

    // Theme toggle tooltips
    public const string SwitchToLightMode = "SwitchToLightMode";
    public const string SwitchToDarkMode = "SwitchToDarkMode";

    // Toast messages
    public const string Copied = "Copied";
    public const string RecordingSaved = "RecordingSaved";
    public const string TranscriptionComplete = "TranscriptionComplete";
    public const string TranscriptionCancelled = "TranscriptionCancelled";
    public const string TranscriptionFailed = "TranscriptionFailed";
    public const string SavedTo = "SavedTo";
    public const string RecordingError = "RecordingError";
    public const string FailedToStartRecording = "FailedToStartRecording";
    public const string CouldNotOpenFileLocation = "CouldNotOpenFileLocation";
    public const string VadEnabledNotice = "VadEnabledNotice";
    public const string NoTranscriptToSearch = "NoTranscriptToSearch";
    public const string OpenFileLocationWindowsOnly = "OpenFileLocationWindowsOnly";
    public const string AudioFileNotFound = "AudioFileNotFound";
    public const string PlaybackSpeedFormat = "PlaybackSpeedFormat";

    // Tooltips
    public const string PreviousMatch = "PreviousMatch";
    public const string NextMatch = "NextMatch";
    public const string CloseEscape = "CloseEscape";
    public const string DecreaseFontSize = "DecreaseFontSize";
    public const string IncreaseFontSize = "IncreaseFontSize";
    public const string CopyToClipboard = "CopyToClipboard";
    public const string ExportTooltip = "ExportTooltip";
    public const string RestartPlayback = "RestartPlayback";
    public const string AccurateModeTooltip = "AccurateModeTooltip";
    public const string TurboModeTooltip = "TurboModeTooltip";
    public const string VadBadgeTooltip = "VadBadgeTooltip";

    // Misc
    public const string Unknown = "Unknown";
    public const string Loading = "Loading";
    public const string Searching = "Searching";
    public const string LMKit = "LMKit";
    public const string StatusBarInfo = "StatusBarInfo";

    // Dictation formatting
    public const string DictationFormatting = "DictationFormatting";
    public const string DictationFormattingDescription = "DictationFormattingDescription";
    public const string DictationCommands = "DictationCommands";
    public const string DictationCommandsSubtitle = "DictationCommandsSubtitle";
    public const string DictationCategoryLineBreaks = "DictationCategoryLineBreaks";
    public const string DictationCategoryPunctuationLineBreak = "DictationCategoryPunctuationLineBreak";
    public const string DictationCategoryPunctuation = "DictationCategoryPunctuation";
    public const string DictationCategoryBrackets = "DictationCategoryBrackets";
    public const string DictationCategoryQuotes = "DictationCategoryQuotes";
    public const string DictationCategorySymbols = "DictationCategorySymbols";
    public const string DictationCategoryTextFormatting = "DictationCategoryTextFormatting";
    public const string ApplyDictationFormatting = "ApplyDictationFormatting";
    public const string Dictation = "Dictation";
    public const string DictationTooltip = "DictationTooltip";

    // Performance settings
    public const string PerformanceSettings = "PerformanceSettings";
    public const string ResourceUsage = "ResourceUsage";
    public const string ResourceUsageDescription = "ResourceUsageDescription";
    public const string ResourceLevelLight = "ResourceLevelLight";
    public const string ResourceLevelBalanced = "ResourceLevelBalanced";
    public const string ResourceLevelPerformance = "ResourceLevelPerformance";
    public const string ResourceLevelMaximum = "ResourceLevelMaximum";
    public const string UsingThreads = "UsingThreads";

    // File loading
    public const string LoadingFile = "LoadingFile";
    public const string ExtractingAudio = "ExtractingAudio";

    // Keyboard shortcuts
    public const string KeyboardShortcuts = "KeyboardShortcuts";
    public const string KeyboardShortcutsDescription = "KeyboardShortcutsDescription";
    public const string ShortcutCategoryGeneral = "ShortcutCategoryGeneral";
    public const string ShortcutCategoryPlayback = "ShortcutCategoryPlayback";
    public const string ShortcutCategoryTranscript = "ShortcutCategoryTranscript";
    public const string ShortcutOpenFile = "ShortcutOpenFile";
    public const string ShortcutExport = "ShortcutExport";
    public const string ShortcutSettings = "ShortcutSettings";
    public const string ShortcutClosePanel = "ShortcutClosePanel";
    public const string ShortcutPlayPause = "ShortcutPlayPause";
    public const string ShortcutSeekBack = "ShortcutSeekBack";
    public const string ShortcutSeekForward = "ShortcutSeekForward";
    public const string ShortcutSearch = "ShortcutSearch";
    public const string ShortcutNextMatch = "ShortcutNextMatch";
    public const string ShortcutPrevMatch = "ShortcutPrevMatch";
    public const string ShortcutShowShortcuts = "ShortcutShowShortcuts";
}
