using LynxTranscribe.Helpers;
using LynxTranscribe.Localization;
using LynxTranscribe.Services;
using L = LynxTranscribe.Localization.LocalizationService;

namespace LynxTranscribe;

/// <summary>
/// Audio recording: Countdown, recording state, input devices
/// </summary>
public partial class MainPage
{
    #region Audio Recording

    private List<AudioInputDevice> _inputDevices = new();

    private void PopulateInputDevices()
    {
        try
        {
#if WINDOWS
            _inputDevices = AudioRecorderService.GetInputDevices();
#elif MACCATALYST
            _inputDevices = MacAudioRecorderService.GetInputDevices();
#else
            _inputDevices = new List<AudioInputDevice>();
#endif
            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    _hasInputDevices = _inputDevices.Count > 0;

                    if (_hasInputDevices)
                    {
                        if (_selectedInputDeviceId >= _inputDevices.Count)
                        {
                            _selectedInputDeviceId = 0;
                        }

                        InputDeviceLabel.Text = _inputDevices[_selectedInputDeviceId].Name;
                        RecordButton.IsEnabled = true;
                        RecordButton.Opacity = 1.0;
                    }
                    else
                    {
                        InputDeviceLabel.Text = "No microphone found";
                        RecordButton.IsEnabled = false;
                        RecordButton.Opacity = 0.5;
                        RecordButtonLabel.Text = L.Localize(StringKeys.NoMic);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error updating input device UI: {ex.Message}");
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error populating input devices: {ex.Message}");
            _hasInputDevices = false;
            MainThread.BeginInvokeOnMainThread(() =>
            {
                InputDeviceLabel.Text = "No microphone found";
                RecordButton.IsEnabled = false;
                RecordButton.Opacity = 0.5;
                RecordButtonLabel.Text = L.Localize(StringKeys.NoMic);
            });
        }
    }

    private async void OnInputDeviceDropdownTapped(object? sender, TappedEventArgs e)
    {
        try
        {
            if (_inputDevices.Count == 0)
            {
                await DisplayAlert("No Devices", "No audio input devices found.", "OK");
                return;
            }

            var deviceNames = _inputDevices.Select(d => d.Name).ToArray();
            var result = await DisplayActionSheet("Select Input Device", "Cancel", null, deviceNames);

            if (result != null && result != "Cancel")
            {
                var index = Array.IndexOf(deviceNames, result);
                if (index >= 0)
                {
                    _selectedInputDeviceId = index;
                    _settingsService.InputDeviceId = _selectedInputDeviceId;
                    InputDeviceLabel.Text = result;
                    UpdateAudioInputStatus();
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OnInputDeviceDropdownTapped error: {ex.Message}");
        }
    }

    private async void OnRecordClicked(object? sender, TappedEventArgs e)
    {
        if (_isRecording || _isCountingDown)
        {
            StopRecording();
        }
        else
        {
            await StartRecordingWithCountdown();
        }
    }

    private void OnStopRecordingClicked(object? sender, TappedEventArgs e)
    {
        StopRecording();
    }

    private async Task StartRecordingWithCountdown()
    {
        if (_isTranscribing || _isRecording || _isCountingDown || !_hasInputDevices)
        {
            return;
        }

        _isCountingDown = true;
        _skipCountdown = false;

        try
        {
            var deviceName = _selectedInputDeviceId >= 0 && _selectedInputDeviceId < _inputDevices.Count
                ? _inputDevices[_selectedInputDeviceId].Name
                : L.Localize(StringKeys.Default);
            RecordingInputLabel.Text = L.Localize(StringKeys.UsingMicrophone, deviceName);

            DropZone.IsVisible = false;
            RecordingPanel.IsVisible = true;
            CountdownPanel.IsVisible = true;
            RecordingActivePanel.IsVisible = false;
            RecordButtonLabel.Text = L.Localize(StringKeys.Cancel);
            BrowseButton.IsEnabled = false;
            BrowseButton.Opacity = 0.5;

            for (int i = AppConstants.Recording.CountdownSeconds; i >= 1 && !_skipCountdown && !_isClosing; i--)
            {
                if (!_isCountingDown || _isClosing) { ResetRecordingUI(); return; }

                CountdownLabel.Text = i.ToString();
                CountdownLabel.Scale = 1.2;
                try { await CountdownLabel.ScaleToAsync(1.0, AppConstants.Timing.AnimationDurationMs, Easing.CubicOut); } catch { }

                // Check skip flag during delay
                for (int j = 0; j < AppConstants.Recording.CountdownCheckIntervals && !_skipCountdown && !_isClosing; j++)
                {
                    await Task.Delay(AppConstants.Timing.CountdownStepDelayMs);
                    if (!_isCountingDown || _isClosing) { ResetRecordingUI(); return; }
                }
            }

            if (_isClosing) { ResetRecordingUI(); return; }

            _isCountingDown = false;

            _audioRecorder.StartRecording(_selectedInputDeviceId);
            _isRecording = true;

            CountdownPanel.IsVisible = false;
            RecordingActivePanel.IsVisible = true;
            RecordButtonLabel.Text = L.Localize(StringKeys.Stop);
            RecordingDuration.Text = "00:00";
            AudioLevelBar.WidthRequest = 0;

            StartRecordingDotAnimation();
            ShowToast(L.Localize(StringKeys.RecordingStarted), ToastType.Info);
        }
        catch (Exception ex)
        {
            _isCountingDown = false;
            _skipCountdown = false;
            ResetRecordingUI();
            ShowToast(L.Localize(StringKeys.FailedToStartRecording, ex.Message), ToastType.Error);
        }
    }

    private bool _skipCountdown = false;

    private void OnCountdownSkipClicked(object? sender, TappedEventArgs e)
    {
        if (_isCountingDown && !_skipCountdown)
        {
            _skipCountdown = true;
        }
    }

    private void ResetRecordingUI()
    {
        RecordingPanel.IsVisible = false;
        CountdownPanel.IsVisible = false;
        RecordingActivePanel.IsVisible = false;
        DropZone.IsVisible = true;
        RecordButtonLabel.Text = L.Localize(StringKeys.Record);
        BrowseButton.IsEnabled = true;
        BrowseButton.Opacity = 1.0;
    }

    private void StopRecording()
    {
        if (_isCountingDown)
        {
            _isCountingDown = false;
            ResetRecordingUI();
            ShowToast(L.Localize(StringKeys.RecordingCancelled), ToastType.Info);
            return;
        }

        if (!_isRecording)
        {
            return;
        }

        _isRecording = false;
        StopRecordingDotAnimation();
        var filePath = _audioRecorder.StopRecording();
        ResetRecordingUI();
    }

    private void OnRecordingDurationChanged(object? sender, TimeSpan duration)
    {
        if (_isClosing)
        {
            return;
        }

        try { MainThread.BeginInvokeOnMainThread(() => { if (!_isClosing) { RecordingDuration.Text = duration.ToString(@"mm\:ss"); } }); }
        catch { /* App closing */ }
    }

    private void OnRecordingLevelChanged(object? sender, float level)
    {
        if (_isClosing)
        {
            return;
        }

        try { MainThread.BeginInvokeOnMainThread(() => { if (!_isClosing) { AudioLevelBar.WidthRequest = level * AppConstants.Layout.AudioLevelBarMaxWidth; } }); }
        catch { /* App closing */ }
    }

    private void OnRecordingCompleted(object? sender, string filePath)
    {
        if (_isClosing)
        {
            return;
        }

        try
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                if (_isClosing)
                {
                    return;
                }

                if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                {
                    ShowToast(L.Localize(StringKeys.RecordingSaved), ToastType.Success);

                    _selectedFilePath = filePath;
                    // DO NOT clear _currentRecordId or _currentSegments - keep history state intact

                    // Show loading state immediately
                    var fileName = System.IO.Path.GetFileName(filePath);
                    var fileInfo = new FileInfo(filePath);
                    SelectedFileName.Text = fileName;
                    SelectedFileInfo.Text = $"{FormatFileSize(fileInfo.Length)} · {L.Localize(StringKeys.Loading)}";

                    EmptyState.IsVisible = false;
                    FileSelectedState.IsVisible = true;
                    ClearButton.IsVisible = true;

                    // DO NOT touch transcript UI - it's managed by tab switching
                    // The right panel shows empty state when on Audio tab (handled by HideTranscriptUI)

                    // Update file selection dependent UI (drop zone, step badges)
                    UpdateFileSelectionUI();

                    if (_isClosing)
                    {
                        return;
                    }

                    // Run heavy operations in parallel
                    var loadAudioTask = LoadAudioFile(filePath);
                    var loadPlayerTask = _audioPlayer.LoadAsync(filePath);
                    var waveformTask = GenerateWaveformAsync(filePath);

                    // Wait for LMKit audio to load
                    await loadAudioTask;
                    if (_isClosing)
                    {
                        return;
                    }

                    // Update duration display
                    var durationText = _lmKitService.LoadedAudio != null ? $" · {_lmKitService.LoadedAudio.Duration:mm\\:ss}" : "";
                    SelectedFileInfo.Text = $"{FormatFileSize(fileInfo.Length)}{durationText}";

                    // Wait for player to be ready
                    if (await loadPlayerTask && !_isClosing)
                    {
                        PlayerPanel.IsVisible = true;
                        TotalTimeLabel.Text = TranscriptExporter.FormatDisplayTime(_audioPlayer.TotalDuration);
                        CurrentTimeLabel.Text = "0:00";
                        PlaybackSlider.Value = 0;
                        PlayPauseIcon.Text = "▶";
                    }

                    // Wait for waveform
                    await waveformTask;
                    if (_isClosing)
                    {
                        return;
                    }

                    UpdateTranscribeButtonState();

                    // Auto-transcribe if enabled
                    if (_settingsService.AutoTranscribeOnImport && _lmKitService.HasLoadedAudio && !_isTranscribing)
                    {
                        OnTranscribeClicked(null, EventArgs.Empty);
                    }
                }
            });
        }
        catch { /* App closing */ }
    }

    private void OnRecordingError(object? sender, string error)
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

                _isRecording = false;
                StopRecordingDotAnimation();
                ResetRecordingUI();
                ShowToast(L.Localize(StringKeys.RecordingError, error), ToastType.Error);
            });
        }
        catch { /* App closing */ }
    }

    private void StartRecordingDotAnimation()
    {
        _recordingDotTimer = Dispatcher.CreateTimer();
        _recordingDotTimer.Interval = TimeSpan.FromMilliseconds(AppConstants.Timing.RecordingDotBlinkMs);
        _recordingDotTimer.Tick += (s, e) => RecordingDot.Opacity = RecordingDot.Opacity > 0.5 ? 0.3 : 1.0;
        _recordingDotTimer.Start();
    }

    private void StopRecordingDotAnimation()
    {
        _recordingDotTimer?.Stop();
        _recordingDotTimer = null;
        RecordingDot.Opacity = 1.0;
    }

    private void OnStopRecordingHoverEnter(object? sender, PointerEventArgs e)
    {
        StopRecordingButton.BackgroundColor = Color.FromArgb("#DC2626");
    }

    private void OnStopRecordingHoverExit(object? sender, PointerEventArgs e)
    {
        StopRecordingButton.BackgroundColor = Color.FromArgb("#EF4444");
    }

    #endregion
}
