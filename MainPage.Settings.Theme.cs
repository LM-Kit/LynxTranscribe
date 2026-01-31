using LynxTranscribe.Helpers;
using LynxTranscribe.Localization;
using LynxTranscribe.Services;
using L = LynxTranscribe.Localization.LocalizationService;
namespace LynxTranscribe;

/// <summary>
/// Settings: Theme toggle and theme application methods
/// </summary>
public partial class MainPage
{
    #region Status Bar Theme Toggle

    /// <summary>
    /// Handles click on the status bar theme toggle button
    /// </summary>
    private void OnThemeToggleClicked(object? sender, TappedEventArgs e)
    {
        var newDarkMode = !_settingsService.DarkMode;
        _settingsService.DarkMode = newDarkMode;

        ApplyTheme(newDarkMode);
        UpdateThemeToggleUI(newDarkMode);

        // Sync settings panel switch
        DarkModeToggle.IsToggled = newDarkMode;
    }

    /// <summary>
    /// Updates the theme toggle icon and tooltip
    /// </summary>
    private void UpdateThemeToggleUI(bool isDark)
    {
        if (isDark)
        {
            // In dark mode: show sun icon (click to switch to light)
            ThemeToggleIcon.Text = "☀️";
            ToolTipProperties.SetText(ThemeToggle, Localization.LocalizationService.Localize(Localization.StringKeys.SwitchToLightMode));
        }
        else
        {
            // In light mode: show moon icon (click to switch to dark)
            ThemeToggleIcon.Text = "🌙";
            ToolTipProperties.SetText(ThemeToggle, Localization.LocalizationService.Localize(Localization.StringKeys.SwitchToDarkMode));
        }
    }

    private async void OnThemeToggleHoverEnter(object? sender, PointerEventArgs e)
    {
        // Smooth scale animation (subtle for status bar)
        await ThemeToggle.ScaleToAsync(1.15, 100, Easing.CubicOut);

        // Add soft ambient glow
        ThemeToggle.Shadow = new Shadow
        {
            Brush = new SolidColorBrush((Color)Resources["AccentPrimary"]!),
            Offset = new Point(0, 0),
            Radius = 8,
            Opacity = 0.4f
        };
    }

    private async void OnThemeToggleHoverExit(object? sender, PointerEventArgs e)
    {
        // Smooth scale back
        await ThemeToggle.ScaleToAsync(1.0, 100, Easing.CubicOut);

        // Remove glow
        ThemeToggle.Shadow = new Shadow { Opacity = 0 };
    }

    #endregion

    /// <summary>
    /// Applies the theme by updating the resource dictionary.
    /// XAML elements using {DynamicResource} will auto-update.
    /// Programmatically created elements are refreshed via the themed controls system.
    /// </summary>
    private void ApplyTheme(bool isDark)
    {
        var resources = this.Resources;

        if (isDark)
        {
            resources["BackgroundPrimary"] = Color.FromArgb("#0A0A0B");
            resources["BackgroundSecondary"] = Color.FromArgb("#111113");
            resources["BackgroundTertiary"] = Color.FromArgb("#18181B");
            resources["SurfaceColor"] = Color.FromArgb("#1A1A1E");
            resources["SurfaceBorder"] = Color.FromArgb("#3F3F46");
            resources["SurfaceBorderSubtle"] = Color.FromArgb("#27272A");
            resources["AccentPrimary"] = Color.FromArgb("#F59E0B");
            resources["AccentText"] = Color.FromArgb("#F59E0B");
            resources["AccentSurface"] = Color.FromArgb("#2D2011");
            resources["AccentMuted"] = Color.FromArgb("#92400E");
            resources["SuccessSurface"] = Color.FromArgb("#052E1C");
            resources["SuccessColor"] = Color.FromArgb("#22C55E");
            resources["DangerSurface"] = Color.FromArgb("#2D1515");
            resources["DangerColor"] = Color.FromArgb("#EF4444");
            resources["TextPrimary"] = Color.FromArgb("#FAFAFA");
            resources["TextSecondary"] = Color.FromArgb("#D4D4D8");
            resources["TextTertiary"] = Color.FromArgb("#A1A1AA");
            resources["TextMuted"] = Color.FromArgb("#71717A");
            resources["DisabledOverlay"] = Color.FromArgb("#60000000");
        }
        else
        {
            resources["BackgroundPrimary"] = Color.FromArgb("#FFFFFF");
            resources["BackgroundSecondary"] = Color.FromArgb("#FAFAFA");
            resources["BackgroundTertiary"] = Color.FromArgb("#F4F4F5");
            resources["SurfaceColor"] = Color.FromArgb("#FFFFFF");
            resources["SurfaceBorder"] = Color.FromArgb("#E4E4E7");
            resources["SurfaceBorderSubtle"] = Color.FromArgb("#F4F4F5");
            resources["AccentPrimary"] = Color.FromArgb("#D97706");
            resources["AccentText"] = Color.FromArgb("#78350F");
            resources["AccentSurface"] = Color.FromArgb("#FEF3C7");
            resources["AccentMuted"] = Color.FromArgb("#B45309");
            resources["SuccessSurface"] = Color.FromArgb("#D1FAE5");
            resources["SuccessColor"] = Color.FromArgb("#16A34A");
            resources["DangerSurface"] = Color.FromArgb("#FEE2E2");
            resources["DangerColor"] = Color.FromArgb("#DC2626");
            resources["TextPrimary"] = Color.FromArgb("#18181B");
            resources["TextSecondary"] = Color.FromArgb("#3F3F46");
            resources["TextTertiary"] = Color.FromArgb("#52525B");
            resources["TextMuted"] = Color.FromArgb("#71717A");
            resources["DisabledOverlay"] = Color.FromArgb("#40FFFFFF");
        }

        // Refresh all UI elements
        RefreshAllUIOnThemeChange();
    }

    /// <summary>
    /// Master refresh method called after theme change.
    /// Ensures ALL UI elements are updated with new theme colors.
    /// </summary>
    private void RefreshAllUIOnThemeChange()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            // 1. Force visual tree to re-evaluate DynamicResource bindings
            ForceVisualTreeRefresh();

            // 2. Refresh all registered themed controls (buttons, tabs, etc.)
            RefreshAllThemedControls();

            // 3. Rebuild programmatically created content (segments, history items)
            RebuildDynamicContent();

            // 4. Update complex state-dependent UI
            RefreshComplexUI();

            // 5. Update misc elements that have custom styling
            RefreshMiscElements();

            // 6. Update view mode UI if visible
            if (ViewModeToggle.IsVisible)
            {
                UpdateViewModeUI();
            }

            // 7. Notify all registered callbacks and fire ThemeChanged event
            NotifyThemeChanged();
        });
    }

    /// <summary>
    /// Rebuilds all dynamically created UI elements (lists, etc.)
    /// </summary>
    private void RebuildDynamicContent()
    {
        // Rebuild history list
        RefreshHistoryList();

        // Rebuild segment views if we have segments
        if (_currentSegments.Count > 0 && TranscriptContainer.IsVisible)
        {
            // Use BuildSegmentView which properly clears tracking lists
            BuildSegmentView();
        }

        // Rebuild language selection list for theme colors
        BuildLanguageSelectionList();
    }

    /// <summary>
    /// Updates complex UI elements that have state-dependent styling.
    /// </summary>
    private void RefreshComplexUI()
    {
        UpdateFileSelectionUI();
        UpdateWaveformColors();
        UpdateSpeedButtonsUI();
        UpdateTranscribeButtonState();

        // Update settings panel elements that have state-dependent styling
        UpdateModelModeUI();
        UpdateSettingsModelModeUI();
        UpdateVadToggleUI();
        UpdateAutoTranscribeInlineUI();
        UpdateDictationIndicator();
        UpdateResourceUsageLevelUI();
        RefreshSpeedControlTheme();
    }

    /// <summary>
    /// Updates miscellaneous elements with custom styling.
    /// </summary>
    private void RefreshMiscElements()
    {
        var r = this.Resources;

        // Watermark image - swap based on theme
        WatermarkLogo.Source = _settingsService.DarkMode ? "watermark_dark.png" : "watermark_light.png";

        // Settings button (neutral styling)
        var backgroundTertiary = (Color)r["BackgroundTertiary"]!;
        var surfaceBorder = (Color)r["SurfaceBorder"]!;
        var textSecondary = (Color)r["TextSecondary"]!;
        var textPrimary = (Color)r["TextPrimary"]!;
        var textMuted = (Color)r["TextMuted"]!;

        SettingsButtonBorder.BackgroundColor = backgroundTertiary;
        SettingsButtonBorder.Stroke = surfaceBorder;
        SettingsLine1.Stroke = new SolidColorBrush(textSecondary);
        SettingsLine2.Stroke = new SolidColorBrush(textSecondary);
        SettingsLine3.Stroke = new SolidColorBrush(textSecondary);
        SettingsDot1.Fill = new SolidColorBrush(textSecondary);
        SettingsDot2.Fill = new SolidColorBrush(textSecondary);
        SettingsDot3.Fill = new SolidColorBrush(textSecondary);

        // History count badge
        HistoryCountBadge.BackgroundColor = backgroundTertiary;

        // Search bar background
        SearchBar.BackgroundColor = (Color)r["BackgroundTertiary"]!;

        // Font control background
        FontSizeControls.BackgroundColor = (Color)r["BackgroundTertiary"]!;

        // Theme toggle button - reset to default state
        ThemeToggle.BackgroundColor = Colors.Transparent;
        ThemeToggle.Stroke = Colors.Transparent;
        ThemeToggle.Scale = 1.0;
        ThemeToggle.Shadow = new Shadow { Opacity = 0 };

        // Play buttons - solid orange with white icon
        var accentPrimary = (Color)r["AccentPrimary"]!;

        PlayPauseButton.BackgroundColor = accentPrimary;
        PlayPauseButton.Stroke = Colors.Transparent;
        PlayPauseIcon.TextColor = Colors.White;

        HistoryPlayPauseButton.BackgroundColor = accentPrimary;
        HistoryPlayPauseButton.Stroke = Colors.Transparent;
        HistoryPlayPauseIcon.TextColor = Colors.White;

        // Toolbar buttons
        var surfaceColor = (Color)r["SurfaceColor"]!;

        // Restart button
        RestartButton.BackgroundColor = backgroundTertiary;
        RestartButton.Stroke = surfaceBorder;
        RestartIcon.TextColor = textSecondary;

        SearchButton.BackgroundColor = backgroundTertiary;
        SearchButton.Stroke = surfaceBorder;
        SearchButtonLabel.TextColor = (Color)r["TextPrimary"]!;

        EditToggle.BackgroundColor = backgroundTertiary;
        EditToggle.Stroke = surfaceBorder;
        EditToggleLabel.TextColor = (Color)r["TextPrimary"]!;

        DictationToggle.BackgroundColor = backgroundTertiary;
        DictationToggle.Stroke = surfaceBorder;
        DictationToggleLabel.TextColor = (Color)r["TextPrimary"]!;

        DictationHelpToolbarButton.BackgroundColor = backgroundTertiary;
        DictationHelpToolbarButton.Stroke = surfaceBorder;
        DictationHelpToolbarButtonLabel.TextColor = (Color)r["TextTertiary"]!;

        // View mode toggle
        ViewModeToggle.BackgroundColor = surfaceColor;
        ViewModeToggle.Stroke = surfaceBorder;

        // Settings panel dropdown buttons
        TranscriptionLanguageButton.BackgroundColor = backgroundTertiary;
        TranscriptionLanguageButton.Stroke = surfaceBorder;
        TranscriptionLanguageLabel.TextColor = textPrimary;

        SettingsLanguageButton.BackgroundColor = backgroundTertiary;
        SettingsLanguageButton.Stroke = surfaceBorder;
        SettingsLanguageLabel.TextColor = textPrimary;

        // Dictation help button in settings
        DictationHelpButton.BackgroundColor = Colors.Transparent;
        DictationHelpButton.Stroke = surfaceBorder;
    }

    /// <summary>
    /// Updates UI elements that depend on whether a file is selected.
    /// </summary>
    private void UpdateFileSelectionUI()
    {
        var resources = this.Resources;
        var backgroundSecondary = (Color)resources["BackgroundSecondary"]!;
        var backgroundTertiary = (Color)resources["BackgroundTertiary"]!;
        var surfaceBorder = (Color)resources["SurfaceBorder"]!;
        var accentSurface = (Color)resources["AccentSurface"]!;
        var accentMuted = (Color)resources["AccentMuted"]!;
        var accentText = (Color)resources["AccentText"]!;
        var textSecondary = (Color)resources["TextSecondary"]!;
        var textTertiary = (Color)resources["TextTertiary"]!;
        var textMuted = (Color)resources["TextMuted"]!;

        if (_selectedFilePath == null)
        {
            // No file selected - Step 1 active
            DropZone.BackgroundColor = backgroundSecondary;
            DropZone.Stroke = surfaceBorder;
            DropZone.StrokeDashArray = new DoubleCollection { 6, 4 };

            Step1Badge.BackgroundColor = accentSurface;
            Step1Badge.Stroke = accentMuted;
            Step1Badge.StrokeDashArray = new DoubleCollection { 4, 3 };
            Step1Label.TextColor = accentText;
            Step1Text.TextColor = textSecondary;

            Step2Badge.BackgroundColor = backgroundTertiary;
            Step2Badge.Stroke = surfaceBorder;
            Step2Badge.StrokeDashArray = null;
            Step2Label.TextColor = textTertiary;
            Step2Text.TextColor = textMuted;
        }
        else
        {
            // File selected - Step 2 active
            DropZone.BackgroundColor = accentSurface;
            DropZone.Stroke = accentMuted;
            DropZone.StrokeDashArray = null;

            Step1Badge.BackgroundColor = backgroundTertiary;
            Step1Badge.Stroke = surfaceBorder;
            Step1Badge.StrokeDashArray = null;
            Step1Label.TextColor = textTertiary;
            Step1Text.TextColor = textMuted;

            Step2Badge.BackgroundColor = accentSurface;
            Step2Badge.Stroke = accentMuted;
            Step2Badge.StrokeDashArray = new DoubleCollection { 4, 3 };
            Step2Label.TextColor = accentText;
            Step2Text.TextColor = textSecondary;
        }
    }

    private async void OnResetSettingsClicked(object? sender, TappedEventArgs e)
    {
        // Show confirmation dialog
        bool confirmed = await this.DisplayAlertAsync(
            L.Localize(StringKeys.ResetToDefaults),
            L.Localize(StringKeys.ResetConfirmMessage),
            L.Localize(StringKeys.OK),
            L.Localize(StringKeys.Cancel));

        if (!confirmed)
        {
            return;
        }

        // Preserve current theme setting
        var currentTheme = _settingsService.DarkMode;

        _settingsService.ResetToDefaults();

        // Restore theme setting
        _settingsService.DarkMode = currentTheme;

        _enableVoiceActivityDetection = AppSettingsService.Defaults.EnableVoiceActivityDetection;
        _useAccurateMode = AppSettingsService.Defaults.UseAccurateMode;
        _audioPlayer.Volume = (float)AppSettingsService.Defaults.Volume;
        _historyAudioPlayer.Volume = (float)AppSettingsService.Defaults.Volume;

        // Reset playback speed
        _playbackSpeed = AppSettingsService.Defaults.PlaybackSpeed;
        _audioPlayer.SetPlaybackSpeed(_playbackSpeed);
        _historyAudioPlayer.SetPlaybackSpeed(_playbackSpeed);
        UpdateSpeedButtonsUI();
        UpdateHistorySpeedUI();

        _leftPanelWidth = AppSettingsService.Defaults.LeftPanelWidth;
        LeftPanel.WidthRequest = _leftPanelWidth;
        _transcriptFontSize = AppSettingsService.Defaults.TranscriptFontSize;

        // Reset paths in services
        LMKit.Global.Configuration.ModelStorageDirectory = _settingsService.ModelStorageDirectory;
        _audioRecorder.SetRecordingsDirectory(_settingsService.RecordingsDirectory);
        _historyService.SetHistoryDirectory(_settingsService.HistoryDirectory);

        UpdateSettingsUI();
        UpdateModelModeUI();
        RefreshTranscriptDisplay();
        RefreshHistoryList();

        ShowToast(L.Localize(StringKeys.SettingsResetToDefaults), ToastType.Success);
    }

    private void UpdateSettingsUI()
    {
        DarkModeToggle.IsToggled = _settingsService.DarkMode;
        VadToggle.IsToggled = _enableVoiceActivityDetection;
        AutoTranscribeToggle.IsToggled = _settingsService.AutoTranscribeOnImport;
        OpenAfterExportToggle.IsToggled = _settingsService.OpenFilesAfterExport;
        DictationFormattingToggle.IsToggled = _settingsService.EnableDictationFormatting;
        VolumeSlider.Value = _settingsService.PlaybackVolume;
        HistoryVolumeSlider.Value = _settingsService.PlaybackVolume;

        // Initialize language picker
        InitializeLanguagePicker();

        // Update path labels
        ModelDirLabel.Text = _settingsService.ModelStorageDirectory;
        RecordingsDirLabel.Text = _settingsService.RecordingsDirectory;
        HistoryDirLabel.Text = _settingsService.HistoryDirectory;

        // Update inline auto-transcribe toggle
        UpdateAutoTranscribeInlineUI();

        // Update transcription language label in settings
        SettingsLanguageLabel.Text = WhisperLanguages.GetDisplayNameFromCode(_settingsService.TranscriptionLanguage);

        // Update transcription mode in settings
        UpdateSettingsModelModeUI();

        // Update resource usage level
        UpdateResourceUsageLevelUI();
    }
}
