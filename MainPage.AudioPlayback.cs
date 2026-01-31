using LMKit.Speech;
using LMKit.Speech.Dictation;
using LynxTranscribe.Helpers;
using LynxTranscribe.Localization;
#if WINDOWS
using NAudio.Wave;
#elif MACCATALYST || IOS
using AVFoundation;
using Foundation;
#endif
using System.Diagnostics;
using L = LynxTranscribe.Localization.LocalizationService;

namespace LynxTranscribe;

/// <summary>
/// Audio playback: Source player (left panel), History player (right panel), waveform, speed control
/// </summary>
public partial class MainPage
{
    private bool _isDraggingHistorySlider = false;
    private bool _isSyncingVolume = false;
    private bool _isBuildingSegments = false;

    #region Source Audio Playback (Left Panel)

    private void OnPlayPauseClicked(object? sender, EventArgs e)
    {
        if (_audioPlayer.IsPlaying)
        {
            _audioPlayer.Pause();
            _playbackTimer?.Stop();
            PlayPauseIcon.Text = "▶";
        }
        else
        {
            _audioPlayer.Play();
            _playbackTimer?.Start();
            PlayPauseIcon.Text = "II";
        }
    }

    private void OnRestartClicked(object? sender, EventArgs e)
    {
        // Seek to beginning
        _audioPlayer.Seek(TimeSpan.Zero);

        // Start playing if not already
        if (!_audioPlayer.IsPlaying)
        {
            _audioPlayer.Play();
            _playbackTimer?.Start();
            PlayPauseIcon.Text = "II";
        }

        // Update slider position
        PlaybackSlider.Value = 0;
        CurrentTimeLabel.Text = "0:00";
    }

    private void OnSeekBackward(object? sender, TappedEventArgs e)
    {
        _audioPlayer.SeekBackward(TimeSpan.FromSeconds(AppConstants.Playback.SeekSeconds));
    }

    private void OnSeekForward(object? sender, TappedEventArgs e)
    {
        _audioPlayer.SeekForward(TimeSpan.FromSeconds(AppConstants.Playback.SeekSeconds));
    }

    private void OnVolumeChanged(object? sender, ValueChangedEventArgs e)
    {
        if (_isSyncingVolume)
        {
            return;
        }

        _audioPlayer.Volume = (float)e.NewValue;
        _historyAudioPlayer.Volume = (float)e.NewValue;
        _settingsService.PlaybackVolume = (float)e.NewValue;

        // Sync history volume slider (if it exists)
        if (HistoryVolumeSlider != null)
        {
            _isSyncingVolume = true;
            HistoryVolumeSlider.Value = e.NewValue;
            _isSyncingVolume = false;
        }
    }

    private void OnPlaybackSliderDragStarted(object? sender, EventArgs e)
    {
        _isDraggingSlider = true;
    }

    private void OnPlaybackSliderDragCompleted(object? sender, EventArgs e)
    {
        _isDraggingSlider = false;
        var position = TimeSpan.FromSeconds(PlaybackSlider.Value / 100 * _audioPlayer.TotalDuration.TotalSeconds);
        _audioPlayer.Seek(position);
    }

    private void OnPlaybackSliderChanged(object? sender, ValueChangedEventArgs e)
    {
        if (_isDraggingSlider)
        {
            var position = TimeSpan.FromSeconds(e.NewValue / 100 * _audioPlayer.TotalDuration.TotalSeconds);
            CurrentTimeLabel.Text = TranscriptExporter.FormatDisplayTime(position);
        }
    }

    private void OnAudioPositionChanged(object? sender, TimeSpan position)
    {
        if (_isClosing)
        {
            return;
        }

        try
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (_isClosing)
                {
                    return;
                }

                var progress = _audioPlayer.TotalDuration.TotalSeconds > 0
                    ? position.TotalSeconds / _audioPlayer.TotalDuration.TotalSeconds * 100
                    : 0;

                if (!_isDraggingSlider)
                {
                    PlaybackSlider.Value = progress;
                    CurrentTimeLabel.Text = TranscriptExporter.FormatDisplayTime(position);
                    _waveformDrawable.PlaybackPosition = progress / 100.0;
                    WaveformView.Invalidate();
                }
            });
        }
        catch { /* App closing */ }
    }

    private void OnAudioPlaybackStopped(object? sender, EventArgs e)
    {
        if (_isClosing)
        {
            return;
        }

        try
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (_isClosing)
                {
                    return;
                }

                _playbackTimer?.Stop();
                PlayPauseIcon.Text = "▶";
                _audioPlayer.Seek(TimeSpan.Zero);
                PlaybackSlider.Value = 0;
                CurrentTimeLabel.Text = "0:00";
                _waveformDrawable.PlaybackPosition = 0;
                WaveformView.Invalidate();
            });
        }
        catch { /* App closing */ }
    }

    #endregion

    #region History Audio Playback (Right Panel)

    private void OnHistoryPlayPauseClicked(object? sender, EventArgs e)
    {
        if (_historyAudioPlayer.IsPlaying)
        {
            _historyAudioPlayer.Pause();
            _historyPlaybackTimer?.Stop();
            HistoryPlayPauseIcon.Text = "▶";
        }
        else
        {
            _historyAudioPlayer.Play();
            _historyPlaybackTimer?.Start();
            HistoryPlayPauseIcon.Text = "▌▌";
        }
    }

    private void OnHistoryPlayPauseHoverEnter(object? sender, PointerEventArgs e)
    {
        ApplyStyle(HistoryPlayPauseButton, ControlStyle.PlayButtonHover, HistoryPlayPauseIcon);
    }

    private void OnHistoryPlayPauseHoverExit(object? sender, PointerEventArgs e)
    {
        ApplyStyle(HistoryPlayPauseButton, ControlStyle.PlayButton, HistoryPlayPauseIcon);
    }

    private void OnHistoryPlaybackSliderDragStarted(object? sender, EventArgs e)
    {
        _isDraggingHistorySlider = true;
    }

    private void OnHistoryPlaybackSliderDragCompleted(object? sender, EventArgs e)
    {
        _isDraggingHistorySlider = false;
        var position = TimeSpan.FromSeconds(HistoryPlaybackSlider.Value / 100 * _historyAudioPlayer.TotalDuration.TotalSeconds);
        _historyAudioPlayer.Seek(position);

        // Find and scroll to the segment at this position
        ScrollToSegmentAtPosition(position);
    }

    /// <summary>
    /// Finds the segment at the given position and scrolls to it (used when user drags slider)
    /// </summary>
    private void ScrollToSegmentAtPosition(TimeSpan position)
    {
        if (_currentSegments == null || _currentSegments.Count == 0 || _segmentViews.Count == 0)
        {
            return;
        }

        // Find segment at this position
        int targetIndex = -1;
        for (int i = 0; i < _currentSegments.Count; i++)
        {
            var seg = _currentSegments[i];
            if (position >= seg.Start && position < seg.End)
            {
                targetIndex = i;
                break;
            }
        }

        // If not found in any segment, find nearest
        if (targetIndex == -1)
        {
            if (position < _currentSegments[0].Start)
            {
                targetIndex = 0;
            }
            else
            {
                // Find segment just before this position
                for (int i = _currentSegments.Count - 1; i >= 0; i--)
                {
                    if (position >= _currentSegments[i].Start)
                    {
                        targetIndex = i;
                        break;
                    }
                }
            }
        }

        // Only update if segment changed
        if (targetIndex >= 0 && targetIndex < _segmentViews.Count && targetIndex != _selectedSegmentIndex)
        {
            UpdateSegmentSelection(targetIndex);
            ScrollToCurrentSegment(_segmentViews[targetIndex]);
        }
    }

    private void OnHistoryPlaybackSliderChanged(object? sender, ValueChangedEventArgs e)
    {
        if (_isDraggingHistorySlider)
        {
            var position = TimeSpan.FromSeconds(e.NewValue / 100 * _historyAudioPlayer.TotalDuration.TotalSeconds);
            HistoryCurrentTimeLabel.Text = TranscriptExporter.FormatDisplayTime(position);

            // Scroll to segment while dragging
            ScrollToSegmentAtPosition(position);
        }
    }

    private void OnHistoryVolumeChanged(object? sender, ValueChangedEventArgs e)
    {
        if (_isSyncingVolume)
        {
            return;
        }

        _historyAudioPlayer.Volume = (float)e.NewValue;
        _audioPlayer.Volume = (float)e.NewValue;
        _settingsService.PlaybackVolume = (float)e.NewValue;

        // Sync settings panel volume slider (if it exists)
        if (VolumeSlider != null)
        {
            _isSyncingVolume = true;
            VolumeSlider.Value = e.NewValue;
            _isSyncingVolume = false;
        }

        // Update icon based on volume level
        if (HistoryVolumeIcon != null)
        {
            if (e.NewValue == 0)
            {
                HistoryVolumeIcon.Text = "🔇";
            }
            else if (e.NewValue < 0.5)
            {
                HistoryVolumeIcon.Text = "🔉";
            }
            else
            {
                HistoryVolumeIcon.Text = "🔊";
            }
        }
    }

    private void OnHistoryAudioPositionChanged(object? sender, TimeSpan position)
    {
        if (_isClosing)
        {
            return;
        }

        try
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (_isClosing)
                {
                    return;
                }

                if (!_isDraggingHistorySlider && !_isManuallySelectingSegment && HistoryPlaybackPanel.IsVisible)
                {
                    var progress = _historyAudioPlayer.TotalDuration.TotalSeconds > 0
                        ? position.TotalSeconds / _historyAudioPlayer.TotalDuration.TotalSeconds * 100
                        : 0;
                    HistoryPlaybackSlider.Value = progress;
                    HistoryCurrentTimeLabel.Text = TranscriptExporter.FormatDisplayTime(position);

                    // Update karaoke highlighting only during actual playback
                    if (_historyAudioPlayer.IsPlaying)
                    {
                        UpdatePlaybackHighlight(position);
                    }
                }
            });
        }
        catch { /* App closing */ }
    }

    private void OnHistoryAudioPlaybackStopped(object? sender, EventArgs e)
    {
        if (_isClosing)
        {
            return;
        }

        try
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (_isClosing)
                {
                    return;
                }

                _historyPlaybackTimer?.Stop();
                HistoryPlayPauseIcon.Text = "▶";
                _historyAudioPlayer.Seek(TimeSpan.Zero);
                HistoryPlaybackSlider.Value = 0;
                HistoryCurrentTimeLabel.Text = "0:00";

                // Clear highlight but keep segments visible
                ClearSegmentHighlight();
            });
        }
        catch { /* App closing */ }
    }

    private void OnBrowseAudioFileClicked(object? sender, TappedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_historyAudioFilePath) && File.Exists(_historyAudioFilePath))
        {
            try
            {
#if WINDOWS
                System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{_historyAudioFilePath}\"");
#elif MACCATALYST
                System.Diagnostics.Process.Start("open", $"-R \"{_historyAudioFilePath}\"");
#else
                ShowToast(L.Localize(StringKeys.OpenFileLocationWindowsOnly), ToastType.Info);
#endif
            }
            catch (Exception ex)
            {
                ShowToast(L.Localize(StringKeys.CouldNotOpenFileLocation, ex.Message), ToastType.Error);
            }
        }
        else
        {
            ShowToast(L.Localize(StringKeys.AudioFileNotFound), ToastType.Warning);
        }
    }

    private void OnBrowseAudioFileHoverEnter(object? sender, PointerEventArgs e)
    {
        ApplyStyle(BrowseAudioFileButton, ControlStyle.ButtonHover, BrowseAudioFileIcon);
    }

    private void OnBrowseAudioFileHoverExit(object? sender, PointerEventArgs e)
    {
        ApplyStyle(BrowseAudioFileButton, ControlStyle.ButtonDefault, BrowseAudioFileIcon);
    }

    #endregion

    #region Speed Control

    // Available speed values in order
    private static readonly double[] SpeedValues = { 0.5, 0.75, 1.0, 1.25, 1.5, 1.75, 2.0 };

    private void OnSpeed05Clicked(object? sender, TappedEventArgs e) => SetPlaybackSpeed(0.5);
    private void OnSpeed1Clicked(object? sender, TappedEventArgs e) => SetPlaybackSpeed(1.0);
    private void OnSpeed15Clicked(object? sender, TappedEventArgs e) => SetPlaybackSpeed(1.5);
    private void OnSpeed2Clicked(object? sender, TappedEventArgs e) => SetPlaybackSpeed(2.0);

    private void SetPlaybackSpeed(double speed)
    {
        _playbackSpeed = Math.Clamp(speed, 0.5, 2.0);
        _audioPlayer.SetPlaybackSpeed(_playbackSpeed);
        _historyAudioPlayer.SetPlaybackSpeed(_playbackSpeed);
        _settingsService.PlaybackSpeed = _playbackSpeed;
        UpdateSpeedButtonsUI();
        UpdateHistorySpeedUI();
    }

    private void UpdateSpeedButtonsUI()
    {
        var accentSurface = (Color)Resources["AccentSurface"]!;
        var accentMuted = (Color)Resources["AccentMuted"]!;
        var accentText = (Color)Resources["AccentText"]!;
        var backgroundTertiary = (Color)Resources["BackgroundTertiary"]!;
        var surfaceBorder = (Color)Resources["SurfaceBorder"]!;
        var textSecondary = (Color)Resources["TextSecondary"]!;

        Speed05Button.BackgroundColor = backgroundTertiary;
        Speed05Button.Stroke = surfaceBorder;
        Speed05Label.TextColor = textSecondary;
        Speed1Button.BackgroundColor = backgroundTertiary;
        Speed1Button.Stroke = surfaceBorder;
        Speed1Label.TextColor = textSecondary;
        Speed15Button.BackgroundColor = backgroundTertiary;
        Speed15Button.Stroke = surfaceBorder;
        Speed15Label.TextColor = textSecondary;
        Speed2Button.BackgroundColor = backgroundTertiary;
        Speed2Button.Stroke = surfaceBorder;
        Speed2Label.TextColor = textSecondary;

        Border activeButton;
        Label activeLabel;

        switch (_playbackSpeed)
        {
            case 0.5: activeButton = Speed05Button; activeLabel = Speed05Label; break;
            case 1.5: activeButton = Speed15Button; activeLabel = Speed15Label; break;
            case 2.0: activeButton = Speed2Button; activeLabel = Speed2Label; break;
            default: activeButton = Speed1Button; activeLabel = Speed1Label; break;
        }

        activeButton.BackgroundColor = accentSurface;
        activeButton.Stroke = accentMuted;
        activeLabel.TextColor = accentText;
    }

    private void UpdateHistorySpeedUI()
    {
        // Update the speed display label
        string speedText = Math.Abs(_playbackSpeed - 1.0) < 0.01 ? "1×" : $"{_playbackSpeed:0.##}×";
        HistorySpeedLabel.Text = speedText;

        // Get theme colors
        var textPrimary = (Color)Resources["TextPrimary"]!;
        var accentText = (Color)Resources["AccentText"]!;
        var textTertiary = (Color)Resources["TextTertiary"]!;

        // Update label color based on whether speed is normal
        bool isNormalSpeed = Math.Abs(_playbackSpeed - 1.0) < 0.01;
        HistorySpeedLabel.TextColor = isNormalSpeed ? textPrimary : accentText;

        // Update +/- button states (dim if at limits)
        bool atMinSpeed = _playbackSpeed <= 0.5 + 0.01;
        bool atMaxSpeed = _playbackSpeed >= 2.0 - 0.01;

        SpeedSlowLabel.TextColor = atMinSpeed ? textTertiary : textPrimary;
        SpeedFastLabel.TextColor = atMaxSpeed ? textTertiary : textPrimary;

        // Update tooltips with localized text
        ToolTipProperties.SetText(SpeedSlowButton, L.Localize(StringKeys.SpeedSlower));
        ToolTipProperties.SetText(SpeedFastButton, L.Localize(StringKeys.SpeedFaster));
        ToolTipProperties.SetText(SpeedValueButton, L.Localize(StringKeys.SpeedReset));
    }

    /// <summary>
    /// Finds the index of the closest speed value in SpeedValues array
    /// </summary>
    private int FindCurrentSpeedIndex()
    {
        int closestIndex = 2; // Default to 1.0 (index 2)
        double minDiff = double.MaxValue;

        for (int i = 0; i < SpeedValues.Length; i++)
        {
            double diff = Math.Abs(SpeedValues[i] - _playbackSpeed);
            if (diff < minDiff)
            {
                minDiff = diff;
                closestIndex = i;
            }
        }

        return closestIndex;
    }

    // Decrease speed
    private void OnSpeedSlowClicked(object? sender, TappedEventArgs e)
    {
        int currentIndex = FindCurrentSpeedIndex();

        if (currentIndex > 0)
        {
            SetPlaybackSpeed(SpeedValues[currentIndex - 1]);
        }
    }

    // Increase speed
    private void OnSpeedFastClicked(object? sender, TappedEventArgs e)
    {
        int currentIndex = FindCurrentSpeedIndex();

        if (currentIndex < SpeedValues.Length - 1)
        {
            SetPlaybackSpeed(SpeedValues[currentIndex + 1]);
        }
    }

    // Reset to 1x
    private void OnSpeedResetClicked(object? sender, TappedEventArgs e)
    {
        SetPlaybackSpeed(1.0);
    }

    // Hover effects for - button
    private void OnSpeedSlowHoverEnter(object? sender, PointerEventArgs e)
    {
        if (_playbackSpeed > 0.5 + 0.01)
        {
            SpeedSlowButton.BackgroundColor = (Color)Resources["BackgroundTertiary"]!;
            SpeedSlowLabel.TextColor = (Color)Resources["AccentText"]!;
        }
    }

    private void OnSpeedSlowHoverExit(object? sender, PointerEventArgs e)
    {
        SpeedSlowButton.BackgroundColor = Colors.Transparent;
        bool atMinSpeed = _playbackSpeed <= 0.5 + 0.01;
        SpeedSlowLabel.TextColor = atMinSpeed
            ? (Color)Resources["TextTertiary"]!
            : (Color)Resources["TextPrimary"]!;
    }

    // Hover effects for + button
    private void OnSpeedFastHoverEnter(object? sender, PointerEventArgs e)
    {
        if (_playbackSpeed < 2.0 - 0.01)
        {
            SpeedFastButton.BackgroundColor = (Color)Resources["BackgroundTertiary"]!;
            SpeedFastLabel.TextColor = (Color)Resources["AccentText"]!;
        }
    }

    private void OnSpeedFastHoverExit(object? sender, PointerEventArgs e)
    {
        SpeedFastButton.BackgroundColor = Colors.Transparent;
        bool atMaxSpeed = _playbackSpeed >= 2.0 - 0.01;
        SpeedFastLabel.TextColor = atMaxSpeed
            ? (Color)Resources["TextTertiary"]!
            : (Color)Resources["TextPrimary"]!;
    }

    // Hover effects for speed value (center)
    private void OnSpeedValueHoverEnter(object? sender, PointerEventArgs e)
    {
        if (Math.Abs(_playbackSpeed - 1.0) > 0.01)
        {
            SpeedValueButton.BackgroundColor = (Color)Resources["AccentSurface"]!;
        }
    }

    private void OnSpeedValueHoverExit(object? sender, PointerEventArgs e)
    {
        SpeedValueButton.BackgroundColor = Colors.Transparent;
    }

    /// <summary>
    /// Refreshes speed control UI after theme change
    /// </summary>
    private void RefreshSpeedControlTheme()
    {
        // Reset background colors to transparent (they use dynamic lookup on hover)
        SpeedSlowButton.BackgroundColor = Colors.Transparent;
        SpeedFastButton.BackgroundColor = Colors.Transparent;
        SpeedValueButton.BackgroundColor = Colors.Transparent;

        // Update all text colors based on current state
        UpdateHistorySpeedUI();
    }

    #endregion

    #region Waveform

    private void InitializeWaveform()
    {
        _waveformDrawable = new WaveformDrawable
        {
            WaveformColor = Color.FromArgb("#3F3F46"),
            PlayedColor = Color.FromArgb("#F59E0B"),
            PositionColor = Color.FromArgb("#F59E0B")
        };
        WaveformView.Drawable = _waveformDrawable;
    }

    private void UpdateWaveformColors()
    {
        var isDark = _settingsService.DarkMode;
        _waveformDrawable.WaveformColor = isDark ? Color.FromArgb("#3F3F46") : Color.FromArgb("#D4D4D8");
        _waveformDrawable.PlayedColor = (Color)Resources["AccentPrimary"]!;
        _waveformDrawable.PositionColor = (Color)Resources["AccentPrimary"]!;
        WaveformView.Invalidate();
    }

    private async Task GenerateWaveformAsync(string filePath)
    {
        try
        {
#if WINDOWS
            var waveformData = await Task.Run(() =>
            {
                using var reader = new AudioFileReader(filePath);
                var totalSamples = (int)(reader.Length / (reader.WaveFormat.BitsPerSample / 8));
                var channels = reader.WaveFormat.Channels;
                var targetBars = 80;
                var samplesPerBar = totalSamples / targetBars / channels;
                if (samplesPerBar < 1)
                {
                    samplesPerBar = 1;
                }

                var waveform = new float[targetBars];
                var buffer = new float[samplesPerBar * channels];

                for (int bar = 0; bar < targetBars; bar++)
                {
                    int samplesRead = reader.Read(buffer, 0, buffer.Length);
                    if (samplesRead == 0)
                    {
                        break;
                    }

                    float maxAmplitude = 0;
                    for (int i = 0; i < samplesRead; i++)
                    {
                        var amplitude = Math.Abs(buffer[i]);
                        if (amplitude > maxAmplitude)
                        {
                            maxAmplitude = amplitude;
                        }
                    }
                    waveform[bar] = maxAmplitude;
                }

                var max = waveform.Max();
                if (max > 0)
                {
                    for (int i = 0; i < waveform.Length; i++)
                    {
                        waveform[i] /= max;
                    }
                }

                return waveform;
            });
#elif MACCATALYST || IOS
            var waveformData = await Task.Run(() =>
            {
                var targetBars = 80;
                var waveform = new float[targetBars];

                try
                {
                    var url = NSUrl.FromFilename(filePath);
                    using var asset = AVAsset.FromUrl(url);
                    var duration = asset.Duration.Seconds;
                    
                    for (int i = 0; i < targetBars; i++)
                    {
                        waveform[i] = 0.3f + 0.4f * (float)((i * 7) % 10) / 10f;
                    }
                }
                catch
                {
                    for (int i = 0; i < targetBars; i++)
                    {
                        waveform[i] = 0.5f;
                    }
                }

                return waveform;
            });
#else
            var waveformData = new float[80];
            for (int i = 0; i < 80; i++)
            {
                waveformData[i] = 0.5f;
            }
#endif

            MainThread.BeginInvokeOnMainThread(() =>
            {
                _waveformDrawable.WaveformData = waveformData;
                _waveformDrawable.PlaybackPosition = 0;
                WaveformView.Invalidate();
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error generating waveform: {ex.Message}");
        }
    }

    private void OnWaveformTapped(object? sender, TappedEventArgs e)
    {
        var position = e.GetPosition(WaveformView);
        if (position.HasValue && WaveformView.Width > 0)
        {
            var progress = position.Value.X / WaveformView.Width;
            progress = Math.Clamp(progress, 0, 1);

            var seekTime = TimeSpan.FromSeconds(progress * _audioPlayer.TotalDuration.TotalSeconds);
            _audioPlayer.Seek(seekTime);

            PlaybackSlider.Value = progress * 100;
            _waveformDrawable.PlaybackPosition = progress;
            WaveformView.Invalidate();
        }
    }

    private double _waveformPanStartX;
    private bool _isDraggingWaveform;

    private void OnWaveformPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        if (WaveformView.Width <= 0 || _audioPlayer.TotalDuration.TotalSeconds <= 0)
        {
            return;
        }

        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _isDraggingWaveform = true;
                _waveformPanStartX = _waveformDrawable.PlaybackPosition * WaveformView.Width;
                break;

            case GestureStatus.Running:
                if (_isDraggingWaveform)
                {
                    var currentX = _waveformPanStartX + e.TotalX;
                    var progress = currentX / WaveformView.Width;
                    progress = Math.Clamp(progress, 0, 1);

                    // Update visual immediately
                    _waveformDrawable.PlaybackPosition = progress;
                    WaveformView.Invalidate();
                    PlaybackSlider.Value = progress * 100;

                    // Update time display
                    var currentTime = TimeSpan.FromSeconds(progress * _audioPlayer.TotalDuration.TotalSeconds);
                    CurrentTimeLabel.Text = TranscriptExporter.FormatDisplayTime(currentTime);
                }
                break;

            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                if (_isDraggingWaveform)
                {
                    _isDraggingWaveform = false;

                    // Seek to final position
                    var finalProgress = _waveformDrawable.PlaybackPosition;
                    var seekTime = TimeSpan.FromSeconds(finalProgress * _audioPlayer.TotalDuration.TotalSeconds);
                    _audioPlayer.Seek(seekTime);
                }
                break;
        }
    }

    #endregion

    #region Unified Segment View

    private int _currentHighlightedSegmentIndex = -1;  // During playback
    private int _selectedSegmentIndex = -1;             // User selection
    private readonly List<Border> _segmentViews = new();
    private readonly List<Editor> _segmentEditors = new();

    // Double-click detection for segments
    private int _lastClickedSegmentIndex = -1;
    private DateTime _lastSegmentClickTime = DateTime.MinValue;

    // Flag to prevent position updates during manual selection
    private bool _isManuallySelectingSegment = false;

    /// <summary>
    /// Builds the unified segment view - always visible, always editable
    /// Uses async batching to avoid blocking UI
    /// </summary>
    public void BuildSegmentView()
    {
        try
        {
            SegmentsList.Children.Clear();
            _segmentViews.Clear();
            _segmentEditors.Clear();
            _currentHighlightedSegmentIndex = -1;
            _selectedSegmentIndex = -1;
            _userLockedSegmentIndex = -1;

            if (_currentSegments == null || _currentSegments.Count == 0)
            {
                var emptyLabel = new Label
                {
                    Text = "No transcript segments available",
                    FontSize = _transcriptFontSize,
                    TextColor = (Color)Resources["TextMuted"]!,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    Margin = new Thickness(20)
                };
                SegmentsList.Children.Add(emptyLabel);
                return;
            }

            BuildSegmentViewAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"BuildSegmentView error: {ex.Message}");
        }
    }

    private int _buildGeneration = 0;

    private async void BuildSegmentViewAsync()
    {
        _buildGeneration++;
        var myGeneration = _buildGeneration;
        _isBuildingSegments = true;

        const int initialBatch = 0;
        const int batchSize = 5;

        try
        {
            // Build remaining in batches, yielding to UI between batches
            for (int start = initialBatch; start < _currentSegments.Count; start += batchSize)
            {
                // Check if we should stop (user switched records, app closing, etc.)
                if (_isClosing || _buildGeneration != myGeneration)
                {
                    return;
                }

                await Task.Delay(10); // Yield to UI thread

                if (_isClosing || _buildGeneration != myGeneration)
                {
                    return;
                }

                var end = Math.Min(start + batchSize, _currentSegments.Count);
                for (int i = start; i < end; i++)
                {
                    var segment = _currentSegments[i];
                    var (border, editor) = CreateEditableSegment(segment, i);
                    SegmentsList.Children.Add(border);
                    _segmentViews.Add(border);
                    _segmentEditors.Add(editor);
                }
            }
        }
        finally
        {
            if (_buildGeneration == myGeneration)
            {
                _isBuildingSegments = false;
            }
        }
    }

    private (Border border, Editor editor) CreateEditableSegment(AudioSegment segment, int index)
    {
        var textPrimary = (Color)Resources["TextPrimary"]!;
        var textSecondary = (Color)Resources["TextSecondary"]!;
        var textTertiary = (Color)Resources["TextTertiary"]!;
        var surfaceBorder = (Color)Resources["SurfaceBorder"]!;
        var backgroundTertiary = (Color)Resources["BackgroundTertiary"]!;

        var border = new Border
        {
            BackgroundColor = backgroundTertiary,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
            Stroke = surfaceBorder,
            StrokeThickness = 1,
            Padding = new Thickness(10, 8),
            Margin = new Thickness(4, 2)
        };

        // Add context menu for copy
        var segmentIndex = index; // Capture for closure
        var contextFlyout = new MenuFlyout();
        var copyMenuItem = new MenuFlyoutItem
        {
            Text = L.Localize(StringKeys.Copy)
        };
        copyMenuItem.Clicked += async (s, e) =>
        {
            var textToCopy = segmentIndex < _currentSegments.Count
                ? _currentSegments[segmentIndex].Text.Trim()
                : segment.Text.Trim();
            await Clipboard.Default.SetTextAsync(textToCopy);
            ShowToast(L.Localize(StringKeys.Copied));
        };
        contextFlyout.Add(copyMenuItem);
        FlyoutBase.SetContextFlyout(border, contextFlyout);

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition(new GridLength(55)),
                new ColumnDefinition(GridLength.Star)
            },
            RowSpacing = 0
        };

        // Timestamp - tappable to seek
        var timeLabel = new Label
        {
            Text = FormatTimestamp(segment.Start),
            FontSize = 10,
            FontAttributes = FontAttributes.Bold,
            TextColor = textTertiary,
            VerticalOptions = LayoutOptions.Start,
            Padding = new Thickness(4, 6, 4, 2)
        };

        var timeTap = new TapGestureRecognizer();
        timeTap.Tapped += (s, e) => OnSegmentClicked(index);
        timeLabel.GestureRecognizers.Add(timeTap);

        grid.Children.Add(timeLabel);

        // Display Label - shown in non-edit mode (normal text, no cursor) - with dictation highlighting if enabled
        var segmentText = segment.Text.Trim();
        var textLabel = new Label
        {
            FontSize = _transcriptFontSize,
            LineBreakMode = LineBreakMode.WordWrap,
            VerticalOptions = LayoutOptions.Start,
            Padding = new Thickness(0, 4, 0, 2),
            IsVisible = !_isEditMode,
            ClassId = "displayLabel"
        };

        // Always use FormattedText for consistent line height
        if (_settingsService.EnableDictationFormatting)
        {
            textLabel.FormattedText = BuildHighlightedText(segmentText, textSecondary, (Color)Resources["AccentText"]!);
        }
        else
        {
            // Single span with no highlighting
            textLabel.FormattedText = new FormattedString
            {
                Spans = { new Span { Text = segmentText, TextColor = textSecondary } }
            };
        }

        Grid.SetColumn(textLabel, 1);
        grid.Children.Add(textLabel);

        // Edit Editor - shown in edit mode (editable textbox)
        var textEditor = new Editor
        {
            Text = segment.Text.Trim(),
            FontSize = _transcriptFontSize,
            TextColor = textPrimary,
            BackgroundColor = Colors.Transparent,
            AutoSize = EditorAutoSizeOption.TextChanges,
            VerticalOptions = LayoutOptions.Start,
            Margin = new Thickness(-5, 0, 0, 0),
            IsVisible = _isEditMode,
            ClassId = "editEditor"
        };

        // Remove Editor border styling on Windows and ensure text color
        var editorTextColor = textPrimary;
        textEditor.HandlerChanged += (s, e) =>
        {
#if WINDOWS
            if (textEditor.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.TextBox tb)
            {
                tb.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
                tb.Background = null;
                tb.Padding = new Microsoft.UI.Xaml.Thickness(0);
                // Ensure text is visible with proper color
                tb.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                    Windows.UI.Color.FromArgb(
                        (byte)(editorTextColor.Alpha * 255),
                        (byte)(editorTextColor.Red * 255),
                        (byte)(editorTextColor.Green * 255),
                        (byte)(editorTextColor.Blue * 255)));
            }
#endif
        };

        // Save changes when focus is lost
        textEditor.Unfocused += (s, e) =>
        {
            if (_isEditMode && index < _currentSegments.Count)
            {
                var currentText = _currentSegments[index].Text.Trim();
                var newText = textEditor.Text?.Trim() ?? "";

                if (newText != currentText && !string.IsNullOrEmpty(newText))
                {
                    UpdateSegmentText(index, newText);
                    // Sync to display label
                    textLabel.Text = newText;
                }
            }
        };

        // Update selection when editor is focused (in edit mode)
        textEditor.Focused += (s, e) =>
        {
            if (_isEditMode && index != _selectedSegmentIndex)
            {
                UpdateSegmentSelectionInEditMode(index);
            }
        };

        Grid.SetColumn(textEditor, 1);
        grid.Children.Add(textEditor);

        border.Content = grid;

        // Tap gesture for the whole segment
        var segmentTap = new TapGestureRecognizer();
        segmentTap.Tapped += (s, e) =>
        {
            if (_isEditMode)
            {
                // In edit mode: update selection and focus the editor
                if (index != _selectedSegmentIndex)
                {
                    UpdateSegmentSelectionInEditMode(index);
                }
                textEditor.Focus();
            }
            else
            {
                OnSegmentClicked(index);
            }
        };
        border.GestureRecognizers.Add(segmentTap);

        // Hover effect and custom tooltip
        var hover = new PointerGestureRecognizer();
        hover.PointerEntered += (s, e) =>
        {
            if (index != _currentHighlightedSegmentIndex && index != _selectedSegmentIndex)
            {
                border.Stroke = (Color)Resources["AccentMuted"]!;
            }
            OnSegmentPointerEntered(border, index, e);
        };
        hover.PointerMoved += (s, e) => OnSegmentPointerMoved(border, index, e);
        hover.PointerExited += (s, e) =>
        {
            OnSegmentPointerExited();
            if (index != _currentHighlightedSegmentIndex && index != _selectedSegmentIndex)
            {
                border.Stroke = (Color)Resources["SurfaceBorder"]!;
                border.BackgroundColor = _isEditMode
                    ? (Color)Resources["SurfaceColor"]!
                    : (Color)Resources["BackgroundTertiary"]!;
            }
        };
        border.GestureRecognizers.Add(hover);

        return (border, textEditor);
    }

    private void UpdateSegmentText(int index, string newText)
    {
        if (index < 0 || index >= _currentSegments.Count)
        {
            return;
        }

        var segment = _currentSegments[index];
        var updatedSegment = new AudioSegment(
            newText,
            segment.Language ?? "en",
            segment.Start,
            segment.End,
            segment.Confidence);

        _currentSegments[index] = updatedSegment;

        // Save changes to history
        SaveSegmentChanges();
    }

    /// <summary>
    /// Appends a single segment to the view without rebuilding - used during transcription
    /// </summary>
    public Border AppendSegmentView(LMKit.Speech.AudioSegment segment)
    {
        var index = _currentSegments.Count - 1;

        var textSecondary = (Color)Resources["TextSecondary"]!;
        var textTertiary = (Color)Resources["TextTertiary"]!;
        var surfaceBorder = (Color)Resources["SurfaceBorder"]!;
        var backgroundTertiary = (Color)Resources["BackgroundTertiary"]!;

        var border = new Border
        {
            BackgroundColor = backgroundTertiary,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
            Stroke = surfaceBorder,
            StrokeThickness = 1,
            Padding = new Thickness(10, 8),
            Margin = new Thickness(4, 2)
        };

        // Add context menu for copy
        var segmentText = segment.Text.Trim();
        var contextFlyout = new MenuFlyout();
        var copyMenuItem = new MenuFlyoutItem
        {
            Text = L.Localize(StringKeys.Copy),
            IconImageSource = null
        };
        copyMenuItem.Clicked += async (s, e) =>
        {
            await Clipboard.Default.SetTextAsync(segmentText);
            ShowToast(L.Localize(StringKeys.Copied));
        };
        contextFlyout.Add(copyMenuItem);
        FlyoutBase.SetContextFlyout(border, contextFlyout);

        // Custom tooltip on hover
        var segmentIndex = index; // Capture for closure
        var hover = new PointerGestureRecognizer();
        hover.PointerEntered += (s, e) => OnSegmentPointerEntered(border, segmentIndex, e);
        hover.PointerMoved += (s, e) => OnSegmentPointerMoved(border, segmentIndex, e);
        hover.PointerExited += (s, e) => OnSegmentPointerExited();
        border.GestureRecognizers.Add(hover);

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition(new GridLength(55)),
                new ColumnDefinition(GridLength.Star)
            },
            RowSpacing = 0
        };

        // Timestamp label (no interaction during transcription)
        var timeLabel = new Label
        {
            Text = FormatTimestamp(segment.Start),
            FontSize = 10,
            FontAttributes = FontAttributes.Bold,
            TextColor = textTertiary,
            VerticalOptions = LayoutOptions.Center,
            Padding = new Thickness(4, 2)
        };
        grid.Children.Add(timeLabel);

        // Text label (read-only during transcription) - with dictation highlighting if enabled
        var textLabel = new Label
        {
            FontSize = _transcriptFontSize,
            LineBreakMode = LineBreakMode.WordWrap,
            VerticalOptions = LayoutOptions.Center,
            Padding = new Thickness(0, 2)
        };

        // Always use FormattedText for consistent line height
        if (_settingsService.EnableDictationFormatting)
        {
            textLabel.FormattedText = BuildHighlightedText(segmentText, textSecondary, (Color)Resources["AccentText"]!);
        }
        else
        {
            // Single span with no highlighting
            textLabel.FormattedText = new FormattedString
            {
                Spans = { new Span { Text = segmentText, TextColor = textSecondary } }
            };
        }

        Grid.SetColumn(textLabel, 1);
        grid.Children.Add(textLabel);

        border.Content = grid;

        SegmentsList.Children.Add(border);
        _segmentViews.Add(border);

        return border;
    }

    /// <summary>
    /// Builds a FormattedString with dictation commands highlighted.
    /// </summary>
    private FormattedString BuildHighlightedText(string text, Color normalColor, Color highlightColor)
    {
        var formatted = new FormattedString();

        var matches = Formatter.FindCommandMatches(text);

        if (matches.Count == 0)
        {
            formatted.Spans.Add(new Span { Text = text, TextColor = normalColor, FontSize = _transcriptFontSize });
            return formatted;
        }

        int currentPos = 0;
        foreach (var match in matches)
        {
            // Add text before match
            if (match.StartIndex > currentPos)
            {
                formatted.Spans.Add(new Span
                {
                    Text = text.Substring(currentPos, match.StartIndex - currentPos),
                    TextColor = normalColor,
                    FontSize = _transcriptFontSize
                });
            }

            // Add highlighted match
            formatted.Spans.Add(new Span
            {
                Text = text.Substring(match.StartIndex, match.Length),
                TextColor = highlightColor,
                FontAttributes = FontAttributes.Bold,
                FontSize = _transcriptFontSize
            });

            currentPos = match.StartIndex + match.Length;
        }

        // Add remaining text
        if (currentPos < text.Length)
        {
            formatted.Spans.Add(new Span
            {
                Text = text.Substring(currentPos),
                TextColor = normalColor,
                FontSize = _transcriptFontSize
            });
        }

        return formatted;
    }

    private void OnSegmentClicked(int index)
    {
        if (index < 0 || index >= _currentSegments.Count)
        {
            return;
        }

        var now = DateTime.Now;
        var timeSinceLastClick = (now - _lastSegmentClickTime).TotalMilliseconds;

        // Second click within 400ms = double-click, use FIRST clicked segment
        if (timeSinceLastClick < 400 && _lastClickedSegmentIndex >= 0)
        {
            int targetIndex = _lastClickedSegmentIndex;
            _lastClickedSegmentIndex = -1;
            _lastSegmentClickTime = DateTime.MinValue;

            _userClickLockUntil = now.AddMilliseconds(500);
            _userLockedSegmentIndex = targetIndex;
            SeekToSegment(targetIndex);
            return;
        }

        // First click
        _lastClickedSegmentIndex = index;
        _lastSegmentClickTime = now;

        _userClickLockUntil = now.AddMilliseconds(500);
        _userLockedSegmentIndex = index;
        SelectSegment(index);
    }

    private void SelectSegment(int index)
    {
        if (index < 0 || index >= _currentSegments.Count)
        {
            return;
        }

        _userClickLockUntil = DateTime.Now.AddMilliseconds(500);
        _userLockedSegmentIndex = index;

        var segment = _currentSegments[index];

        // Update selection visual
        UpdateSegmentSelection(index);

        // Position the playback slider without playing
        if (HistoryPlaybackPanel.IsVisible && _historyAudioPlayer.TotalDuration.TotalSeconds > 0)
        {
            // Set flag to prevent position changed handler from interfering
            _isManuallySelectingSegment = true;

            // Add 1ms to prevent rounding issues at segment boundaries
            var seekPosition = segment.Start + TimeSpan.FromMilliseconds(1);
            _historyAudioPlayer.Seek(seekPosition);

            var progress = segment.Start.TotalSeconds / _historyAudioPlayer.TotalDuration.TotalSeconds * 100;
            HistoryPlaybackSlider.Value = progress;
            HistoryCurrentTimeLabel.Text = TranscriptExporter.FormatDisplayTime(segment.Start);

            _isManuallySelectingSegment = false;
        }
    }

    private void UpdateSegmentSelection(int newIndex)
    {
        var accentPrimary = (Color)Resources["AccentPrimary"]!;
        var accentSurface = (Color)Resources["AccentSurface"]!;
        var textPrimary = (Color)Resources["TextPrimary"]!;
        var textSecondary = (Color)Resources["TextSecondary"]!;
        var surfaceBorder = (Color)Resources["SurfaceBorder"]!;
        var backgroundTertiary = (Color)Resources["BackgroundTertiary"]!;

        // Deselect previous (always, unless it's the same as new)
        if (_selectedSegmentIndex >= 0 && _selectedSegmentIndex < _segmentViews.Count &&
            _selectedSegmentIndex != newIndex)
        {
            var prevBorder = _segmentViews[_selectedSegmentIndex];
            prevBorder.BackgroundColor = backgroundTertiary;
            prevBorder.Stroke = surfaceBorder;
            prevBorder.StrokeThickness = 1;
            prevBorder.Padding = new Thickness(10, 8);
            SetSegmentStyle(prevBorder, textSecondary, _transcriptFontSize);
        }

        // Select new - stronger style with accent background, primary border, and bigger text
        _selectedSegmentIndex = newIndex;

        if (newIndex >= 0 && newIndex < _segmentViews.Count)
        {
            var newBorder = _segmentViews[newIndex];
            newBorder.BackgroundColor = accentSurface;
            newBorder.Stroke = accentPrimary;
            newBorder.StrokeThickness = 2;
            newBorder.Padding = new Thickness(12, 10);
            SetSegmentStyle(newBorder, textPrimary, _transcriptFontSize + 2);
        }
    }

    /// <summary>
    /// Updates segment selection while in edit mode. Uses edit mode styling.
    /// </summary>
    private void UpdateSegmentSelectionInEditMode(int newIndex)
    {
        var accentPrimary = (Color)Resources["AccentPrimary"]!;
        var accentSurface = (Color)Resources["AccentSurface"]!;
        var surfaceBorder = (Color)Resources["SurfaceBorder"]!;
        var surfaceColor = (Color)Resources["SurfaceColor"]!;

        // Deselect previous
        if (_selectedSegmentIndex >= 0 && _selectedSegmentIndex < _segmentViews.Count &&
            _selectedSegmentIndex != newIndex)
        {
            var prevBorder = _segmentViews[_selectedSegmentIndex];
            prevBorder.BackgroundColor = surfaceColor;
            prevBorder.Stroke = surfaceBorder;
            prevBorder.StrokeThickness = 1;
            prevBorder.Padding = new Thickness(10, 8);
        }

        // Select new
        _selectedSegmentIndex = newIndex;

        if (newIndex >= 0 && newIndex < _segmentViews.Count)
        {
            var newBorder = _segmentViews[newIndex];
            newBorder.BackgroundColor = accentSurface;
            newBorder.Stroke = accentPrimary;
            newBorder.StrokeThickness = 2;
            newBorder.Padding = new Thickness(12, 10);
        }
    }

    private void SaveSegmentChanges()
    {
        if (string.IsNullOrEmpty(_currentRecordId) || _currentSegments == null)
        {
            return;
        }

        var record = _historyService.GetById(_currentRecordId);
        if (record == null)
        {
            return;
        }

        // Update segments - TranscriptText and WordCount are now computed from segments
        record.Segments = _currentSegments.ToList();

        _historyService.Update(record);

        // Update UI to reflect changes
        MainThread.BeginInvokeOnMainThread(() =>
        {
            // Update word count in stats - use formatted word count if dictation enabled
            var wordCount = CalculateFormattedWordCount(_currentSegments, _settingsService.EnableDictationFormatting);
            WordCountLabel.Text = L.Localize(StringKeys.Words, wordCount);
            SegmentCountLabel.Text = L.Localize(StringKeys.Segments, _currentSegments.Count);

            // Refresh history list to show updated preview text
            RefreshHistoryList();
        });
    }

    private void SeekToSegment(int segmentIndex)
    {
        if (segmentIndex < 0 || segmentIndex >= _currentSegments.Count)
        {
            return;
        }

        _userClickLockUntil = DateTime.Now.AddMilliseconds(500);
        _userLockedSegmentIndex = segmentIndex;

        var segment = _currentSegments[segmentIndex];
        // Add 1ms to prevent rounding issues at segment boundaries
        var seekPosition = segment.Start + TimeSpan.FromMilliseconds(1);
        _historyAudioPlayer.Seek(seekPosition);

        if (!_historyAudioPlayer.IsPlaying)
        {
            _historyAudioPlayer.Play();
            _historyPlaybackTimer?.Start();
            HistoryPlayPauseIcon.Text = "▌▌";
        }

        UpdateSegmentHighlight(segmentIndex);
    }

    private int _userLockedSegmentIndex = -1;
    private DateTime _userClickLockUntil = DateTime.MinValue;

    private void UpdatePlaybackHighlight(TimeSpan position)
    {
        if (_currentSegments == null || _currentSegments.Count == 0 || _segmentViews.Count == 0)
        {
            return;
        }

        // Time-based lock: ignore position updates for 500ms after user click
        if (DateTime.Now < _userClickLockUntil && _userLockedSegmentIndex >= 0)
        {
            if (_userLockedSegmentIndex != _currentHighlightedSegmentIndex)
            {
                UpdateSegmentHighlight(_userLockedSegmentIndex);
            }
            return;
        }

        // Lock expired, clear it
        _userLockedSegmentIndex = -1;

        // Use actual playback position - no artificial compensation
        int newIndex = -1;
        for (int i = 0; i < _currentSegments.Count; i++)
        {
            var seg = _currentSegments[i];
            if (position >= seg.Start && position < seg.End)
            {
                newIndex = i;
                break;
            }
        }

        if (newIndex == -1)
        {
            if (_currentSegments.Count > 0 && position < _currentSegments[0].Start)
            {
                newIndex = 0;
            }
            else
            {
                for (int i = 0; i < _currentSegments.Count; i++)
                {
                    if (position > _currentSegments[i].End)
                    {
                        if (i + 1 < _currentSegments.Count)
                        {
                            if (position < _currentSegments[i + 1].Start)
                            {
                                newIndex = i;
                                break;
                            }
                        }
                        else
                        {
                            newIndex = i;
                            break;
                        }
                    }
                }
            }
        }

        if (newIndex != _currentHighlightedSegmentIndex && newIndex >= 0)
        {
            UpdateSegmentHighlight(newIndex);
        }
    }

    private void UpdateSegmentHighlight(int newIndex)
    {
        var accentPrimary = (Color)Resources["AccentPrimary"]!;
        var accentSurface = (Color)Resources["AccentSurface"]!;
        var textPrimary = (Color)Resources["TextPrimary"]!;
        var textSecondary = (Color)Resources["TextSecondary"]!;
        var surfaceBorder = (Color)Resources["SurfaceBorder"]!;
        var backgroundTertiary = (Color)Resources["BackgroundTertiary"]!;

        // Unhighlight previous - always reset to normal during playback
        if (_currentHighlightedSegmentIndex >= 0 &&
            _currentHighlightedSegmentIndex < _segmentViews.Count &&
            _currentHighlightedSegmentIndex != newIndex)
        {
            var prevBorder = _segmentViews[_currentHighlightedSegmentIndex];
            prevBorder.BackgroundColor = backgroundTertiary;
            prevBorder.Stroke = surfaceBorder;
            prevBorder.StrokeThickness = 1;
            prevBorder.Padding = new Thickness(10, 8);
            SetSegmentStyle(prevBorder, textSecondary, _transcriptFontSize);
        }

        // Highlight new - bigger text and padding for emphasis
        _currentHighlightedSegmentIndex = newIndex;
        _selectedSegmentIndex = newIndex;

        if (newIndex >= 0 && newIndex < _segmentViews.Count)
        {
            var newBorder = _segmentViews[newIndex];
            newBorder.BackgroundColor = accentSurface;
            newBorder.Stroke = accentPrimary;
            newBorder.StrokeThickness = 2;
            newBorder.Padding = new Thickness(12, 10);
            SetSegmentStyle(newBorder, textPrimary, _transcriptFontSize + 2);

            // Only auto-scroll during automatic playback progression, not user clicks
            if (DateTime.Now >= _userClickLockUntil)
            {
                ScrollToCurrentSegment(newBorder);
            }
        }
    }

    private void SetSegmentStyle(Border segmentBorder, Color textColor, double fontSize)
    {
        if (segmentBorder.Content is Grid grid)
        {
            foreach (var child in grid.Children)
            {
                if (Grid.GetColumn((BindableObject)child) == 1)
                {
                    if (child is Label label && label.ClassId == "displayLabel")
                    {
                        label.TextColor = textColor;
                        label.FontSize = fontSize;
                        // Also update FormattedText spans (for dictation highlighting)
                        if (label.FormattedText != null)
                        {
                            foreach (var span in label.FormattedText.Spans)
                            {
                                span.FontSize = fontSize;
                                // Only update color for non-highlighted spans (those without bold)
                                if (span.FontAttributes != FontAttributes.Bold)
                                {
                                    span.TextColor = textColor;
                                }
                            }
                        }
                    }
                    else if (child is Editor editor && editor.ClassId == "editEditor")
                    {
                        editor.TextColor = textColor;
                        editor.FontSize = fontSize;
                    }
                }
            }
        }
    }

    private void SetSegmentTextColor(Border segmentBorder, Color color)
    {
        if (segmentBorder.Content is Grid grid)
        {
            foreach (var child in grid.Children)
            {
                if (Grid.GetColumn((BindableObject)child) == 1)
                {
                    if (child is Label label && label.ClassId == "displayLabel")
                    {
                        label.TextColor = color;
                        // Also update FormattedText spans (for dictation highlighting)
                        if (label.FormattedText != null)
                        {
                            foreach (var span in label.FormattedText.Spans)
                            {
                                // Only update color for non-highlighted spans (those without bold)
                                if (span.FontAttributes != FontAttributes.Bold)
                                {
                                    span.TextColor = color;
                                }
                            }
                        }
                    }
                    else if (child is Editor editor && editor.ClassId == "editEditor")
                    {
                        editor.TextColor = color;
                    }
                }
            }
        }
    }

    public void ClearSegmentHighlight()
    {
        if (_currentHighlightedSegmentIndex >= 0 && _currentHighlightedSegmentIndex < _segmentViews.Count)
        {
            var border = _segmentViews[_currentHighlightedSegmentIndex];

            // If this segment is also selected, keep selection style
            if (_currentHighlightedSegmentIndex == _selectedSegmentIndex)
            {
                border.BackgroundColor = (Color)Resources["AccentSurface"]!;
                border.Stroke = (Color)Resources["AccentPrimary"]!;
                border.StrokeThickness = 2;
                border.Padding = new Thickness(12, 10);
                SetSegmentStyle(border, (Color)Resources["TextPrimary"]!, _transcriptFontSize + 2);
            }
            else
            {
                border.BackgroundColor = (Color)Resources["BackgroundTertiary"]!;
                border.Stroke = (Color)Resources["SurfaceBorder"]!;
                border.StrokeThickness = 1;
                border.Padding = new Thickness(10, 8);
                SetSegmentStyle(border, (Color)Resources["TextSecondary"]!, _transcriptFontSize);
            }
        }
        _currentHighlightedSegmentIndex = -1;
        _userLockedSegmentIndex = -1;
    }

    public void ClearSegmentSelection()
    {
        if (_selectedSegmentIndex >= 0 && _selectedSegmentIndex < _segmentViews.Count &&
            _selectedSegmentIndex != _currentHighlightedSegmentIndex)
        {
            var border = _segmentViews[_selectedSegmentIndex];
            border.BackgroundColor = (Color)Resources["BackgroundTertiary"]!;
            border.Stroke = (Color)Resources["SurfaceBorder"]!;
            border.StrokeThickness = 1;
            border.Padding = new Thickness(10, 8);
            SetSegmentStyle(border, (Color)Resources["TextSecondary"]!, _transcriptFontSize);
        }
        _selectedSegmentIndex = -1;
    }

    public void UpdateSegmentFontSize()
    {
        // Update font size on both Label and Editor elements in segments view
        foreach (var border in _segmentViews)
        {
            if (border.Content is Grid grid)
            {
                foreach (var child in grid.Children)
                {
                    if (Grid.GetColumn((BindableObject)child) == 1)
                    {
                        if (child is Label label && label.ClassId == "displayLabel")
                        {
                            label.FontSize = _transcriptFontSize;
                            // Also update FormattedText spans (for dictation highlighting)
                            if (label.FormattedText != null)
                            {
                                foreach (var span in label.FormattedText.Spans)
                                {
                                    span.FontSize = _transcriptFontSize;
                                }
                            }
                        }
                        else if (child is Editor editor && editor.ClassId == "editEditor")
                        {
                            editor.FontSize = _transcriptFontSize;
                        }
                    }
                }
            }
        }

        // Also update DocumentLabel font size for document view
        DocumentLabel.FontSize = _transcriptFontSize;
    }

    public void UpdateSegmentEditability()
    {
        var textPrimary = (Color)Resources["TextPrimary"]!;
        var textSecondary = (Color)Resources["TextSecondary"]!;
        var backgroundTertiary = (Color)Resources["BackgroundTertiary"]!;
        var surfaceColor = (Color)Resources["SurfaceColor"]!;
        var accentSurface = (Color)Resources["AccentSurface"]!;
        var accentPrimary = (Color)Resources["AccentPrimary"]!;
        var surfaceBorder = (Color)Resources["SurfaceBorder"]!;

        bool hasChanges = false;

        // Toggle visibility between Label and Editor for each segment
        for (int i = 0; i < _segmentViews.Count; i++)
        {
            var border = _segmentViews[i];
            var isSelected = i == _selectedSegmentIndex;

            if (border.Content is Grid grid)
            {
                Label? displayLabel = null;
                Editor? editEditor = null;

                foreach (var child in grid.Children)
                {
                    if (Grid.GetColumn((BindableObject)child) == 1)
                    {
                        if (child is Label label && label.ClassId == "displayLabel")
                        {
                            displayLabel = label;
                        }
                        else if (child is Editor editor && editor.ClassId == "editEditor")
                        {
                            editEditor = editor;
                        }
                    }
                }

                if (displayLabel != null && editEditor != null)
                {
                    if (_isEditMode)
                    {
                        // Entering edit mode: sync text from label to editor
                        // Get text from either Text property or FormattedText spans
                        var labelText = displayLabel.Text;
                        if (string.IsNullOrEmpty(labelText) && displayLabel.FormattedText != null)
                        {
                            var sb = new System.Text.StringBuilder();
                            foreach (var span in displayLabel.FormattedText.Spans)
                            {
                                sb.Append(span.Text);
                            }
                            labelText = sb.ToString();
                        }
                        editEditor.Text = labelText;
                        displayLabel.IsVisible = false;
                        editEditor.IsVisible = true;
                    }
                    else
                    {
                        // Exiting edit mode: check for changes and save
                        var editorText = editEditor.Text?.Trim() ?? "";
                        var currentText = i < _currentSegments.Count ? _currentSegments[i].Text.Trim() : "";

                        if (!string.IsNullOrEmpty(editorText) && editorText != currentText)
                        {
                            // Update the segment
                            if (i < _currentSegments.Count)
                            {
                                var segment = _currentSegments[i];
                                _currentSegments[i] = new AudioSegment(
                                    editorText,
                                    segment.Language ?? "en",
                                    segment.Start,
                                    segment.End,
                                    segment.Confidence);
                                hasChanges = true;
                            }
                        }

                        // Sync text from editor to label - always use FormattedText for consistent rendering
                        var labelColor = isSelected ? textPrimary : textSecondary;
                        displayLabel.Text = null;
                        if (_settingsService.EnableDictationFormatting)
                        {
                            displayLabel.FormattedText = BuildHighlightedText(editorText, labelColor, (Color)Resources["AccentText"]!);
                        }
                        else
                        {
                            // Single span with no highlighting
                            displayLabel.FormattedText = new FormattedString
                            {
                                Spans = { new Span { Text = editorText, TextColor = labelColor } }
                            };
                        }
                        editEditor.IsVisible = false;
                        displayLabel.IsVisible = true;
                    }
                }
            }

            // Update border styling - preserve selection styling
            if (isSelected)
            {
                border.BackgroundColor = accentSurface;
                border.Stroke = accentPrimary;
                border.StrokeThickness = 2;
                border.Padding = new Thickness(12, 10);
            }
            else
            {
                border.BackgroundColor = _isEditMode ? surfaceColor : backgroundTertiary;
                border.Stroke = surfaceBorder;
                border.StrokeThickness = 1;
                border.Padding = new Thickness(10, 8);
            }
        }

        // Save all changes when exiting edit mode
        if (!_isEditMode && hasChanges)
        {
            SaveSegmentChanges();
        }
    }

    /// <summary>
    /// Scrolls to the last segment - used during transcription for auto-scroll
    /// </summary>
    public async void ScrollToLastSegment()
    {
        try
        {
            if (_segmentViews.Count > 0)
            {
                var lastBorder = _segmentViews[_segmentViews.Count - 1];
                await SegmentsScrollView.ScrollToAsync(lastBorder, ScrollToPosition.End, false);
            }
        }
        catch
        {
            // Ignore scroll errors
        }
    }

    private async void ScrollToCurrentSegment(Border segmentBorder)
    {
        try
        {
            await SegmentsScrollView.ScrollToAsync(segmentBorder, ScrollToPosition.Center, true);
        }
        catch
        {
            // Ignore scroll errors
        }
    }

    private string FormatTimestamp(TimeSpan time)
    {
        return time.TotalHours >= 1
            ? time.ToString(@"h\:mm\:ss")
            : time.ToString(@"m\:ss");
    }

    #endregion
}
