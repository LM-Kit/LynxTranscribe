#if WINDOWS
using LynxTranscribe.Helpers;
using NAudio.Wave;

namespace LynxTranscribe.Services;

/// <summary>
/// Service for audio playback with position tracking and speed control.
/// </summary>
public class AudioPlayerService : IDisposable
{
    private WaveOutEvent? _waveOut;
    private AudioFileReader? _audioReader;
    private VarispeedSampleProvider? _speedProvider;
    private string? _currentFilePath;
    private bool _isDisposed;
    private float _playbackSpeed = 1.0f;

    // Track actual playback position accounting for buffering
    private DateTime _playStartTime;
    private TimeSpan _playStartPosition;
    private bool _isTrackingPlayback = false;

    public event EventHandler<TimeSpan>? PositionChanged;
    public event EventHandler? PlaybackStopped;

    /// <summary>
    /// Gets whether audio is currently playing.
    /// </summary>
    public bool IsPlaying => _waveOut?.PlaybackState == PlaybackState.Playing;

    /// <summary>
    /// Gets whether audio is paused.
    /// </summary>
    public bool IsPaused => _waveOut?.PlaybackState == PlaybackState.Paused;

    /// <summary>
    /// Gets whether audio is stopped.
    /// </summary>
    public bool IsStopped => _waveOut?.PlaybackState == PlaybackState.Stopped || _waveOut == null;

    /// <summary>
    /// Gets the current playback position.
    /// Uses time-based tracking for accuracy during playback.
    /// </summary>
    public TimeSpan CurrentPosition
    {
        get
        {
            if (_audioReader == null)
            {
                return TimeSpan.Zero;
            }

            // When playing, calculate position based on elapsed time (more accurate)
            if (_isTrackingPlayback && IsPlaying)
            {
                var elapsed = DateTime.UtcNow - _playStartTime;
                var adjustedElapsed = TimeSpan.FromTicks((long)(elapsed.Ticks * _playbackSpeed));
                var calculatedPosition = _playStartPosition + adjustedElapsed;

                // Clamp to valid range
                if (calculatedPosition < TimeSpan.Zero)
                {
                    return TimeSpan.Zero;
                }

                if (calculatedPosition > _audioReader.TotalTime)
                {
                    return _audioReader.TotalTime;
                }

                return calculatedPosition;
            }

            // When not playing, use the reader position
            return _audioReader.CurrentTime;
        }
    }

    /// <summary>
    /// Gets the total duration of the loaded audio.
    /// </summary>
    public TimeSpan TotalDuration => _audioReader?.TotalTime ?? TimeSpan.Zero;

    /// <summary>
    /// Gets or sets the playback volume (0.0 to 1.0).
    /// </summary>
    public float Volume
    {
        get => _waveOut?.Volume ?? 1.0f;
        set
        {
            if (_waveOut != null)
            {
                _waveOut.Volume = Math.Clamp(value, 0f, 1f);
            }
        }
    }

    /// <summary>
    /// Gets the current playback speed.
    /// </summary>
    public double PlaybackSpeed => _playbackSpeed;

    /// <summary>
    /// Loads an audio file for playback.
    /// </summary>
    public bool Load(string filePath)
    {
        try
        {
            // Ensure clean state
            Stop();
            DisposeAudio();

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                System.Diagnostics.Debug.WriteLine($"AudioPlayerService.Load: File not found: {filePath}");
                return false;
            }

            _audioReader = new AudioFileReader(filePath);
            _speedProvider = new VarispeedSampleProvider(_audioReader.ToSampleProvider());
            _speedProvider.Speed = _playbackSpeed;

            _waveOut = new WaveOutEvent();
            _waveOut.Init(_speedProvider);
            _waveOut.PlaybackStopped += OnPlaybackStopped;
            _currentFilePath = filePath;

            System.Diagnostics.Debug.WriteLine($"AudioPlayerService.Load: Success - {filePath}, Duration: {_audioReader.TotalTime}");
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AudioPlayerService.Load: Exception - {ex.Message}");
            DisposeAudio();
            return false;
        }
    }

    /// <summary>
    /// Loads an audio file for playback asynchronously (non-blocking).
    /// </summary>
    public async Task<bool> LoadAsync(string filePath)
    {
        try
        {
            // Ensure clean state on UI thread
            Stop();
            DisposeAudio();

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                System.Diagnostics.Debug.WriteLine($"AudioPlayerService.LoadAsync: File not found: {filePath}");
                return false;
            }

            // Load audio reader on background thread (this is the slow part)
            var reader = await Task.Run(() => new AudioFileReader(filePath));

            // Initialize playback on UI thread
            _audioReader = reader;
            _speedProvider = new VarispeedSampleProvider(_audioReader.ToSampleProvider());
            _speedProvider.Speed = _playbackSpeed;

            _waveOut = new WaveOutEvent();
            _waveOut.Init(_speedProvider);
            _waveOut.PlaybackStopped += OnPlaybackStopped;
            _currentFilePath = filePath;

            System.Diagnostics.Debug.WriteLine($"AudioPlayerService.LoadAsync: Success - {filePath}, Duration: {_audioReader.TotalTime}");
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AudioPlayerService.LoadAsync: Exception - {ex.Message}");
            DisposeAudio();
            return false;
        }
    }

    /// <summary>
    /// Starts or resumes playback.
    /// </summary>
    public void Play()
    {
        if (_waveOut != null && _audioReader != null)
        {
            // Start time-based position tracking
            _playStartPosition = _audioReader.CurrentTime;
            _playStartTime = DateTime.UtcNow;
            _isTrackingPlayback = true;

            _waveOut.Play();
        }
    }

    /// <summary>
    /// Pauses playback.
    /// </summary>
    public void Pause()
    {
        if (_waveOut != null)
        {
            _waveOut.Pause();
            _isTrackingPlayback = false;

            // Sync the reader position with our calculated position
            if (_audioReader != null)
            {
                var currentPos = CurrentPosition;
                _audioReader.CurrentTime = currentPos;
            }
        }
    }

    /// <summary>
    /// Toggles between play and pause.
    /// </summary>
    public void TogglePlayPause()
    {
        if (IsPlaying)
        {
            Pause();
        }
        else
        {
            Play();
        }
    }

    /// <summary>
    /// Stops playback and resets position to start.
    /// </summary>
    public void Stop()
    {
        _isTrackingPlayback = false;
        _waveOut?.Stop();
        if (_audioReader != null)
        {
            _audioReader.Position = 0;
        }

        _speedProvider?.Reset();
    }

    /// <summary>
    /// Seeks to a specific position.
    /// </summary>
    public void Seek(TimeSpan position)
    {
        if (_audioReader != null)
        {
            var clampedPosition = TimeSpan.FromTicks(
                Math.Clamp(position.Ticks, 0, _audioReader.TotalTime.Ticks));
            _audioReader.CurrentTime = clampedPosition;
            _speedProvider?.Reset();

            // Reset time-based tracking to new position
            if (_isTrackingPlayback)
            {
                _playStartPosition = clampedPosition;
                _playStartTime = DateTime.UtcNow;
            }

            PositionChanged?.Invoke(this, clampedPosition);
        }
    }

    /// <summary>
    /// Seeks forward by the specified duration.
    /// </summary>
    public void SeekForward(TimeSpan duration)
    {
        Seek(CurrentPosition + duration);
    }

    /// <summary>
    /// Seeks backward by the specified duration.
    /// </summary>
    public void SeekBackward(TimeSpan duration)
    {
        Seek(CurrentPosition - duration);
    }

    /// <summary>
    /// Sets the playback speed (0.5 to 2.0).
    /// </summary>
    public void SetPlaybackSpeed(double speed)
    {
        // If currently tracking playback, update the reference point before changing speed
        if (_isTrackingPlayback && IsPlaying)
        {
            _playStartPosition = CurrentPosition;
            _playStartTime = DateTime.UtcNow;
        }

        _playbackSpeed = (float)Math.Clamp(speed, 0.5, 2.0);
        if (_speedProvider != null)
        {
            _speedProvider.Speed = _playbackSpeed;
        }
    }

    /// <summary>
    /// Updates the position (call this periodically from a timer).
    /// </summary>
    public void UpdatePosition()
    {
        if (IsPlaying && _audioReader != null)
        {
            // Use CurrentPosition which does time-based tracking
            PositionChanged?.Invoke(this, CurrentPosition);
        }
    }

    private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
    {
        PlaybackStopped?.Invoke(this, EventArgs.Empty);
    }

    private void DisposeAudio()
    {
        if (_waveOut != null)
        {
            _waveOut.PlaybackStopped -= OnPlaybackStopped;
            _waveOut.Dispose();
            _waveOut = null;
        }

        _speedProvider = null;
        _audioReader?.Dispose();
        _audioReader = null;
        _currentFilePath = null;
    }

    public void Dispose()
    {
        if (!_isDisposed)
        {
            DisposeAudio();
            _isDisposed = true;
        }
    }
}
#endif
