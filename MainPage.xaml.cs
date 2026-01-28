using LMKit.Speech;
using LynxTranscribe.Helpers;
using LynxTranscribe.Localization;
using LynxTranscribe.Services;
using System.Diagnostics;
using L = LynxTranscribe.Localization.LocalizationService;

namespace LynxTranscribe;

/// <summary>
/// Main page - Core fields and initialization
/// Split into partial classes for maintainability:
/// - MainPage.xaml.cs (this file) - Fields, constructor
/// - MainPage.UI.cs - Drag/drop, toast, fonts, hover, lifecycle, splitter
/// - MainPage.History.cs - Tab navigation, history management
/// - MainPage.AudioPlayback.cs - Audio playback (source and history)
/// - MainPage.Recording.cs - Audio recording
/// - MainPage.Settings.cs - Settings panel, theme, model mode
/// - MainPage.Transcription.cs - File selection, transcription
/// - MainPage.Export.cs - Export actions
/// </summary>
public partial class MainPage : ContentPage
{
    // Core state
    private string? _selectedFilePath;
    private CancellationTokenSource? _cancellationTokenSource;
    private readonly Stopwatch _stopwatch = new();

    // Transcription state
    private bool _isTranscribing = false;
    private bool _isModelLoading = false;
    private bool _useAccurateMode = true;
    private bool _enableVoiceActivityDetection = true;

    // Services
    private readonly LMKitService _lmKitService = new();
    private readonly TranscriptionHistoryService _historyService = new();
    private readonly AppSettingsService _settingsService = new();
    private readonly AudioPlayerService _audioPlayer = new();        // Left panel (Audio tab)
    private readonly AudioPlayerService _historyAudioPlayer = new(); // Right panel (History playback)
    private readonly AudioRecorderService _audioRecorder = new();

    // UI State
    private string? _currentRecordId = null;
    private bool _isFileTabActive = true;
    private bool _isLoadingHistoryRecord = false;
    private bool _isDraggingSlider = false;
    private string? _historyAudioFilePath = null;
    private List<AudioSegment> _currentSegments = new();
    private IDispatcherTimer? _playbackTimer;        // Left panel timer
    private IDispatcherTimer? _historyPlaybackTimer; // Right panel timer
    private bool _isEditMode = false;
    private bool _isDocumentView = false;

    // Recording state
    private bool _isRecording = false;
    private bool _isCountingDown = false;
    private IDispatcherTimer? _recordingDotTimer;
    private int _selectedInputDeviceId = 0;
    private bool _hasInputDevices = false;

    // Playback
    private double _playbackSpeed = 1.0;
    private WaveformDrawable _waveformDrawable = new();

    // Toast notification
    private CancellationTokenSource? _toastCts;

    // Estimated time tracking
    private DateTime _transcriptionStartTime;
    private double _lastProgressValue = 0;

    // Font size - use defaults from AppSettingsService
    private double _transcriptFontSize = AppSettingsService.Defaults.TranscriptFontSize;
    private static double MinFontSize => AppSettingsService.Defaults.MinFontSize;
    private static double MaxFontSize => AppSettingsService.Defaults.MaxFontSize;

    // Splitter - use defaults from AppSettingsService
    private double _leftPanelWidth = AppSettingsService.Defaults.LeftPanelWidth;
    private const double MinPanelWidth = AppConstants.Layout.MinPanelWidth;
    private const double MaxPanelWidth = AppConstants.Layout.MaxPanelWidth;
    private bool _isDraggingSplitter = false;
    private double _dragStartX;
    private double _dragStartWidth;

    // Waveform animation
    private IDispatcherTimer? _waveformAnimationTimer;
    private bool _isWaveformAnimating = false;

    // App lifecycle
    private bool _isClosing = false;

    public MainPage()
    {
        InitializeComponent();

        // Load persisted settings
        _useAccurateMode = _settingsService.UseAccurateMode;
        _enableVoiceActivityDetection = _settingsService.EnableVoiceActivityDetection;

        // Initialize localization
        Localization.LocalizationService.Instance.SetLanguage(_settingsService.Language);

        // Initialize the themed controls registry (before ApplyTheme)
        InitializeThemedControls();

        // Apply saved theme
        ApplyTheme(_settingsService.DarkMode);

        // Initialize theme toggle state
        UpdateThemeToggleUI(_settingsService.DarkMode);

        SystemEx.Initialize(_settingsService);

        // Update backend label
        UpdateBackendLabel();

        // Configure service paths
        _historyService.SetHistoryDirectory(_settingsService.HistoryDirectory);
        _audioRecorder.SetRecordingsDirectory(_settingsService.RecordingsDirectory);

        // Setup audio players
        InitializeAudioPlayers();

        // Initialize language picker
        InitializeTranscriptionLanguagePicker();

        // Initialize UI state
        UpdateTranscribeButtonState();
        UpdateModelModeUI();
        UpdateHistoryBadge();
        UpdateSettingsUI();
        UpdateStatusBar();

        // Initialize left panel width from settings
        _leftPanelWidth = _settingsService.LeftPanelWidth;
        LeftPanel.WidthRequest = _leftPanelWidth;

        // Initialize transcript font size from settings
        _transcriptFontSize = _settingsService.TranscriptFontSize;

        // Initialize waveform drawable
        InitializeWaveform();

        // Setup audio recorder
        InitializeAudioRecorder();

        // Setup keyboard shortcuts for search
        SetupKeyboardShortcuts();

        // Apply localized strings (must be after all UI initialization)
        RefreshLocalizedStrings();
    }

    private void InitializeTranscriptionLanguagePicker()
    {
        // Set button text to current selection
        var currentCode = _settingsService.TranscriptionLanguage;
        TranscriptionLanguageLabel.Text = WhisperLanguages.GetDisplayNameFromCode(currentCode);

        // Build the language list
        BuildLanguageSelectionList();
    }

    private void BuildLanguageSelectionList()
    {
        LanguageSelectionList.Children.Clear();

        var languages = WhisperLanguages.GetDisplayNames();
        var currentCode = _settingsService.TranscriptionLanguage;

        foreach (var langName in languages)
        {
            var code = WhisperLanguages.GetCodeFromDisplayName(langName);
            var countryCode = WhisperLanguages.GetCountryCodeFromCode(code);
            var isSelected = code == currentCode;

            var item = new Border
            {
                BackgroundColor = isSelected ? (Color)Resources["AccentSurface"]! : Colors.Transparent,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 4 },
                Stroke = isSelected ? (Color)Resources["AccentPrimary"]! : Colors.Transparent,
                StrokeThickness = isSelected ? 1 : 0,
                Padding = new Thickness(8, 6),
                ClassId = code
            };

            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition(new GridLength(36)),
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Auto)
                },
                ColumnSpacing = 6
            };

            // Language code on left with accent color
            var codeLabel = new Label
            {
                Text = countryCode,
                FontSize = 10,
                FontAttributes = FontAttributes.Bold,
                TextColor = (Color)Resources["AccentText"]!,
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Start
            };
            grid.Children.Add(codeLabel);

            // Language name
            var nameLabel = new Label
            {
                Text = langName,
                FontSize = 12,
                TextColor = isSelected ? (Color)Resources["AccentText"]! : (Color)Resources["TextPrimary"]!,
                VerticalOptions = LayoutOptions.Center
            };
            Grid.SetColumn(nameLabel, 1);
            grid.Children.Add(nameLabel);

            // Checkmark for selected
            if (isSelected)
            {
                var checkLabel = new Label
                {
                    Text = "✓",
                    FontSize = 12,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = (Color)Resources["AccentText"]!,
                    VerticalOptions = LayoutOptions.Center
                };
                Grid.SetColumn(checkLabel, 2);
                grid.Children.Add(checkLabel);
            }

            item.Content = grid;

            // Tap to select
            var tap = new TapGestureRecognizer();
            var capturedCode = code;
            tap.Tapped += (s, e) => OnLanguageItemSelected(capturedCode);
            item.GestureRecognizers.Add(tap);

            // Hover effect
            var hover = new PointerGestureRecognizer();
            var capturedItem = item;
            var capturedIsSelected = isSelected;
            hover.PointerEntered += (s, e) =>
            {
                if (!capturedIsSelected)
                {
                    capturedItem.BackgroundColor = (Color)Resources["BackgroundTertiary"]!;
                }
            };
            hover.PointerExited += (s, e) =>
            {
                if (!capturedIsSelected)
                {
                    capturedItem.BackgroundColor = Colors.Transparent;
                }
            };
            item.GestureRecognizers.Add(hover);

            LanguageSelectionList.Children.Add(item);
        }
    }

    private void OnLanguageItemSelected(string code)
    {
        _settingsService.TranscriptionLanguage = code;
        var displayName = WhisperLanguages.GetDisplayNameFromCode(code);
        TranscriptionLanguageLabel.Text = displayName;
        SettingsLanguageLabel.Text = displayName;
        LanguageSelectionOverlay.IsVisible = false;
        BuildLanguageSelectionList(); // Rebuild for next time
    }

    private async void OnTranscriptionLanguageButtonClicked(object? sender, TappedEventArgs e)
    {
        // Show overlay first so it gets measured
        LanguageSelectionOverlay.IsVisible = true;

        // Wait for layout pass to complete
        await Task.Delay(10);

        PositionLanguageSelectionPopup();
    }

    private async void OnSettingsLanguageButtonClicked(object? sender, TappedEventArgs e)
    {
        // Use the same language selection overlay but position it differently
        LanguageSelectionOverlay.IsVisible = true;

        await Task.Delay(10);

        PositionLanguageSelectionPopupForSettings();
    }

    private void PositionLanguageSelectionPopupForSettings()
    {
        // Position popup centered in the overlay for settings panel
        // since the settings panel is on the right side
        var overlayWidth = LanguageSelectionOverlay.Width;
        var overlayHeight = LanguageSelectionOverlay.Height;

        if (overlayWidth <= 0 || overlayHeight <= 0)
        {
            LanguageSelectionPopup.TranslationX = 0;
            LanguageSelectionPopup.TranslationY = 0;
            return;
        }

        // Get the settings button position
        var buttonBounds = GetAbsoluteBounds(SettingsLanguageButton);

        if (buttonBounds.Width <= 0)
        {
            LanguageSelectionPopup.TranslationX = 0;
            LanguageSelectionPopup.TranslationY = 0;
            return;
        }

        const double popupWidth = 220;
        const double popupHeight = 320;

        // Position to the left of the button (since settings is on right side)
        double targetX = buttonBounds.Left - popupWidth - 12;
        double targetY = buttonBounds.Center.Y - (popupHeight / 2);

        double centerX = overlayWidth / 2;
        double centerY = overlayHeight / 2;

        double translateX = targetX + (popupWidth / 2) - centerX;
        double translateY = targetY + (popupHeight / 2) - centerY;

        // Clamp to viewport
        double maxTranslateX = (overlayWidth / 2) - (popupWidth / 2) - 20;
        double minTranslateX = -(overlayWidth / 2) + (popupWidth / 2) + 20;
        double maxTranslateY = (overlayHeight / 2) - (popupHeight / 2) - 20;
        double minTranslateY = -(overlayHeight / 2) + (popupHeight / 2) + 20;

        translateX = Math.Clamp(translateX, minTranslateX, maxTranslateX);
        translateY = Math.Clamp(translateY, minTranslateY, maxTranslateY);

        LanguageSelectionPopup.TranslationX = translateX;
        LanguageSelectionPopup.TranslationY = translateY;
    }

    private void OnSettingsLanguageButtonHoverEnter(object? sender, PointerEventArgs e)
    {
        SettingsLanguageButton.BackgroundColor = (Color)Resources["BackgroundSecondary"]!;
    }

    private void OnSettingsLanguageButtonHoverExit(object? sender, PointerEventArgs e)
    {
        SettingsLanguageButton.BackgroundColor = (Color)Resources["BackgroundTertiary"]!;
    }

    private void PositionLanguageSelectionPopup()
    {
        // Position the popup to the right of the button
        // We use translation from center since the popup has Center/Center alignment

        // Get the overlay container size
        var overlayWidth = LanguageSelectionOverlay.Width;
        var overlayHeight = LanguageSelectionOverlay.Height;

        if (overlayWidth <= 0 || overlayHeight <= 0)
        {
            // Use page dimensions as fallback
            overlayWidth = this.Width;
            overlayHeight = this.Height;
        }

        if (overlayWidth <= 0 || overlayHeight <= 0)
        {
            // Still no dimensions, center it
            LanguageSelectionPopup.TranslationX = 0;
            LanguageSelectionPopup.TranslationY = 0;
            return;
        }

        // Popup dimensions
        const double popupWidth = 220;
        const double popupHeight = 320;

        // Get the absolute position of the button using platform-specific bounds
        var buttonAbsoluteBounds = GetAbsoluteBounds(TranscriptionLanguageButton);

        if (buttonAbsoluteBounds.Width <= 0)
        {
            // Fallback to centered position
            LanguageSelectionPopup.TranslationX = 0;
            LanguageSelectionPopup.TranslationY = 0;
            return;
        }

        // Calculate target position: to the right of the button
        double targetX = buttonAbsoluteBounds.Right + 12;
        double targetY = buttonAbsoluteBounds.Center.Y - (popupHeight / 2);

        // The popup uses Center/Center positioning, so calculate offset from center
        double centerX = overlayWidth / 2;
        double centerY = overlayHeight / 2;

        // Calculate translation to move from center to target position
        double translateX = targetX + (popupWidth / 2) - centerX;
        double translateY = targetY + (popupHeight / 2) - centerY;

        // Clamp to ensure popup stays within viewport with padding
        double maxTranslateX = (overlayWidth / 2) - (popupWidth / 2) - 20;
        double minTranslateX = -(overlayWidth / 2) + (popupWidth / 2) + 20;
        double maxTranslateY = (overlayHeight / 2) - (popupHeight / 2) - 20;
        double minTranslateY = -(overlayHeight / 2) + (popupHeight / 2) + 20;

        translateX = Math.Clamp(translateX, minTranslateX, maxTranslateX);
        translateY = Math.Clamp(translateY, minTranslateY, maxTranslateY);

        LanguageSelectionPopup.TranslationX = translateX;
        LanguageSelectionPopup.TranslationY = translateY;
    }

    private Rect GetAbsoluteBounds(VisualElement element)
    {
        // Walk up the visual tree to calculate absolute position
        double x = element.X;
        double y = element.Y;
        double width = element.Width;
        double height = element.Height;

        var parent = element.Parent as VisualElement;
        while (parent != null)
        {
            x += parent.X;
            y += parent.Y;

            // Account for scroll positions if parent is a ScrollView
            if (parent is ScrollView scrollView)
            {
                x -= scrollView.ScrollX;
                y -= scrollView.ScrollY;
            }

            parent = parent.Parent as VisualElement;
        }

        return new Rect(x, y, width, height);
    }

    private void OnTranscriptionLanguageButtonHoverEnter(object? sender, PointerEventArgs e)
    {
        TranscriptionLanguageButton.BackgroundColor = (Color)Resources["BackgroundSecondary"]!;
    }

    private void OnTranscriptionLanguageButtonHoverExit(object? sender, PointerEventArgs e)
    {
        TranscriptionLanguageButton.BackgroundColor = (Color)Resources["BackgroundTertiary"]!;
    }

    private void OnLanguageSelectionOverlayClicked(object? sender, TappedEventArgs e)
    {
        LanguageSelectionOverlay.IsVisible = false;
    }

    private void OnLanguageSelectionCloseClicked(object? sender, TappedEventArgs e)
    {
        LanguageSelectionOverlay.IsVisible = false;
    }

    private void OnLanguageSelectionCloseHoverEnter(object? sender, PointerEventArgs e)
    {
        LanguageSelectionCloseButton.BackgroundColor = (Color)Resources["BackgroundTertiary"]!;
    }

    private void OnLanguageSelectionCloseHoverExit(object? sender, PointerEventArgs e)
    {
        LanguageSelectionCloseButton.BackgroundColor = Colors.Transparent;
    }

    private void InitializeAudioPlayers()
    {
        // Setup audio player (left panel - Audio tab)
        _audioPlayer.PositionChanged += OnAudioPositionChanged;
        _audioPlayer.PlaybackStopped += OnAudioPlaybackStopped;
        _audioPlayer.Volume = _settingsService.PlaybackVolume;

        // Setup playback timer (left panel)
        _playbackTimer = Dispatcher.CreateTimer();
        _playbackTimer.Interval = TimeSpan.FromMilliseconds(AppConstants.Playback.TimerIntervalMs);
        _playbackTimer.Tick += (s, e) => _audioPlayer.UpdatePosition();

        // Setup history audio player (right panel - History playback)
        _historyAudioPlayer.PositionChanged += OnHistoryAudioPositionChanged;
        _historyAudioPlayer.PlaybackStopped += OnHistoryAudioPlaybackStopped;
        _historyAudioPlayer.Volume = _settingsService.PlaybackVolume;
        HistoryVolumeSlider.Value = _settingsService.PlaybackVolume;

        // Setup history playback timer (right panel)
        _historyPlaybackTimer = Dispatcher.CreateTimer();
        _historyPlaybackTimer.Interval = TimeSpan.FromMilliseconds(AppConstants.Playback.TimerIntervalMs);
        _historyPlaybackTimer.Tick += (s, e) => _historyAudioPlayer.UpdatePosition();

        // Load saved playback speed
        _playbackSpeed = _settingsService.PlaybackSpeed;
        _audioPlayer.SetPlaybackSpeed(_playbackSpeed);
        _historyAudioPlayer.SetPlaybackSpeed(_playbackSpeed);
        UpdateSpeedButtonsUI();
        UpdateHistorySpeedUI();

        // Pre-warm the audio subsystem to avoid first-time initialization delays
        PreWarmAudioSubsystem();
    }

    private void PreWarmAudioSubsystem()
    {
        // Warm up NAudio's WaveOutEvent to ensure the audio subsystem is ready
        // when the user first loads a history item. Do this synchronously to
        // ensure it completes before user interaction.
        try
        {
            using var warmup = new NAudio.Wave.WaveOutEvent();
            System.Diagnostics.Debug.WriteLine("Audio subsystem pre-warmed successfully");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Audio warmup warning: {ex.Message}");
            // Continue anyway - the app can still function
        }
    }

    private void InitializeAudioRecorder()
    {
        _audioRecorder.DurationChanged += OnRecordingDurationChanged;
        _audioRecorder.LevelChanged += OnRecordingLevelChanged;
        _audioRecorder.RecordingStopped += OnRecordingCompleted;
        _audioRecorder.RecordingError += OnRecordingError;

        // Initialize input device picker
        _selectedInputDeviceId = _settingsService.InputDeviceId;
        PopulateInputDevices();
    }

    private void UpdateBackendLabel()
    {
        try
        {
            var backend = LMKit.Global.Runtime.Backend;
            var backendName = backend.ToString();
            BackendLabel.Text = backendName;
            ToolTipProperties.SetText(BackendLabel, L.Localize(StringKeys.LMKitBackend, backendName));
        }
        catch
        {
            BackendLabel.Text = L.Localize(StringKeys.Unknown);
            ToolTipProperties.SetText(BackendLabel, L.Localize(StringKeys.CouldNotDetectBackend));
        }
    }

    private void UpdateStatusBar()
    {
        // Update VAD status
        UpdateVadStatus();

        // Update audio input status
        UpdateAudioInputStatus();

        // Update backend
        UpdateBackendLabel();
    }

    private void UpdateVadStatus()
    {
        if (_enableVoiceActivityDetection)
        {
            VadStatusLabel.Text = L.Localize(StringKeys.On);
            VadStatusLabel.TextColor = Color.FromArgb("#22c55e");
            VadStatusBadge.BackgroundColor = (Color)Resources["SuccessSurface"]!;
            ToolTipProperties.SetText(VadStatusBadge, L.Localize(StringKeys.VadEnabled));
        }
        else
        {
            VadStatusLabel.Text = L.Localize(StringKeys.Off);
            VadStatusLabel.TextColor = Color.FromArgb("#ef4444");
            VadStatusBadge.BackgroundColor = (Color)Resources["DangerSurface"]!;
            ToolTipProperties.SetText(VadStatusBadge, L.Localize(StringKeys.VadDisabled));
        }
    }

    private void UpdateAudioInputStatus()
    {
        try
        {
            var devices = AudioRecorderService.GetInputDevices();
            if (devices.Count > 0 && _selectedInputDeviceId < devices.Count)
            {
                var deviceName = devices[_selectedInputDeviceId].Name;
                AudioInputStatusLabel.Text = deviceName;
                ToolTipProperties.SetText(AudioInputStatusLabel, deviceName);
            }
            else
            {
                AudioInputStatusLabel.Text = L.Localize(StringKeys.NoAudioDevice);
                ToolTipProperties.SetText(AudioInputStatusLabel, L.Localize(StringKeys.NoAudioDeviceAvailable));
            }
        }
        catch
        {
            AudioInputStatusLabel.Text = L.Localize(StringKeys.Unknown);
            ToolTipProperties.SetText(AudioInputStatusLabel, L.Localize(StringKeys.CouldNotDetectAudioDevice));
        }
    }
}
