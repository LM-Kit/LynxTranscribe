#if MACCATALYST || IOS
using AVFoundation;
using Foundation;

namespace LynxTranscribe.Services;

public class MacAudioPlayerService : IDisposable
{
    private AVAudioPlayer? _player;
    private string? _currentFilePath;
    private bool _isDisposed;
    private float _playbackSpeed = 1.0f;
    private NSTimer? _positionTimer;

    public event EventHandler<TimeSpan>? PositionChanged;
    public event EventHandler? PlaybackStopped;

    public bool IsPlaying => _player?.Playing ?? false;
    public bool IsPaused => _player != null && !_player.Playing && _player.CurrentTime > 0;
    public bool IsStopped => _player == null || (!_player.Playing && _player.CurrentTime == 0);

    public TimeSpan CurrentPosition
    {
        get
        {
            if (_player == null) return TimeSpan.Zero;
            return TimeSpan.FromSeconds(_player.CurrentTime);
        }
    }

    public TimeSpan TotalDuration => _player != null ? TimeSpan.FromSeconds(_player.Duration) : TimeSpan.Zero;

    public float Volume
    {
        get => _player?.Volume ?? 1.0f;
        set
        {
            if (_player != null)
            {
                _player.Volume = Math.Clamp(value, 0f, 1f);
            }
        }
    }

    public double PlaybackSpeed => _playbackSpeed;

    public bool Load(string filePath)
    {
        try
        {
            Stop();
            DisposeAudio();

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                return false;
            }

            var url = NSUrl.FromFilename(filePath);
            NSError? error = null;
            _player = AVAudioPlayer.FromUrl(url, out error);

            if (error != null || _player == null)
            {
                System.Diagnostics.Debug.WriteLine($"MacAudioPlayerService.Load error: {error?.LocalizedDescription}");
                return false;
            }

            _player.EnableRate = true;
            _player.Rate = _playbackSpeed;
            _player.PrepareToPlay();
            _player.FinishedPlaying += OnPlayerFinished;
            _currentFilePath = filePath;

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MacAudioPlayerService.Load exception: {ex.Message}");
            DisposeAudio();
            return false;
        }
    }

    public async Task<bool> LoadAsync(string filePath)
    {
        return await Task.Run(() => Load(filePath));
    }

    public void Play()
    {
        if (_player != null)
        {
            _player.Rate = _playbackSpeed;
            _player.Play();
            StartPositionTimer();
        }
    }

    public void Pause()
    {
        _player?.Pause();
        StopPositionTimer();
    }

    public void TogglePlayPause()
    {
        if (IsPlaying) Pause();
        else Play();
    }

    public void Stop()
    {
        StopPositionTimer();
        if (_player != null)
        {
            _player.Stop();
            _player.CurrentTime = 0;
        }
    }

    public void Seek(TimeSpan position)
    {
        if (_player != null)
        {
            var seconds = Math.Clamp(position.TotalSeconds, 0, _player.Duration);
            _player.CurrentTime = seconds;
            PositionChanged?.Invoke(this, TimeSpan.FromSeconds(seconds));
        }
    }

    public void SeekForward(TimeSpan duration)
    {
        Seek(CurrentPosition + duration);
    }

    public void SeekBackward(TimeSpan duration)
    {
        Seek(CurrentPosition - duration);
    }

    public void SetPlaybackSpeed(double speed)
    {
        _playbackSpeed = (float)Math.Clamp(speed, 0.5, 2.0);
        if (_player != null && _player.Playing)
        {
            _player.Rate = _playbackSpeed;
        }
    }

    public void UpdatePosition()
    {
        if (IsPlaying && _player != null)
        {
            PositionChanged?.Invoke(this, CurrentPosition);
        }
    }

    private void StartPositionTimer()
    {
        StopPositionTimer();
        _positionTimer = NSTimer.CreateRepeatingScheduledTimer(0.1, timer =>
        {
            if (IsPlaying)
            {
                MainThread.BeginInvokeOnMainThread(() => PositionChanged?.Invoke(this, CurrentPosition));
            }
        });
    }

    private void StopPositionTimer()
    {
        _positionTimer?.Invalidate();
        _positionTimer = null;
    }

    private void OnPlayerFinished(object? sender, AVStatusEventArgs e)
    {
        StopPositionTimer();
        MainThread.BeginInvokeOnMainThread(() => PlaybackStopped?.Invoke(this, EventArgs.Empty));
    }

    private void DisposeAudio()
    {
        StopPositionTimer();
        if (_player != null)
        {
            _player.FinishedPlaying -= OnPlayerFinished;
            _player.Dispose();
            _player = null;
        }
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
