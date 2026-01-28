using LynxTranscribe.Helpers;
using LynxTranscribe.Localization;
using LynxTranscribe.Services;
using L = LynxTranscribe.Localization.LocalizationService;

namespace LynxTranscribe;

/// <summary>
/// UI-related functionality: Drag/drop, toast notifications, font size, hover effects, lifecycle, splitter
/// </summary>
public partial class MainPage
{
    #region Lifecycle

    protected override bool OnBackButtonPressed()
    {
        if (SettingsOverlay.IsVisible)
        {
            SettingsOverlay.IsVisible = false;
            return true;
        }
        return base.OnBackButtonPressed();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        StartPeriodicWaveformAnimation();

        // Subscribe to language changes
        L.Instance.LanguageChanged += OnLanguageChanged;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        L.Instance.LanguageChanged -= OnLanguageChanged;
        PrepareForClose();
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(RefreshLocalizedStrings);
    }

    /// <summary>
    /// Refreshes all localizable UI strings when language changes or on startup.
    /// </summary>
    private void RefreshLocalizedStrings()
    {
        // Navigation tabs
        FileTabLabel.Text = L.Localize(StringKeys.Audio);
        HistoryTabLabel.Text = L.Localize(StringKeys.History);

        // Mode buttons
        TurboButtonLabel.Text = L.Localize(StringKeys.Turbo);
        AccurateButtonLabel.Text = L.Localize(StringKeys.Accurate);

        // Main actions
        TranscribeButtonText.Text = L.Localize(StringKeys.Transcribe);
        CancelTranscriptionText.Text = L.Localize(StringKeys.Cancel);
        if (OverlayCancelText != null)
        {
            OverlayCancelText.Text = L.Localize(StringKeys.Cancel);
        }

        BrowseButtonLabel.Text = L.Localize(StringKeys.Browse);
        RecordButtonLabel.Text = L.Localize(StringKeys.Record);
        ClearButtonLabel.Text = L.Localize(StringKeys.Clear);

        // Drop zone
        DropZoneTitle.Text = L.Localize(StringKeys.DropAudioFileHere);
        DropZoneSubtitle.Text = L.Localize(StringKeys.OrBrowseFilesRecordAudio);

        // Recording
        GetReadyLabel.Text = L.Localize(StringKeys.GetReady);
        ClickToSkipLabel.Text = L.Localize(StringKeys.ClickToSkip);
        RecordingLabel.Text = L.Localize(StringKeys.Recording);
        StopRecordingLabel.Text = L.Localize(StringKeys.StopRecording);

        // History panel
        NoHistoryTitle.Text = L.Localize(StringKeys.NoHistoryYet);
        NoHistorySubtitle.Text = L.Localize(StringKeys.TranscriptionsWillAppearHere);
        ClearAllLabel.Text = L.Localize(StringKeys.ClearAll);
        HistorySearchEntry.Placeholder = L.Localize(StringKeys.SearchHistory);

        // Transcript panel
        TranscriptHeaderLabel.Text = L.Localize(StringKeys.Transcript);
        SearchButtonLabel.Text = L.Localize(StringKeys.Search);
        ToolTipProperties.SetText(SearchButton, L.Localize(StringKeys.SearchInTranscript));
        SearchEntry.Placeholder = L.Localize(StringKeys.FindInTranscript);
        EditToggleLabel.Text = _isEditMode ? L.Localize(StringKeys.Editing) : L.Localize(StringKeys.Edit);
        CopyButtonLabel.Text = L.Localize(StringKeys.Copy);
        ExportButtonLabel.Text = L.Localize(StringKeys.Export);

        // Stats badges
        VadLabel.Text = L.Localize(StringKeys.VAD);

        // Empty state
        EmptyStateTitle.Text = L.Localize(StringKeys.ReadyToTranscribe);
        EmptyStateSubtitle.Text = L.Localize(StringKeys.ConvertSpeechToText);
        Step1Text.Text = L.Localize(StringKeys.SelectOrDropAudioFile);
        Step2Text.Text = L.Localize(StringKeys.ClickTranscribeToStart);
        SupportedFormatsLabel.Text = L.Localize(StringKeys.SupportedFormats);

        // Export panel
        ExportTranscriptTitle.Text = L.Localize(StringKeys.ExportTranscript);
        PlainTextLabel.Text = L.Localize(StringKeys.PlainText);
        PlainTextDescLabel.Text = L.Localize(StringKeys.PlainTextDescription);
        WordDocLabel.Text = L.Localize(StringKeys.WordDocument);
        WordDocDescLabel.Text = L.Localize(StringKeys.WordDocumentDescription);
        RichTextLabel.Text = L.Localize(StringKeys.RichText);
        RichTextDescLabel.Text = L.Localize(StringKeys.RichTextDescription);
        SrtLabel.Text = L.Localize(StringKeys.SrtSubtitles);
        SrtDescLabel.Text = L.Localize(StringKeys.SrtSubtitlesDescription);
        VttLabel.Text = L.Localize(StringKeys.WebVttSubtitles);
        VttDescLabel.Text = L.Localize(StringKeys.WebVttSubtitlesDescription);
        OpenFileAfterExportLabel.Text = L.Localize(StringKeys.OpenFileAfterExport);
        ExportDictationLabel.Text = L.Localize(StringKeys.ApplyDictationFormatting);

        // Settings panel
        SettingsTitle.Text = L.Localize(StringKeys.Settings);
        DarkModeLabel.Text = L.Localize(StringKeys.DarkMode);
        DarkModeDescription.Text = L.Localize(StringKeys.DarkModeDescription);
        LanguageLabel.Text = L.Localize(StringKeys.Language);
        LanguageDescription.Text = L.Localize(StringKeys.LanguageDescription);
        VadSettingLabel.Text = L.Localize(StringKeys.VoiceActivityDetection);
        VadSettingDescription.Text = L.Localize(StringKeys.VadDescription);
        AutoTranscribeLabel.Text = L.Localize(StringKeys.AutoTranscribeOnImport);
        AutoTranscribeDescription.Text = L.Localize(StringKeys.AutoTranscribeOnImportDescription);
        AutoTranscribeInlineLabel.Text = L.Localize(StringKeys.AutoTranscribeOnImport);
        TranscriptionLanguageSettingLabel.Text = L.Localize(StringKeys.TranscriptionLanguageSetting);
        TranscriptionLanguageSettingDescription.Text = L.Localize(StringKeys.TranscriptionLanguageSettingDescription);
        TranscriptionModeSettingLabel.Text = L.Localize(StringKeys.TranscriptionModeSetting);
        TranscriptionModeSettingDescription.Text = L.Localize(StringKeys.TranscriptionModeSettingDescription);
        SettingsTurboLabel.Text = L.Localize(StringKeys.Turbo);
        SettingsAccurateLabel.Text = L.Localize(StringKeys.Accurate);
        OpenAfterExportLabel.Text = L.Localize(StringKeys.OpenFilesAfterExport);
        OpenAfterExportDescription.Text = L.Localize(StringKeys.OpenFilesAfterExportDescription);
        DictationFormattingLabel.Text = L.Localize(StringKeys.DictationFormatting);
        DictationFormattingDescription.Text = L.Localize(StringKeys.DictationFormattingDescription);
        DictationHelpTitle.Text = L.Localize(StringKeys.DictationCommands);
        DictationHelpSubtitle.Text = L.Localize(StringKeys.DictationCommandsSubtitle);
        DictationToggleLabel.Text = L.Localize(StringKeys.Dictation);
        ToolTipProperties.SetText(DictationToggle, L.Localize(StringKeys.DictationTooltip));
        ToolTipProperties.SetText(DictationHelpToolbarButton, L.Localize(StringKeys.ShowDictationCommands));
        AudioInputLabel.Text = L.Localize(StringKeys.AudioInput);
        AudioInputDescription.Text = L.Localize(StringKeys.AudioInputDescription);
        PerformanceSettingsLabel.Text = L.Localize(StringKeys.PerformanceSettings);
        ResourceUsageLabel.Text = L.Localize(StringKeys.ResourceUsage);
        ResourceUsageDescription.Text = L.Localize(StringKeys.ResourceUsageDescription);
        ResourceLevel1Label.Text = L.Localize(StringKeys.ResourceLevelLight);
        ResourceLevel2Label.Text = L.Localize(StringKeys.ResourceLevelBalanced);
        ResourceLevel3Label.Text = L.Localize(StringKeys.ResourceLevelPerformance);
        ResourceLevel4Label.Text = L.Localize(StringKeys.ResourceLevelMaximum);
        StorageLocationsLabel.Text = L.Localize(StringKeys.StorageLocations);
        GeneralSettingsHeader.Text = L.Localize(StringKeys.GeneralSettings);
        TranscriptionSettingsHeader.Text = L.Localize(StringKeys.TranscriptionSettings);
        EscHintLabel.Text = L.Localize(StringKeys.PressEscToClose);
        VersionLabel.Text = $"{AppConstants.AppName} v{AppConstants.Version}";
        FooterAppNameLabel.Text = AppConstants.AppName;
        FooterVersionLabel.Text = $"v{AppConstants.Version}";
        ModelDirTitle.Text = L.Localize(StringKeys.ModelDirectory);
        ModelDirDescription.Text = L.Localize(StringKeys.ModelDirectoryDescription);
        OpenModelDirLabel.Text = L.Localize(StringKeys.Open);
        ChangeModelDirLabel.Text = L.Localize(StringKeys.Change);
        RecordingsDirTitle.Text = L.Localize(StringKeys.RecordingsDirectory);
        RecordingsDirDescription.Text = L.Localize(StringKeys.RecordingsDirectoryDescription);
        OpenRecordingsDirLabel.Text = L.Localize(StringKeys.Open);
        ChangeRecordingsDirLabel.Text = L.Localize(StringKeys.Change);
        HistoryDirTitle.Text = L.Localize(StringKeys.HistoryDirectory);
        HistoryDirDescription.Text = L.Localize(StringKeys.HistoryDirectoryDescription);
        OpenHistoryDirLabel.Text = L.Localize(StringKeys.Open);
        ChangeHistoryDirLabel.Text = L.Localize(StringKeys.Change);
        ResetButtonLabel.Text = L.Localize(StringKeys.ResetToDefaults);

        // Segment tooltip labels
        TooltipDurationLabel.Text = L.Localize(StringKeys.Duration);
        TooltipLanguageLabel.Text = L.Localize(StringKeys.SegmentLanguage);
        TooltipConfidenceLabel.Text = L.Localize(StringKeys.Confidence);

        // VAD badge tooltip
        ToolTipProperties.SetText(VadBadge, L.Localize(StringKeys.VadBadgeTooltip));

        // Action button tooltips
        ToolTipProperties.SetText(CopyButton, L.Localize(StringKeys.CopyToClipboard));
        ToolTipProperties.SetText(SaveButton, L.Localize(StringKeys.ExportTooltip));
        ToolTipProperties.SetText(BrowseAudioFileButton, L.Localize(StringKeys.OpenFileLocation));
        ToolTipProperties.SetText(RestartButton, L.Localize(StringKeys.RestartPlayback));

        // Status bar
        VadStatusLabel.Text = _enableVoiceActivityDetection ? L.Localize(StringKeys.On) : L.Localize(StringKeys.Off);
        ToolTipProperties.SetText(VadStatusContainer, L.Localize(StringKeys.VadTooltip));

        // Model mode badge - refresh label and tooltip based on current mode setting
        if (!string.IsNullOrEmpty(ModelModeLabel.Text))
        {
            ModelModeLabel.Text = _useAccurateMode ? L.Localize(StringKeys.Accurate) : L.Localize(StringKeys.Turbo);
            UpdateModelModeTooltip(_useAccurateMode);
        }

        // Theme toggle tooltip
        UpdateThemeToggleUI(_settingsService.DarkMode);
    }

    private void OnWindowDestroying(object? sender, EventArgs e)
    {
        PrepareForClose();
    }

    private void PrepareForClose()
    {
        // Set closing flag FIRST to stop all async operations
        _isClosing = true;

        // Stop all timers immediately
        StopWaveformAnimation();
        StopTranscribeButtonPulse();
        _playbackTimer?.Stop();
        _historyPlaybackTimer?.Stop();
        _recordingDotTimer?.Stop();

        // Cancel all MAUI animations on specific elements
        try
        {
            WaveformIcon?.CancelAnimations();
            WaveBar1?.CancelAnimations();
            WaveBar2?.CancelAnimations();
            WaveBar3?.CancelAnimations();
            WaveBar4?.CancelAnimations();
            WaveBar5?.CancelAnimations();
            WaveBar6?.CancelAnimations();
            CountdownLabel?.CancelAnimations();
            ToastContainer?.CancelAnimations();
            FontSizeControls?.CancelAnimations();
        }
        catch { }

        // Stop audio
        try { _audioPlayer?.Stop(); } catch { }
        try { _historyAudioPlayer?.Stop(); } catch { }
        try { _audioRecorder?.StopRecording(); } catch { }

        // Cancel any pending operations
        try { _cancellationTokenSource?.Cancel(); } catch { }
        try { _toastCts?.Cancel(); } catch { }
        _tooltipGeneration++;
        _tooltipVisible = false;
        _tooltipReady = false;

        // Dispose LM-Kit resources
        try { _lmKitService?.Dispose(); } catch { }

        // Clear history cache to release memory
        try { ClearHistoryCache(); } catch { }
    }

    private void StartPeriodicWaveformAnimation()
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            if (_isClosing)
            {
                return;
            }

            await Task.Delay(500);
            if (_isClosing)
            {
                return;
            }

            await AnimateWaveformBars();
        });

        _waveformAnimationTimer = Dispatcher.CreateTimer();
        _waveformAnimationTimer.Interval = TimeSpan.FromSeconds(10);
        _waveformAnimationTimer.Tick += async (s, e) =>
        {
            if (_isClosing)
            {
                return;
            }

            if (EmptyState.IsVisible && !_isRecording && !_isTranscribing)
            {
                await AnimateWaveformBars();
            }
        };
        _waveformAnimationTimer.Start();
    }

    private void StopWaveformAnimation()
    {
        _waveformAnimationTimer?.Stop();
    }

    private async Task AnimateWaveformBars()
    {
        if (_isWaveformAnimating || _isClosing)
        {
            return;
        }

        _isWaveformAnimating = true;

        try
        {
            if (_isClosing)
            {
                return;
            }

            var bars = new[] { WaveBar1, WaveBar2, WaveBar3, WaveBar4, WaveBar5, WaveBar6 };
            var originalHeights = new double[] { 8, 16, 24, 14, 20, 10 };
            var random = new Random();

            SafeAnimate(() => WaveformIcon.ScaleToAsync(1.1, 200, Easing.CubicOut));
            if (_isClosing)
            {
                return;
            }

            await Task.Delay(200);

            for (int cycle = 0; cycle < 6 && !_isClosing; cycle++)
            {
                for (int i = 0; i < bars.Length && !_isClosing; i++)
                {
                    var idx = i;
                    var targetHeight = 6 + random.Next(18);
                    SafeAnimate(() => bars[idx].ScaleYToAsync(targetHeight / originalHeights[idx], 200, Easing.CubicInOut));
                }
                if (_isClosing)
                {
                    return;
                }

                await Task.Delay(250);
            }

            if (!_isClosing)
            {
                for (int i = 0; i < bars.Length; i++)
                {
                    var idx = i;
                    SafeAnimate(() => bars[idx].ScaleYToAsync(1.0, 300, Easing.CubicOut));
                }

                SafeAnimate(() => WaveformIcon.ScaleToAsync(1.0, 300, Easing.CubicOut));
                await Task.Delay(300);
            }
        }
        catch (ObjectDisposedException) { }
        catch (Exception) { }
        finally
        {
            _isWaveformAnimating = false;
            if (!_isClosing)
            {
                try
                {
                    WaveBar1.ScaleY = 1; WaveBar2.ScaleY = 1; WaveBar3.ScaleY = 1;
                    WaveBar4.ScaleY = 1; WaveBar5.ScaleY = 1; WaveBar6.ScaleY = 1;
                    WaveformIcon.Scale = 1;
                }
                catch { }
            }
        }
    }

    /// <summary>
    /// Safely starts an animation, catching disposal exceptions during app close.
    /// </summary>
    private void SafeAnimate(Func<Task> animationAction)
    {
        if (_isClosing)
        {
            return;
        }

        try
        {
            _ = animationAction();
        }
        catch (ObjectDisposedException) { }
        catch (InvalidOperationException) { }
        catch { }
    }

    #endregion

    #region Drag & Drop

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        e.AcceptedOperation = DataPackageOperation.Copy;
        var accentSurface = (Color)Resources["AccentSurface"]!;
        var accentPrimary = (Color)Resources["AccentPrimary"]!;
        DropZone.BackgroundColor = accentSurface;
        DropZone.Stroke = accentPrimary;
    }

    private void OnDragLeave(object? sender, DragEventArgs e)
    {
        if (_selectedFilePath == null)
        {
            var backgroundSecondary = (Color)Resources["BackgroundSecondary"]!;
            var surfaceBorder = (Color)Resources["SurfaceBorder"]!;
            DropZone.BackgroundColor = backgroundSecondary;
            DropZone.Stroke = surfaceBorder;
        }
    }

    private async void OnDrop(object? sender, DropEventArgs e)
    {
        if (_selectedFilePath == null)
        {
            var backgroundSecondary = (Color)Resources["BackgroundSecondary"]!;
            var surfaceBorder = (Color)Resources["SurfaceBorder"]!;
            DropZone.BackgroundColor = backgroundSecondary;
            DropZone.Stroke = surfaceBorder;
        }

        try
        {
            string? filePath = null;

#if WINDOWS
            if (e.PlatformArgs?.DragEventArgs?.DataView is Windows.ApplicationModel.DataTransfer.DataPackageView dataView)
            {
                if (dataView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.StorageItems))
                {
                    var items = await dataView.GetStorageItemsAsync();
                    if (items.Count > 0 && items[0] is Windows.Storage.StorageFile file)
                    {
                        filePath = file.Path;
                    }
                }
            }
#else
            var data = e.Data;
            if (data.Properties != null)
            {
                foreach (var key in data.Properties.Keys)
                {
                    var value = data.Properties[key];
                    if (value is IEnumerable<string> paths)
                    {
                        filePath = paths.FirstOrDefault();
                        if (!string.IsNullOrEmpty(filePath))
                        {
                            break;
                        }
                    }
                    if (value is string strPath && File.Exists(strPath))
                    {
                        filePath = strPath;
                        break;
                    }
                }
            }
            if (string.IsNullOrEmpty(filePath))
            {
                try
                {
                    var text = await data.GetTextAsync();
                    if (!string.IsNullOrEmpty(text) && File.Exists(text))
                    {
                        filePath = text;
                    }
                }
                catch { }
            }
#endif

            if (!string.IsNullOrEmpty(filePath))
            {
                await LoadDroppedFile(filePath);
            }
        }
        catch { }
    }

    private async Task LoadDroppedFile(string filePath)
    {
        if (!AppConstants.IsSupportedAudioFile(filePath))
        {
            var ext = System.IO.Path.GetExtension(filePath).ToLowerInvariant();
            ShowError("Unsupported Format", $"The file format '{ext}' is not supported.\n\nSupported formats: WAV, MP3, FLAC, OGG, M4A, WMA");
            return;
        }

        _selectedFilePath = filePath;
        // DO NOT clear _currentRecordId or _currentSegments - keep history state intact

        // Show loading state immediately
        var fileName = System.IO.Path.GetFileName(filePath);
        var fileInfo = new FileInfo(filePath);
        SelectedFileName.Text = fileName;
        SelectedFileInfo.Text = $"{FormatFileSize(fileInfo.Length)} · Loading...";

        EmptyState.IsVisible = false;
        FileSelectedState.IsVisible = true;
        ClearButton.IsVisible = true;

        // DO NOT touch transcript UI - it's managed by tab switching
        // Close search if open
        if (SearchBar.IsVisible)
        {
            CloseSearch();
        }

        // Update file selection dependent UI (drop zone, step badges)
        UpdateFileSelectionUI();

        // Run heavy operations in parallel on background threads
        var loadAudioTask = LoadAudioFile(filePath);
        var loadPlayerTask = _audioPlayer.LoadAsync(filePath);
        var waveformTask = GenerateWaveformAsync(filePath);

        // Wait for LMKit audio to load (for duration info)
        await loadAudioTask;

        // Update duration display
        var durationText = _lmKitService.LoadedAudio != null ? $" · {_lmKitService.LoadedAudio.Duration:mm\\:ss}" : "";
        SelectedFileInfo.Text = $"{FormatFileSize(fileInfo.Length)}{durationText}";

        // Wait for player to be ready
        if (await loadPlayerTask)
        {
            PlayerPanel.IsVisible = true;
            TotalTimeLabel.Text = TranscriptExporter.FormatDisplayTime(_audioPlayer.TotalDuration);
            CurrentTimeLabel.Text = "0:00";
            PlaybackSlider.Value = 0;
            PlayPauseIcon.Text = "▶";
        }

        // Wait for waveform (may already be done)
        await waveformTask;

        UpdateTranscribeButtonState();

        // Auto-transcribe if enabled
        if (_settingsService.AutoTranscribeOnImport && _lmKitService.HasLoadedAudio && !_isTranscribing)
        {
            OnTranscribeClicked(null, EventArgs.Empty);
        }
    }

    #endregion

    #region Segment Tooltip

    private Border? _hoveredSegmentBorder;
    private int _hoveredSegmentIndex = -1;
    private int _tooltipGeneration = 0;
    private bool _tooltipVisible = false;
    private bool _tooltipReady = false;  // Content ready, waiting for mouse move to show

    private async void OnSegmentPointerEntered(Border border, int index, PointerEventArgs e)
    {
        if (_isClosing)
        {
            return;
        }

        _hoveredSegmentBorder = border;
        _hoveredSegmentIndex = index;
        _tooltipReady = false;

        // Cancel any pending tooltip by incrementing generation
        _tooltipGeneration++;
        var myGeneration = _tooltipGeneration;

        // Wait 1 second in small increments to allow cancellation without exceptions
        for (int i = 0; i < 10; i++)
        {
            await Task.Delay(100);
            if (_isClosing || _tooltipGeneration != myGeneration || _hoveredSegmentIndex != index)
            {
                return;
            }
        }

        // Prepare tooltip content but don't show yet - wait for mouse move
        PrepareSegmentTooltip(index);
        _tooltipReady = true;
    }

    private void OnSegmentPointerMoved(Border border, int index, PointerEventArgs e)
    {
        if (_isClosing)
        {
            return;
        }

        try
        {
            var position = e.GetPosition(this);
            if (!position.HasValue)
            {
                return;
            }

            // If tooltip is ready but not visible, show it now at current position
            if (_tooltipReady && !_tooltipVisible)
            {
                SegmentTooltip.TranslationX = position.Value.X;
                SegmentTooltip.TranslationY = position.Value.Y - 120;
                SegmentTooltip.IsVisible = true;
                _tooltipVisible = true;
                _tooltipReady = false;
            }
            // If tooltip is visible, follow mouse
            else if (_tooltipVisible)
            {
                SegmentTooltip.TranslationX = position.Value.X;
                SegmentTooltip.TranslationY = position.Value.Y - 120;
            }
        }
        catch { }
    }

    private void OnSegmentPointerExited()
    {
        _tooltipGeneration++;  // Cancel any pending tooltip
        SegmentTooltip.IsVisible = false;
        _tooltipVisible = false;
        _tooltipReady = false;
        _hoveredSegmentBorder = null;
        _hoveredSegmentIndex = -1;
    }

    private void PrepareSegmentTooltip(int segmentIndex)
    {
        if (_currentSegments == null || segmentIndex < 0 || segmentIndex >= _currentSegments.Count)
        {
            return;
        }

        var segment = _currentSegments[segmentIndex];
        var duration = segment.End - segment.Start;

        // Get confidence - it may be 0-1 or 0-100 depending on source
        var confidenceRaw = segment.Confidence;
        var confidence = confidenceRaw > 1 ? confidenceRaw : confidenceRaw * 100;

        // Format values
        var durationStr = duration.TotalSeconds < 1
            ? $"{duration.TotalMilliseconds:F0}ms"
            : $"{duration.TotalSeconds:F1}s";
        var langStr = !string.IsNullOrEmpty(segment.Language) ? segment.Language.ToUpper() : "N/A";
        var confidenceStr = confidence > 0 ? $"{confidence:F0}%" : "N/A";
        var timeRange = $"{FormatTimestamp(segment.Start)} → {FormatTimestamp(segment.End)}";

        // Update tooltip content
        TooltipTimeRange.Text = timeRange;
        TooltipDuration.Text = durationStr;
        TooltipLanguage.Text = langStr;
        TooltipConfidence.Text = confidenceStr;

        // Set confidence indicator color
        var confidenceColor = confidence >= 80 ? "#22c55e" :  // Green
                              confidence >= 60 ? "#eab308" :  // Yellow
                              confidence >= 40 ? "#f97316" :  // Orange
                              confidence > 0 ? "#ef4444" :  // Red
                                                 "#71717a";   // Gray for N/A
        TooltipConfidenceIcon.TextColor = Color.FromArgb(confidenceColor);
        TooltipConfidenceIcon.IsVisible = confidence > 0;
    }

    #endregion

    #region Toast Notifications

    private enum ToastType { Info, Success, Warning, Error }

    private async void ShowToast(string message, ToastType type = ToastType.Info)
    {
        if (_isClosing)
        {
            return;
        }

        _toastCts?.Cancel();
        _toastCts = new CancellationTokenSource();
        var token = _toastCts.Token;

        var (bgColor, iconColor, icon) = type switch
        {
            ToastType.Success => (
                Color.FromArgb("#1a2e1a"),  // Dark green surface
                Color.FromArgb("#4ade80"),  // Bright green icon
                "✓"),
            ToastType.Warning => (
                Color.FromArgb("#2e2a1a"),  // Dark amber surface
                Color.FromArgb("#fbbf24"),  // Bright amber icon
                "⚠"),
            ToastType.Error => (
                Color.FromArgb("#2e1a1a"),  // Dark red surface
                Color.FromArgb("#f87171"),  // Bright red icon
                "✕"),
            _ => (
                Color.FromArgb("#1e2433"),  // Dark blue-gray surface
                Color.FromArgb("#94a3b8"),  // Muted icon
                "ℹ")
        };

        try
        {
            ToastContainer.BackgroundColor = bgColor;
            ToastIcon.Text = icon;
            ToastIcon.TextColor = iconColor;
            ToastMessage.TextColor = Color.FromArgb("#e2e8f0");  // Light text
            ToastMessage.Text = message;

            ToastContainer.Opacity = 0;
            ToastContainer.TranslationY = 30;
            ToastContainer.IsVisible = true;

            if (_isClosing)
            {
                return;
            }

            // Slide up and fade in
            await Task.WhenAll(
                ToastContainer.FadeToAsync(1, 250),
                ToastContainer.TranslateToAsync(0, 0, 250, Easing.CubicOut)
            );

            if (_isClosing)
            {
                return;
            }

            await Task.Delay(3000, token);
            if (!token.IsCancellationRequested && !_isClosing)
            {
                // Slide down and fade out
                await Task.WhenAll(
                    ToastContainer.FadeToAsync(0, 200),
                    ToastContainer.TranslateToAsync(0, 30, 200, Easing.CubicIn)
                );
                ToastContainer.IsVisible = false;
            }
        }
        catch (TaskCanceledException) { }
        catch (ObjectDisposedException) { }
    }

    #endregion

    #region Transcript Font Size

    private void OnFontDecreaseClicked(object? sender, TappedEventArgs e)
    {
        if (_transcriptFontSize > MinFontSize)
        {
            _transcriptFontSize -= 1;
            UpdateSegmentFontSize();
            _settingsService.TranscriptFontSize = _transcriptFontSize;
        }
    }

    private void OnFontIncreaseClicked(object? sender, TappedEventArgs e)
    {
        if (_transcriptFontSize < MaxFontSize)
        {
            _transcriptFontSize += 1;
            UpdateSegmentFontSize();
            _settingsService.TranscriptFontSize = _transcriptFontSize;
        }
    }

    private void OnFontControlHoverEnter(object? sender, PointerEventArgs e)
    {
        SafeAnimate(() => FontSizeControls.FadeToAsync(0.9, 150));
    }

    private void OnFontControlHoverExit(object? sender, PointerEventArgs e)
    {
        SafeAnimate(() => FontSizeControls.FadeToAsync(0, 300));
    }

    private void OnFontButtonHoverEnter(object? sender, PointerEventArgs e)
    {
        if (sender is Border button)
        {
            button.BackgroundColor = (Color)Resources["AccentSurface"]!;
        }
    }

    private void OnFontButtonHoverExit(object? sender, PointerEventArgs e)
    {
        if (sender is Border button)
        {
            button.BackgroundColor = Colors.Transparent;
        }
    }

    #endregion

    #region Splitter/Resize

    private DateTime _lastSplitterUpdate = DateTime.MinValue;
    private const int SplitterThrottleMs = 32; // ~30fps during drag for smoother performance
    private double _lastAppliedWidth = 0;

    private void OnSplitterPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        // Don't allow resizing when panel is collapsed
        if (_isLeftPanelCollapsed)
        {
            return;
        }

        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _isDraggingSplitter = true;
                _dragStartX = e.TotalX;
                _dragStartWidth = _leftPanelWidth;
                _lastAppliedWidth = _leftPanelWidth;
                break;
            case GestureStatus.Running:
                if (_isDraggingSplitter)
                {
                    var newWidth = _dragStartWidth + e.TotalX;
                    newWidth = Math.Clamp(newWidth, MinPanelWidth, MaxPanelWidth);

                    // Only update if width changed significantly (reduces layout thrashing)
                    if (Math.Abs(newWidth - _lastAppliedWidth) >= 2)
                    {
                        _leftPanelWidth = newWidth;
                        _lastAppliedWidth = newWidth;
                        LeftPanel.WidthRequest = newWidth;

                        // Throttle history item scaling updates more aggressively
                        var now = DateTime.Now;
                        if ((now - _lastSplitterUpdate).TotalMilliseconds >= SplitterThrottleMs)
                        {
                            _lastSplitterUpdate = now;
                            UpdateHistoryItemVisibility(newWidth);
                        }
                    }
                }
                break;
            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                _isDraggingSplitter = false;
                _settingsService.LeftPanelWidth = _leftPanelWidth;
                // Final full update on release
                UpdateHistoryItemScale(_leftPanelWidth);
                break;
        }
    }

    /// <summary>
    /// Lightweight update during drag - only toggles visibility, no font scaling
    /// </summary>
    private void UpdateHistoryItemVisibility(double panelWidth)
    {
        var showPreview = panelWidth >= 300;
        var showMeta = panelWidth >= 280;

        // Hide history count badge when panel is narrow to prevent overlap
        var showHistoryCount = panelWidth >= 290;
        if (HistoryCountBadge.IsVisible != showHistoryCount && _historyService.GetAll().Count > 0)
        {
            // Only toggle if there are items (badge should stay hidden if count is 0)
            HistoryCountBadge.IsVisible = showHistoryCount;
        }

        foreach (var child in HistoryList.Children)
        {
            if (child is Border border && border.Content is Grid grid)
            {
                foreach (var gridChild in grid.Children)
                {
                    if (gridChild is Label label && label.ClassId == "preview")
                    {
                        label.IsVisible = showPreview;
                    }
                    else if (gridChild is HorizontalStackLayout stack && stack.ClassId == "metaRow")
                    {
                        stack.IsVisible = showMeta;
                    }
                }
            }
        }
    }

    private void UpdateHistoryItemScale(double panelWidth)
    {
        // Update history count badge visibility based on panel width
        UpdateHistoryBadge();

        // Calculate font scale factor: 260px = 0.85, 340px = 1.0, 500px = 1.15
        var fontScale = 0.85 + ((panelWidth - MinPanelWidth) / (MaxPanelWidth - MinPanelWidth) * 0.30);
        fontScale = Math.Clamp(fontScale, 0.85, 1.15);

        var baseTitleSize = 12.0;
        var basePreviewSize = 11.0;
        var baseMetaSize = 10.0;
        var baseIconSize = 14.0;

        var showPreview = panelWidth >= 300;
        var showMeta = panelWidth >= 280;

        foreach (var child in HistoryList.Children)
        {
            if (child is Border border && border.Content is Grid grid)
            {
                foreach (var gridChild in grid.Children)
                {
                    if (gridChild is Label label)
                    {
                        if (label.ClassId == "title")
                        {
                            label.FontSize = baseTitleSize * fontScale;
                        }
                        else if (label.ClassId == "preview")
                        {
                            label.IsVisible = showPreview;
                            label.FontSize = basePreviewSize * fontScale;
                        }
                        else if (label.ClassId == "icon")
                        {
                            label.FontSize = baseIconSize * fontScale;
                        }
                    }
                    else if (gridChild is HorizontalStackLayout stack)
                    {
                        if (stack.ClassId == "metaRow")
                        {
                            stack.IsVisible = showMeta;
                            foreach (var stackChild in stack.Children)
                            {
                                if (stackChild is Label metaLabel && metaLabel.ClassId == "meta")
                                {
                                    metaLabel.FontSize = baseMetaSize * fontScale;
                                }
                                else if (stackChild is HorizontalStackLayout iconsStack && iconsStack.ClassId == "iconsRow")
                                {
                                    foreach (var iconChild in iconsStack.Children)
                                    {
                                        if (iconChild is Label iconLabel && iconLabel.ClassId == "icon")
                                        {
                                            iconLabel.FontSize = baseIconSize * fontScale;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    #endregion

    #region Hover Effects

    private void OnSettingsButtonHoverEnter(object? sender, PointerEventArgs e)
    {
        var accentSurface = (Color)Resources["AccentSurface"]!;
        var accentMuted = (Color)Resources["AccentMuted"]!;
        var accentText = (Color)Resources["AccentText"]!;

        SettingsButtonBorder.BackgroundColor = accentSurface;
        SettingsButtonBorder.Stroke = accentMuted;

        // Change sliders icon to accent color on hover
        SettingsLine1.Stroke = new SolidColorBrush(accentText);
        SettingsLine2.Stroke = new SolidColorBrush(accentText);
        SettingsLine3.Stroke = new SolidColorBrush(accentText);
        SettingsDot1.Fill = new SolidColorBrush(accentText);
        SettingsDot2.Fill = new SolidColorBrush(accentText);
        SettingsDot3.Fill = new SolidColorBrush(accentText);
    }

    private void OnSettingsButtonHoverExit(object? sender, PointerEventArgs e)
    {
        var backgroundTertiary = (Color)Resources["BackgroundTertiary"]!;
        var surfaceBorder = (Color)Resources["SurfaceBorder"]!;
        var textSecondary = (Color)Resources["TextSecondary"]!;

        SettingsButtonBorder.BackgroundColor = backgroundTertiary;
        SettingsButtonBorder.Stroke = surfaceBorder;

        // Restore sliders icon to neutral color
        SettingsLine1.Stroke = new SolidColorBrush(textSecondary);
        SettingsLine2.Stroke = new SolidColorBrush(textSecondary);
        SettingsLine3.Stroke = new SolidColorBrush(textSecondary);
        SettingsDot1.Fill = new SolidColorBrush(textSecondary);
        SettingsDot2.Fill = new SolidColorBrush(textSecondary);
        SettingsDot3.Fill = new SolidColorBrush(textSecondary);
    }

    private void OnBrowseHoverEnter(object? sender, PointerEventArgs e)
    {
        ApplyStyle(BrowseButton, ControlStyle.ButtonAccentHover);
    }

    private void OnBrowseHoverExit(object? sender, PointerEventArgs e)
    {
        ApplyStyle(BrowseButton, ControlStyle.ButtonAccent);
    }

    private void OnClearHoverEnter(object? sender, PointerEventArgs e)
    {
        ClearButton.BackgroundColor = (Color)Resources["DangerSurface"]!;
    }

    private void OnClearHoverExit(object? sender, PointerEventArgs e)
    {
        ApplyStyle(ClearButton, ControlStyle.ButtonTransparent);
    }

    private void OnTranscribeHoverEnter(object? sender, PointerEventArgs e)
    {
        // Brighter background on hover
        TranscribeButton.BackgroundColor = Color.FromArgb("#FFB830");
        TranscribeButton.Scale = 1.02;
    }

    private void OnTranscribeHoverExit(object? sender, PointerEventArgs e)
    {
        // Back to accent color
        TranscribeButton.BackgroundColor = (Color)Resources["AccentPrimary"]!;
        TranscribeButton.Scale = 1.0;
    }

    private void OnCopyHoverEnter(object? sender, PointerEventArgs e)
    {
        if (CopyButtonLabel.Text != "Copied!")
        {
            ApplyStyle(CopyButton, ControlStyle.ButtonHover, CopyButtonLabel);
        }
    }

    private void OnCopyHoverExit(object? sender, PointerEventArgs e)
    {
        if (CopyButtonLabel.Text != "Copied!")
        {
            ApplyStyle(CopyButton, ControlStyle.ButtonDefault, CopyButtonLabel);
        }
    }

    private void OnSaveHoverEnter(object? sender, PointerEventArgs e)
    {
        ApplyStyle(SaveButton, ControlStyle.ButtonHover, null);
    }

    private void OnSaveHoverExit(object? sender, PointerEventArgs e)
    {
        ApplyStyle(SaveButton, ControlStyle.ButtonDefault, null);
    }

    private void OnFileTabHoverEnter(object? sender, PointerEventArgs e)
    {
        if (!_isFileTabActive)
        {
            // Stronger hover effect for dark mode - use AccentSurface with border highlight
            FileTabButton.BackgroundColor = (Color)Resources["AccentSurface"]!;
            FileTabButton.Stroke = (Color)Resources["AccentMuted"]!;
            FileTabLabel.TextColor = (Color)Resources["AccentText"]!;
        }
    }

    private void OnFileTabHoverExit(object? sender, PointerEventArgs e)
    {
        if (!_isFileTabActive)
        {
            ApplyStyle(FileTabButton, ControlStyle.TabInactive, FileTabLabel);
        }
    }

    private void OnHistoryTabHoverEnter(object? sender, PointerEventArgs e)
    {
        if (_isFileTabActive)
        {
            // Stronger hover effect for dark mode - use AccentSurface with border highlight
            HistoryTabButton.BackgroundColor = (Color)Resources["AccentSurface"]!;
            HistoryTabButton.Stroke = (Color)Resources["AccentMuted"]!;
            HistoryTabLabel.TextColor = (Color)Resources["AccentText"]!;
        }
    }

    private void OnHistoryTabHoverExit(object? sender, PointerEventArgs e)
    {
        if (_isFileTabActive)
        {
            ApplyStyle(HistoryTabButton, ControlStyle.TabInactive, HistoryTabLabel);
        }
    }

    private void OnRecordHoverEnter(object? sender, PointerEventArgs e)
    {
        if (!_hasInputDevices || _isRecording)
        {
            return;
        }

        RecordButton.BackgroundColor = (Color)Resources["DangerSurface"]!;
    }

    private void OnRecordHoverExit(object? sender, PointerEventArgs e)
    {
        if (!_hasInputDevices || _isRecording)
        {
            return;
        }

        ApplyStyle(RecordButton, ControlStyle.ButtonTransparent);
    }

    private void OnAccurateHoverEnter(object? sender, PointerEventArgs e)
    {
        // Only show hover if Accurate is NOT currently selected
        if (!_useAccurateMode)
        {
            ApplyStyle(AccurateButton, ControlStyle.ModeHover, AccurateButtonLabel);
        }
    }

    private void OnAccurateHoverExit(object? sender, PointerEventArgs e)
    {
        // Only reset if Accurate is NOT currently selected
        if (!_useAccurateMode)
        {
            ApplyStyle(AccurateButton, ControlStyle.ModeUnselected, AccurateButtonLabel);
        }
    }

    private void OnTurboHoverEnter(object? sender, PointerEventArgs e)
    {
        // Only show hover if Turbo is NOT currently selected (meaning Accurate IS selected)
        if (_useAccurateMode)
        {
            ApplyStyle(TurboButton, ControlStyle.ModeHover, TurboButtonLabel);
        }
    }

    private void OnTurboHoverExit(object? sender, PointerEventArgs e)
    {
        // Only reset if Turbo is NOT currently selected (meaning Accurate IS selected)
        if (_useAccurateMode)
        {
            ApplyStyle(TurboButton, ControlStyle.ModeUnselected, TurboButtonLabel);
        }
    }

    private void OnClearHistoryHoverEnter(object? sender, PointerEventArgs e)
    {
        ClearHistoryButton.BackgroundColor = (Color)Resources["DangerSurface"]!;
    }

    private void OnClearHistoryHoverExit(object? sender, PointerEventArgs e)
    {
        ApplyStyle(ClearHistoryButton, ControlStyle.ButtonTransparent);
    }

    private void OnCloseSettingsHoverEnter(object? sender, PointerEventArgs e)
    {
        ApplyStyle(CloseSettingsButton, ControlStyle.ButtonTransparentHover);
    }

    private void OnCloseSettingsHoverExit(object? sender, PointerEventArgs e)
    {
        ApplyStyle(CloseSettingsButton, ControlStyle.ButtonTransparent);
    }

    private void OnPlayPauseHoverEnter(object? sender, PointerEventArgs e)
    {
        ApplyStyle(PlayPauseButton, ControlStyle.PlayButtonHover, PlayPauseIcon);
    }

    private void OnPlayPauseHoverExit(object? sender, PointerEventArgs e)
    {
        ApplyStyle(PlayPauseButton, ControlStyle.PlayButton, PlayPauseIcon);
    }

    private void OnRestartHoverEnter(object? sender, PointerEventArgs e)
    {
        RestartButton.BackgroundColor = (Color)Resources["SurfaceColor"]!;
        RestartButton.Stroke = (Color)Resources["AccentMuted"]!;
        RestartIcon.TextColor = (Color)Resources["AccentText"]!;
    }

    private void OnRestartHoverExit(object? sender, PointerEventArgs e)
    {
        RestartButton.BackgroundColor = (Color)Resources["BackgroundTertiary"]!;
        RestartButton.Stroke = (Color)Resources["SurfaceBorder"]!;
        RestartIcon.TextColor = (Color)Resources["TextSecondary"]!;
    }

    private void OnResetHoverEnter(object? sender, PointerEventArgs e)
    {
        ResetButtonLabel.TextColor = (Color)Resources["AccentPrimary"]!;
        ResetButtonLabel.TextDecorations = TextDecorations.Underline;
    }

    private void OnResetHoverExit(object? sender, PointerEventArgs e)
    {
        ResetButtonLabel.TextColor = (Color)Resources["TextMuted"]!;
        ResetButtonLabel.TextDecorations = TextDecorations.None;
    }

    private void OnSplitterHoverEnter(object? sender, PointerEventArgs e)
    {
        SplitterDots.Opacity = 1.0;
    }

    private void OnSplitterHoverExit(object? sender, PointerEventArgs e)
    {
        SplitterDots.Opacity = 0.6;
    }

    #endregion

    #region Panel Collapse

    private bool _isLeftPanelCollapsed = false;
    private double _leftPanelExpandedWidth = AppSettingsService.Defaults.LeftPanelWidth;

    private void OnCollapseButtonClicked(object? sender, TappedEventArgs e)
    {
        _leftPanelExpandedWidth = LeftPanel.WidthRequest > 0 ? LeftPanel.WidthRequest : _leftPanelWidth;
        LeftPanel.IsVisible = false;
        Splitter.IsVisible = false;  // Hide splitter when collapsed
        ExpandButton.IsVisible = true;
        _isLeftPanelCollapsed = true;
    }

    private void OnExpandButtonClicked(object? sender, TappedEventArgs e)
    {
        LeftPanel.IsVisible = true;
        LeftPanel.WidthRequest = _leftPanelExpandedWidth;
        Splitter.IsVisible = true;  // Show splitter when expanded
        ExpandButton.IsVisible = false;
        _isLeftPanelCollapsed = false;
    }

    private void OnCollapseButtonHoverEnter(object? sender, PointerEventArgs e)
    {
        var r = Resources;
        CollapseButton.BackgroundColor = (Color)r["AccentSurface"]!;
        CollapseButton.Stroke = (Color)r["AccentMuted"]!;
        CollapseButtonLabel.TextColor = (Color)r["AccentText"]!;
    }

    private void OnCollapseButtonHoverExit(object? sender, PointerEventArgs e)
    {
        CollapseButton.BackgroundColor = Colors.Transparent;
        CollapseButton.Stroke = Colors.Transparent;
        CollapseButtonLabel.TextColor = (Color)Resources["TextPrimary"]!;
    }

    private void OnExpandButtonHoverEnter(object? sender, PointerEventArgs e)
    {
        if (sender is Border border)
        {
            var r = Resources;
            border.BackgroundColor = (Color)r["AccentSurface"]!;
            border.Stroke = (Color)r["AccentMuted"]!;
        }
    }

    private void OnExpandButtonHoverExit(object? sender, PointerEventArgs e)
    {
        if (sender is Border border)
        {
            var r = Resources;
            border.BackgroundColor = (Color)r["SurfaceColor"]!;
            border.Stroke = (Color)r["SurfaceBorder"]!;
        }
    }

    private void OnTranscriptAreaHoverEnter(object? sender, PointerEventArgs e)
    {
        SafeAnimate(() => FontSizeControls.FadeToAsync(0.9, 150));
    }

    private void OnTranscriptAreaHoverExit(object? sender, PointerEventArgs e)
    {
        SafeAnimate(() => FontSizeControls.FadeToAsync(0, 300));
    }

    #endregion

    #region Utilities

    private static string FormatFileSize(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB" };
        int i = 0;
        double size = bytes;
        while (size >= 1024 && i < suffixes.Length - 1)
        {
            size /= 1024;
            i++;
        }
        return $"{size:F1} {suffixes[i]}";
    }

    #endregion

    #region Error Dialog

    private void ShowError(string title, string message)
    {
        ErrorDialogTitle.Text = title;
        ErrorDialogMessage.Text = message;
        ErrorDialogOverlay.IsVisible = true;
    }

    private void OnErrorDialogDismiss(object? sender, TappedEventArgs e)
    {
        ErrorDialogOverlay.IsVisible = false;
    }

    #endregion

    #region Stats Helpers

    private void UpdateModelModeDisplay(bool isAccurate)
    {
        ModelModeLabel.Text = isAccurate ? L.Localize(StringKeys.Accurate) : L.Localize(StringKeys.Turbo);
        UpdateModelModeTooltip(isAccurate);
    }

    /// <summary>
    /// Updates the model mode badge tooltip based on the mode
    /// </summary>
    private void UpdateModelModeTooltip(bool isAccurate)
    {
        var tooltip = isAccurate
            ? L.Localize(StringKeys.AccurateModeTooltip)
            : L.Localize(StringKeys.TurboModeTooltip);
        ToolTipProperties.SetText(ModelModeBadge, tooltip);
    }

    #endregion
}
