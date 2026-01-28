using LMKit.Speech;
using LynxTranscribe.Helpers;
using LynxTranscribe.Localization;
using LynxTranscribe.Services;
using L = LynxTranscribe.Localization.LocalizationService;

namespace LynxTranscribe;

/// <summary>
/// Transcription: File selection, model loading, transcription process
/// </summary>
public partial class MainPage
{
    private bool _canTranscribe = false;
    private System.Timers.Timer? _transcribeButtonPulseTimer;

    // Throttling for UI updates during transcription
    private DateTime _lastProgressUpdate = DateTime.MinValue;
    private DateTime _lastSegmentUIUpdate = DateTime.MinValue;
    private const int ProgressUpdateIntervalMs = 100;  // Update progress max 10x/sec
    private const int SegmentUIUpdateIntervalMs = 333;  // Update segments max 3x/sec

    #region File Selection

    private async void OnSelectFileClicked(object? sender, TappedEventArgs e)
    {
        try
        {
            var fileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.WinUI, new[] { ".wav", ".mp3", ".flac", ".ogg", ".m4a", ".wma" } },
                { DevicePlatform.macOS, new[] { "wav", "mp3", "flac", "ogg", "m4a" } },
                { DevicePlatform.MacCatalyst, new[] { "wav", "mp3", "flac", "ogg", "m4a" } }
            });

            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Select an audio file",
                FileTypes = fileTypes
            });

            if (result != null)
            {
                await LoadDroppedFile(result.FullPath);
            }
        }
        catch (Exception ex)
        {
            ShowError("File Selection Error", ex.Message);
        }
    }

    private async Task LoadAudioFile(string filePath)
    {
        try
        {
            await _lmKitService.LoadAudioAsync(filePath);
        }
        catch (Exception ex)
        {
            ShowError("Audio Load Error", ex.Message);
        }
    }

    private void OnClearFileClicked(object? sender, TappedEventArgs e)
    {
        // Only reset the audio file selection in left panel
        // Do NOT clear history state (_currentRecordId, _currentSegments)
        ResetLeftPanel();

        UpdateTranscribeButtonState();
        MainThread.BeginInvokeOnMainThread(async () => await AnimateWaveformBars());
    }

    /// <summary>
    /// Resets the entire left panel to empty state
    /// </summary>
    private void ResetLeftPanel()
    {
        _audioPlayer.Stop();
        _playbackTimer?.Stop();
        PlayPauseIcon.Text = "▶";

        _selectedFilePath = null;
        _lmKitService.DisposeAudio();

        _waveformDrawable.WaveformData = Array.Empty<float>();
        _waveformDrawable.PlaybackPosition = 0;
        WaveformView.Invalidate();

        PlayerPanel.IsVisible = false;

        _isRecording = false;
        _isCountingDown = false;
        StopRecordingDotAnimation();
        RecordingPanel.IsVisible = false;
        CountdownPanel.IsVisible = false;
        RecordingActivePanel.IsVisible = false;
        RecordButtonLabel.Text = L.Localize(StringKeys.Record);
        BrowseButton.IsEnabled = true;
        BrowseButton.Opacity = 1.0;

        DropZone.IsVisible = true;
        EmptyState.IsVisible = true;
        FileSelectedState.IsVisible = false;
        ClearButton.IsVisible = false;

        UpdateFileSelectionUI();
    }

    #endregion

    #region Transcription Button State

    private void UpdateTranscribeButtonState()
    {
        // Only show transcription controls when on Audio tab
        if (!_isFileTabActive)
        {
            TranscriptionControlsPanel.IsVisible = false;
            TranscribeButton.IsVisible = false;
            CancelTranscriptionButton.IsVisible = false;
            HeaderCancelButton.IsVisible = false;
            LeftPanelDisabledOverlay.IsVisible = false;
            InitialStepsPanel.IsVisible = true;
            return;
        }

        if (_isTranscribing || _isModelLoading)
        {
            StopTranscribeButtonPulse();
            TranscribeButton.IsVisible = false;
            CancelTranscriptionButton.IsVisible = true;
            HeaderCancelButton.IsVisible = true;
            TranscriptionControlsPanel.IsVisible = true;
            InitialStepsPanel.IsVisible = false;
            LeftPanelDisabledOverlay.IsVisible = true;
        }
        else
        {
            CancelTranscriptionButton.IsVisible = false;
            HeaderCancelButton.IsVisible = false;
            LeftPanelDisabledOverlay.IsVisible = false;

            _canTranscribe = _lmKitService.HasLoadedAudio;
            if (_canTranscribe)
            {
                // Show file info and controls, hide steps
                InitialStepsPanel.IsVisible = false;
                TranscriptionControlsPanel.IsVisible = true;
                TranscribeButton.IsVisible = true;

                // Update file info in center panel
                UpdateCenterFileInfo();

                UpdateVadToggleUI();
                StartTranscribeButtonPulse();
            }
            else
            {
                // Show steps, hide controls
                StopTranscribeButtonPulse();
                InitialStepsPanel.IsVisible = true;
                TranscriptionControlsPanel.IsVisible = false;
                TranscribeButton.IsVisible = false;
            }
        }
    }

    private void UpdateCenterFileInfo()
    {
        if (!string.IsNullOrEmpty(_selectedFilePath))
        {
            CenterFileName.Text = System.IO.Path.GetFileName(_selectedFilePath);

            var fileInfo = new FileInfo(_selectedFilePath);
            var sizeText = FormatFileSize(fileInfo.Length);
            var durationText = _lmKitService.LoadedAudio != null ? _lmKitService.LoadedAudio.Duration.ToString(@"mm\:ss") : "";

            CenterFileInfo.Text = !string.IsNullOrEmpty(durationText)
                ? $"{sizeText} · {durationText}"
                : sizeText;
        }
    }

    private void StartTranscribeButtonPulse()
    {
        if (_transcribeButtonPulseTimer != null)
        {
            return;
        }

        _transcribeButtonPulseTimer = new System.Timers.Timer(3000);
        _transcribeButtonPulseTimer.Elapsed += (s, e) =>
        {
            if (_isClosing || Application.Current == null)
            {
                return;
            }

            if (_canTranscribe && !_isTranscribing && !_isModelLoading)
            {
                SafeInvokeOnMainThread(async () =>
                {
                    if (!_isClosing)
                    {
                        await PulseTranscribeButton();
                    }
                });
            }
        };
        _transcribeButtonPulseTimer.AutoReset = true;
        _transcribeButtonPulseTimer.Start();

        if (_isClosing)
        {
            return;
        }

        SafeInvokeOnMainThread(async () =>
        {
            await Task.Delay(500);
            if (!_isClosing && _canTranscribe && !_isTranscribing && !_isModelLoading)
            {
                await PulseTranscribeButton();
            }
        });
    }

    /// <summary>
    /// Safely invoke an action on the main thread, catching exceptions if app is closing.
    /// </summary>
    private void SafeInvokeOnMainThread(Action action)
    {
        if (_isClosing || Application.Current == null)
        {
            return;
        }

        try
        {
            MainThread.BeginInvokeOnMainThread(action);
        }
        catch (InvalidOperationException)
        {
            // Main thread not available - app is closing
        }
    }

    private void StopTranscribeButtonPulse()
    {
        _transcribeButtonPulseTimer?.Stop();
        _transcribeButtonPulseTimer?.Dispose();
        _transcribeButtonPulseTimer = null;
        try
        {
            if (!_isClosing)
            {
                TranscribeButton.Scale = 1.0;
            }
        }
        catch { /* App closing */ }
    }

    private async Task PulseTranscribeButton()
    {
        if (_isClosing)
        {
            return;
        }

        try
        {
            for (int j = 0; j < 8; j++)
            {
                if (_isClosing)
                {
                    return;
                }

                TranscribeButton.Scale = 1.0 + (0.08 * j / 8.0);
                await Task.Delay(25);
            }
            for (int j = 8; j >= 0; j--)
            {
                if (_isClosing)
                {
                    return;
                }

                TranscribeButton.Scale = 1.0 + (0.08 * j / 8.0);
                await Task.Delay(25);
            }
            if (!_isClosing)
            {
                TranscribeButton.Scale = 1.0;
            }
        }
        catch
        {
            try { if (!_isClosing) { TranscribeButton.Scale = 1.0; } } catch { }
        }
    }

    private async Task AnimateTranscribeButton()
    {
        try
        {
            await Task.Delay(300);
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    TranscribeButton.Scale = 1.0 + (0.15 * j / 10.0);
                    await Task.Delay(20);
                }
                for (int j = 10; j >= 0; j--)
                {
                    TranscribeButton.Scale = 1.0 + (0.15 * j / 10.0);
                    await Task.Delay(20);
                }
                await Task.Delay(100);
            }
            TranscribeButton.Scale = 1.0;
        }
        catch
        {
            TranscribeButton.Scale = 1.0;
        }
    }

    #endregion

    #region Transcription Process

    private async void OnTranscribeClicked(object? sender, EventArgs e)
    {
        if (_isTranscribing || _isModelLoading)
        {
            return;
        }

        if (!_lmKitService.HasLoadedAudio)
        {
            ShowError("No Audio File", "Please select an audio file first.");
            return;
        }

        _isTranscribing = true;
        _currentRecordId = null;
        _currentSegments.Clear();
        UpdateTranscribeButtonState();

        // Reset view mode to segments
        _isDocumentView = false;
        DocumentScrollView.IsVisible = false;
        SegmentsScrollView.IsVisible = true;
        DocumentLabel.Text = "";

        if (_audioPlayer.IsPlaying)
        {
            _audioPlayer.Pause();
            _playbackTimer?.Stop();
            PlayPauseIcon.Text = "▶";
        }

        // Hide previous results
        EmptyTranscriptState.IsVisible = false; WatermarkLogo.IsVisible = false;
        TranscriptContainer.IsVisible = false;
        TranscriptHeaderLabel.IsVisible = false;
        HistoryPlaybackPanel.IsVisible = false;
        ProgressPanel.IsVisible = false;
        EstimatedTimeLabel.IsVisible = false;
        StatsPanel.IsVisible = false;
        ResultActions.IsVisible = false;

        try
        {
            await PerformTranscription();
        }
        catch (OperationCanceledException)
        {
            HandleTranscriptionCancelled();
        }
        catch (Exception ex)
        {
            ShowError(L.Localize(StringKeys.TranscriptionFailed), ex.Message);
            HandleTranscriptionError();
        }
        finally
        {
            _isTranscribing = false;
            _isModelLoading = false;

            // Hide progress/loading UI
            ProgressPanel.IsVisible = false;
            EstimatedTimeLabel.IsVisible = false;
            ModelLoadingState.IsVisible = false;

            // Reset cancel button state
            CancelTranscriptionText.Text = L.Localize(StringKeys.Cancel);
            CancelTranscriptionIcon.Text = "✕";
            StopTranscribeButtonPulse();

            // Re-evaluate button state - will show transcribe button if audio still loaded
            UpdateTranscribeButtonState();
        }
    }

    private void HandleTranscriptionCancelled()
    {
        // Always show empty state when cancelled, don't show partial results
        _currentSegments.Clear();
        TranscriptContainer.IsVisible = false;
        TranscriptHeaderLabel.IsVisible = false;
        HistoryPlaybackPanel.IsVisible = false;
        EmptyTranscriptState.IsVisible = true;
        WatermarkLogo.IsVisible = true;
        EditToggle.IsVisible = false;
        SearchButton.IsVisible = false;
        DictationGroup.IsVisible = false;
        ViewModeToggle.IsVisible = false;
        StatsPanel.IsVisible = false;
        ResultActions.IsVisible = false;
    }

    private void HandleTranscriptionError()
    {
        if (_currentSegments.Count == 0)
        {
            TranscriptContainer.IsVisible = false;
            TranscriptHeaderLabel.IsVisible = false;
            HistoryPlaybackPanel.IsVisible = false;
            EmptyTranscriptState.IsVisible = true; WatermarkLogo.IsVisible = true;
            EditToggle.IsVisible = false;
            SearchButton.IsVisible = false;
            DictationGroup.IsVisible = false;
            ViewModeToggle.IsVisible = false;
        }
        else
        {
            ShowPartialResults();
        }
    }

    private void ShowPartialResults()
    {
        TranscriptContainer.IsVisible = true;
        TranscriptHeaderLabel.IsVisible = true;
        StatsPanel.IsVisible = true;
        ResultActions.IsVisible = true;
        EditToggle.IsVisible = _currentSegments.Count > 0;
        SearchButton.IsVisible = _currentSegments.Count > 0;
        DictationGroup.IsVisible = _currentSegments.Count > 0;
        ViewModeToggle.IsVisible = false; // Hide during transcription
        UpdateDictationIndicator();

        var partialWords = CalculateFormattedWordCount(_currentSegments, _settingsService.EnableDictationFormatting);
        WordCountLabel.Text = L.Localize(StringKeys.WordsPartial, partialWords);
        SegmentCountLabel.Text = L.Localize(StringKeys.Segments, _currentSegments.Count);
        SegmentCountLabel.IsVisible = true;
        SegmentCountSeparator.IsVisible = true;
        UpdateModelModeDisplay(_useAccurateMode);
        VadBadge.IsVisible = _enableVoiceActivityDetection;
    }

    private async Task PerformTranscription()
    {
        _stopwatch.Restart();
        _transcriptionStartTime = DateTime.Now;
        _lastProgressValue = 0;
        _cancellationTokenSource = new CancellationTokenSource();
        var segments = new List<AudioSegment>();

        await EnsureModelLoaded();

        if (!_lmKitService.HasLoadedModel)
        {
            throw new Exception("Failed to load model.");
        }

        // Prepare UI for transcription
        TranscriptContainer.IsVisible = true;
        TranscriptHeaderLabel.IsVisible = true;
        UpdateDictationIndicator();
        _currentSegments.Clear();
        SegmentsList.Children.Clear();
        _segmentViews.Clear();
        _segmentEditors.Clear();
        _currentHighlightedSegmentIndex = -1;
        _selectedSegmentIndex = -1;
        EditToggle.IsVisible = false;
        SearchButton.IsVisible = false;
        DictationGroup.IsVisible = false;
        ViewModeToggle.IsVisible = false;

        ProgressPanel.IsVisible = true;
        EstimatedTimeLabel.IsVisible = true;
        EstimatedTimeLabel.Text = L.Localize(StringKeys.Calculating);
        await UpdateProgress("Initializing...", 0, "0%");

        // Setup speech-to-text using service
        var speechToText = _lmKitService.CreateSpeechToText(_enableVoiceActivityDetection);

        var segmentLock = new object();

        speechToText.OnNewSegment += (sender, args) =>
        {
            if (string.IsNullOrWhiteSpace(args.Segment.Text))
            {
                return;
            }

            var segment = args.Segment;
            lock (segmentLock) { segments.Add(segment); }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (!_isTranscribing)
                {
                    return;
                }

                _currentSegments.Add(segment);
                AppendSegmentView(segment);

                // Throttle scrolling (it's expensive)
                var now = DateTime.Now;
                if ((now - _lastSegmentUIUpdate).TotalMilliseconds >= SegmentUIUpdateIntervalMs)
                {
                    _lastSegmentUIUpdate = now;
                    // Scroll to bottom using direct Y coordinate
                    _ = ScrollSegmentsToBottomAsync();
                }
            });
        };

        speechToText.OnProgress += (sender, args) =>
        {
            // Throttle progress updates to prevent flooding the main thread
            var now = DateTime.Now;
            if ((now - _lastProgressUpdate).TotalMilliseconds < ProgressUpdateIntervalMs)
            {
                return;
            }

            _lastProgressUpdate = now;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (!_isTranscribing)
                {
                    return;
                }

                TranscriptionProgress.Progress = args.Progress / 100.0;
                ProgressLabel.Text = L.Localize(StringKeys.Transcribing);
                ProgressDetailLabel.Text = $"{args.Progress}%";

                UpdateEstimatedTime(args.Progress);
            });
        };

        // Get selected language from settings
        var transcriptionLanguage = _settingsService.TranscriptionLanguage;

        // Run transcription
        SpeechToText.TranscriptionResult result = null!;
        await Task.Run(async () =>
        {
            result = await speechToText.TranscribeAsync(
                _lmKitService.LoadedAudio!,
                transcriptionLanguage,
                cancellationToken: _cancellationTokenSource.Token
            );
        });

        _stopwatch.Stop();
        _cancellationTokenSource.Token.ThrowIfCancellationRequested();

        await UpdateProgress("Finalizing...", 1.0, "100%");
        await Task.Delay(150);

        // If no segments from events, try to get from result
        if (segments.Count == 0 && result?.Segments != null)
        {
            segments = ExtractSegmentsFromResult(result);
        }

        // Finalize - segments are the source of truth
        var processingTime = _stopwatch.Elapsed.TotalSeconds;
        var modelMode = _useAccurateMode ? "Accurate" : "Turbo";

        // Keep the selected transcription language as-is (auto stays auto)
        var detectedLanguage = transcriptionLanguage;

        string? savedRecordId = null;
        if (segments.Count > 0)
        {
            SaveToHistory(segments, detectedLanguage);
            savedRecordId = _currentRecordId;
        }

        // Get text from the saved record (single source of truth)
        var record = savedRecordId != null ? _historyService.GetById(savedRecordId) : null;
        var plainText = record?.TranscriptText ?? "";
        // Use formatted word count if dictation is enabled
        var wordCount = CalculateFormattedWordCount(segments, _settingsService.EnableDictationFormatting);

        _isTranscribing = false;
        FinalizeTranscriptionView(savedRecordId, segments, plainText, wordCount, processingTime, modelMode, detectedLanguage);
    }

    private void UpdateEstimatedTime(int progress)
    {
        var elapsed = (DateTime.Now - _transcriptionStartTime).TotalSeconds;

        // Always show elapsed time
        var elapsedSpan = TimeSpan.FromSeconds(elapsed);
        ElapsedTimeLabel.Text = elapsedSpan.TotalMinutes >= 1
            ? L.Localize(StringKeys.ElapsedMinutes, (int)elapsedSpan.TotalMinutes, elapsedSpan.Seconds)
            : L.Localize(StringKeys.Elapsed, elapsed);
        ElapsedTimeLabel.IsVisible = true;

        if (progress <= 5 || progress >= 100)
        {
            // Hide remaining time if progress is too low or complete
            TimeSeparatorLabel.IsVisible = false;
            EstimatedTimeLabel.IsVisible = false;
            return;
        }

        var progressDelta = progress - _lastProgressValue;

        if (progressDelta > 0)
        {
            var totalEstimated = elapsed / (progress / 100.0);
            var remaining = totalEstimated - elapsed;

            if (remaining > 0)
            {
                var remainingSpan = TimeSpan.FromSeconds(remaining);
                EstimatedTimeLabel.Text = remainingSpan.TotalMinutes >= 1
                    ? L.Localize(StringKeys.RemainingMinutes, remainingSpan.Minutes, remainingSpan.Seconds)
                    : L.Localize(StringKeys.Remaining, remainingSpan.Seconds);
                TimeSeparatorLabel.IsVisible = true;
                EstimatedTimeLabel.IsVisible = true;
            }
        }
        _lastProgressValue = progress;
    }

    private List<AudioSegment> ExtractSegmentsFromResult(SpeechToText.TranscriptionResult result)
    {
        var segments = new List<AudioSegment>();
        var estimatedTime = TimeSpan.Zero;

        foreach (var seg in result.Segments)
        {
            if (string.IsNullOrWhiteSpace(seg.Text))
            {
                continue;
            }

            var segDuration = TimeSpan.FromSeconds(2);
            segments.Add(new AudioSegment(
                seg.Text.Trim(),
                seg.Language ?? "en",
                estimatedTime,
                estimatedTime + segDuration,
                seg.Confidence
            ));
            estimatedTime += segDuration;
        }
        return segments;
    }

    private void FinalizeTranscriptionView(string? recordId, List<AudioSegment> segments,
        string transcriptText, int wordCount, double processingTime, string modelMode, string? detectedLanguage = null)
    {
        // Reset left panel
        ResetLeftPanel();

        // Switch to history tab
        _isFileTabActive = false;
        UpdateTabUI();

        // Stop history playback
        _historyAudioPlayer.Stop();
        _historyPlaybackTimer?.Stop();
        HistoryPlayPauseIcon.Text = "▶";
        ClearSegmentHighlight();
        _historyAudioFilePath = null;

        // Setup transcript display
        _currentSegments = segments;
        _isEditMode = false;
        _isDocumentView = false; // Start in segments view
        EmptyTranscriptState.IsVisible = false; WatermarkLogo.IsVisible = false;
        TranscriptContainer.IsVisible = true;
        TranscriptHeaderLabel.IsVisible = true;
        UpdateDictationIndicator();
        BuildSegmentView();
        EditToggle.IsVisible = segments.Count > 0;
        SearchButton.IsVisible = segments.Count > 0;
        DictationGroup.IsVisible = segments.Count > 0;
        ViewModeToggle.IsVisible = segments.Count > 0;
        UpdateEditToggleUI();
        UpdateViewModeUI();

        // Setup stats
        StatsPanel.IsVisible = true;
        ResultActions.IsVisible = true;
        WordCountLabel.Text = L.Localize(StringKeys.Words, wordCount);
        SegmentCountLabel.Text = L.Localize(StringKeys.Segments, segments.Count);
        SegmentCountLabel.IsVisible = true;
        SegmentCountSeparator.IsVisible = true;

        // Display processing time
        if (processingTime >= 60)
        {
            var minutes = (int)(processingTime / 60);
            var seconds = processingTime % 60;
            ProcessingTimeLabel.Text = L.Localize(StringKeys.ProcessedInMinutes, minutes, seconds);
        }
        else
        {
            ProcessingTimeLabel.Text = L.Localize(StringKeys.ProcessedIn, processingTime);
        }
        ProcessingTimeLabel.IsVisible = true;
        ProcessingTimeSeparator.IsVisible = true;

        UpdateModelModeDisplay(modelMode == "Accurate");
        VadBadge.IsVisible = _enableVoiceActivityDetection;

        // Display language badge (always show, including auto)
        if (!string.IsNullOrEmpty(detectedLanguage))
        {
            LanguageNameLabel.Text = WhisperLanguages.GetDisplayNameFromCode(detectedLanguage);
            LanguageBadge.IsVisible = true;
        }
        else
        {
            LanguageNameLabel.Text = WhisperLanguages.GetDisplayNameFromCode("auto");
            LanguageBadge.IsVisible = true;
        }

        // Setup history playback
        HistoryPlaybackPanel.IsVisible = false;
        if (!string.IsNullOrEmpty(recordId))
        {
            SetupHistoryPlayback(recordId);
        }

        // Refresh and scroll
        RefreshHistoryList();
        ScrollToTop();

        // Select first segment after scroll completes
        if (segments.Count > 0)
        {
            _ = Dispatcher.DispatchAsync(() =>
            {
                if (_segmentViews.Count > 0)
                {
                    UpdateSegmentSelection(0);
                }
            });
        }
    }

    private void SetupHistoryPlayback(string recordId)
    {
        var record = _historyService.GetById(recordId);
        if (record == null)
        {
            return;
        }

        _currentRecordId = record.Id;
        HistoryDateLabel.Text = record.TranscribedAt.ToString("MMM d, yyyy");

        if (string.IsNullOrEmpty(record.AudioFilePath) || !File.Exists(record.AudioFilePath))
        {
            return;
        }

        _historyAudioFilePath = record.AudioFilePath;
        if (_historyAudioPlayer.Load(record.AudioFilePath))
        {
            HistoryTotalTimeLabel.Text = TranscriptExporter.FormatDisplayTime(_historyAudioPlayer.TotalDuration);
            HistoryCurrentTimeLabel.Text = "0:00";
            HistoryPlaybackSlider.Value = 0;
            HistoryPlayPauseIcon.Text = "▶";
            HistoryAudioSourceLabel.Text = System.IO.Path.GetFileName(record.AudioFilePath);
            HistoryPlaybackPanel.IsVisible = true;
        }
    }

    /// <summary>
    /// Scrolls to top. Fire-and-forget because ScrollToAsync can hang indefinitely in MAUI.
    /// Waits for segment building to complete to avoid scroll conflicts.
    /// </summary>
    private async void ScrollToTop()
    {
        // Wait for segment building to complete (max 2 seconds)
        var timeout = DateTime.Now.AddSeconds(2);
        while (_isBuildingSegments && DateTime.Now < timeout)
        {
            await Task.Delay(50);
        }

        MainThread.BeginInvokeOnMainThread(() =>
        {
            try { _ = HistoryScrollView.ScrollToAsync(0, 0, false); } catch { }
            try { _ = SegmentsScrollView.ScrollToAsync(0, 0, false); } catch { }
        });
    }

    /// <summary>
    /// Scrolls segments list to bottom during transcription.
    /// Uses multiple retry attempts to handle MAUI ScrollView initialization quirks.
    /// </summary>
    private async Task ScrollSegmentsToBottomAsync()
    {
        if (!_isTranscribing)
        {
            return;
        }

        // Wait for layout to complete
        await Task.Delay(50);

        for (int attempt = 0; attempt < 3; attempt++)
        {
            if (!_isTranscribing)
            {
                return;
            }

            try
            {
                // Force layout update
                SegmentsList.InvalidateMeasure();
                await Task.Delay(30);

                // Calculate scroll position
                var contentHeight = SegmentsList.Height;
                var viewportHeight = SegmentsScrollView.Height;

                if (contentHeight > 0 && viewportHeight > 0 && contentHeight > viewportHeight)
                {
                    var scrollY = Math.Max(0, contentHeight - viewportHeight + 20);
                    await SegmentsScrollView.ScrollToAsync(0, scrollY, false);

                    // Verify scroll worked
                    await Task.Delay(20);
                    if (SegmentsScrollView.ScrollY >= scrollY - 50)
                    {
                        return; // Success
                    }
                }
                else if (_segmentViews.Count > 0)
                {
                    // Fallback to element-based scroll
                    var lastElement = _segmentViews[_segmentViews.Count - 1];
                    await SegmentsScrollView.ScrollToAsync(lastElement, ScrollToPosition.End, false);
                    return;
                }
            }
            catch
            {
                // Ignore and retry
            }

            await Task.Delay(50);
        }
    }

    #endregion

    #region Model Loading

    private async Task EnsureModelLoaded()
    {
        // Check if requested model is already loaded
        var requestedModelId = _useAccurateMode ? LMKitService.ModelIdAccurate : LMKitService.ModelIdTurbo;
        if (_lmKitService.HasLoadedModel && _lmKitService.CurrentModelId == requestedModelId)
        {
            return;
        }

        _isModelLoading = true;
        UpdateTranscribeButtonState();

        var modelName = _lmKitService.GetModelDisplayName(_useAccurateMode);
        ModelLoadingDetail.Text = "";

        var modelCard = _lmKitService.GetModelCard(_useAccurateMode);
        bool needsDownload = !modelCard.IsLocallyAvailable;

        if (needsDownload)
        {
            var sizeMB = modelCard.FileSize / (1024.0 * 1024.0);
            ModelLoadingLabel.Text = L.Localize(StringKeys.DownloadingModel, modelName);
            ModelLoadingDetail.Text = L.Localize(StringKeys.MBRequired, sizeMB);

            // Hide original loading state - overlay will show progress instead
            ModelLoadingState.IsVisible = false;

            // Show download overlay - only cancel button should work
            SetUIEnabledDuringDownload(false);
        }
        else
        {
            // For local loading (no download), show the original loading state
            ModelLoadingState.IsVisible = true;
            ModelLoadingLabel.Text = L.Localize(StringKeys.LoadingModel, modelName);
        }

        try
        {
            await _lmKitService.EnsureModelLoadedAsync(_useAccurateMode, OnModelDownloadProgress);
        }
        finally
        {
            _isModelLoading = false;
            ModelLoadingState.IsVisible = false;

            // Re-enable UI after download completes or fails
            if (needsDownload)
            {
                SetUIEnabledDuringDownload(true);
            }
        }
    }

    /// <summary>
    /// Enable or disable UI during model download using a blocking overlay.
    /// Only the cancel button on the overlay remains active.
    /// </summary>
    private void SetUIEnabledDuringDownload(bool enabled)
    {
        if (DownloadBlockingOverlay == null)
        {
            return;
        }

        DownloadBlockingOverlay.IsVisible = !enabled;

        // Hide header cancel button when overlay is shown (overlay has its own cancel button)
        CancelTranscriptionButton.IsVisible = enabled && (_isTranscribing || _isModelLoading);

        if (!enabled && OverlayDownloadLabel != null && OverlayDownloadDetail != null)
        {
            // Sync initial text to overlay
            OverlayDownloadLabel.Text = ModelLoadingLabel.Text;
            OverlayDownloadDetail.Text = ModelLoadingDetail.Text;
        }
    }

    private bool OnModelDownloadProgress(string path, long? contentLength, long bytesRead)
    {
        if (contentLength.HasValue && contentLength.Value > 0)
        {
            // Throttle download progress UI updates
            var now = DateTime.Now;
            if ((now - _lastProgressUpdate).TotalMilliseconds < ProgressUpdateIntervalMs)
            {
                return !_lmKitService.ModelDownloadCancelRequest;
            }

            _lastProgressUpdate = now;

            var percentage = (int)((double)bytesRead / contentLength.Value * 100);
            var downloadedMB = bytesRead / (1024.0 * 1024.0);
            var totalMB = contentLength.Value / (1024.0 * 1024.0);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                var labelText = L.Localize(StringKeys.DownloadingModelProgress);
                var detailText = $"{downloadedMB:F0} / {totalMB:F0} MB ({percentage}%)";

                // Update both the original labels and the overlay labels
                ModelLoadingLabel.Text = labelText;
                ModelLoadingDetail.Text = detailText;
                if (OverlayDownloadLabel != null)
                {
                    OverlayDownloadLabel.Text = labelText;
                }

                if (OverlayDownloadDetail != null)
                {
                    OverlayDownloadDetail.Text = detailText;
                }
            });
        }
        return !_lmKitService.ModelDownloadCancelRequest;
    }

    #endregion

    #region Cancel Button

    private void OnCancelTranscriptionClicked(object? sender, TappedEventArgs e)
    {
        if (_isModelLoading)
        {
            _lmKitService.RequestModelDownloadCancel();
        }
        else if (_isTranscribing)
        {
            _cancellationTokenSource?.Cancel();
        }

        CancelTranscriptionText.Text = L.Localize(StringKeys.Cancelling);
        CancelTranscriptionIcon.Text = "⏳";
        if (OverlayCancelText != null)
        {
            OverlayCancelText.Text = L.Localize(StringKeys.Cancelling);
        }
    }

    private void OnCancelTranscriptionHoverEnter(object? sender, PointerEventArgs e)
    {
        // Brighter red on hover for dark theme
        CancelTranscriptionButton.BackgroundColor = Color.FromArgb("#dc2626");
        CancelTranscriptionButton.Stroke = Color.FromArgb("#ef4444");
        CancelTranscriptionButton.StrokeThickness = 1;
        CancelTranscriptionButton.Scale = 1.02;
    }

    private void OnCancelTranscriptionHoverExit(object? sender, PointerEventArgs e)
    {
        CancelTranscriptionButton.BackgroundColor = (Color)Resources["DangerColor"]!;
        CancelTranscriptionButton.Stroke = Colors.Transparent;
        CancelTranscriptionButton.StrokeThickness = 0;
        CancelTranscriptionButton.Scale = 1.0;

        if (_isTranscribing || _isModelLoading)
        {
            CancelTranscriptionText.Text = L.Localize(StringKeys.Cancel);
            CancelTranscriptionIcon.Text = "✕";
        }
    }

    private void OnHeaderCancelHoverEnter(object? sender, PointerEventArgs e)
    {
        HeaderCancelButton.BackgroundColor = Color.FromArgb("#dc2626");
        HeaderCancelButton.Stroke = Color.FromArgb("#fca5a5");
        HeaderCancelIcon.TextColor = Colors.White;
        HeaderCancelText.TextColor = Colors.White;
        HeaderCancelButton.Scale = 1.02;
    }

    private void OnHeaderCancelHoverExit(object? sender, PointerEventArgs e)
    {
        HeaderCancelButton.BackgroundColor = (Color)Resources["DangerSurface"]!;
        HeaderCancelButton.Stroke = (Color)Resources["DangerColor"]!;
        HeaderCancelIcon.TextColor = (Color)Resources["DangerColor"]!;
        HeaderCancelText.TextColor = (Color)Resources["DangerColor"]!;
        HeaderCancelButton.Scale = 1.0;
    }

    #endregion

    #region Progress

    private async Task UpdateProgress(string title, double progress, string percentage)
    {
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            ProgressLabel.Text = title;
            TranscriptionProgress.Progress = progress;
            ProgressDetailLabel.Text = percentage;
        });
    }

    #endregion
}
