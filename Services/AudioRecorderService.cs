using NAudio.Wave;

namespace LynxTranscribe.Services;

/// <summary>
/// Service for recording audio from input devices.
/// Records at 16kHz mono WAV format optimized for Whisper transcription.
/// </summary>
public class AudioRecorderService : IDisposable
{
    private WaveInEvent? _waveIn;
    private WaveFileWriter? _writer;
    private string? _currentFilePath;
    private bool _isRecording;
    private DateTime _recordingStartTime;
    private readonly object _lock = new();
    private string _recordingsDirectory;

    // Target format for Whisper: 16kHz, mono, 16-bit
    private readonly WaveFormat _targetFormat = new(16000, 16, 1);

    public event EventHandler<TimeSpan>? DurationChanged;
    public event EventHandler<float>? LevelChanged;
    public event EventHandler<string>? RecordingStopped;
    public event EventHandler<string>? RecordingError;

    public bool IsRecording => _isRecording;
    public TimeSpan Duration => _isRecording ? DateTime.Now - _recordingStartTime : TimeSpan.Zero;

    public AudioRecorderService()
    {
        // Default to temp directory
        _recordingsDirectory = Path.Combine(Path.GetTempPath(), "LynxTranscribe");
        Directory.CreateDirectory(_recordingsDirectory);
    }

    /// <summary>
    /// Sets the directory where recordings are stored.
    /// </summary>
    public void SetRecordingsDirectory(string directory)
    {
        if (string.IsNullOrEmpty(directory))
        {
            return;
        }

        try
        {
            Directory.CreateDirectory(directory);
            _recordingsDirectory = directory;
        }
        catch
        {
            // Keep using current path if new path is invalid
        }
    }

    /// <summary>
    /// Gets the current recordings directory.
    /// </summary>
    public string RecordingsDirectory => _recordingsDirectory;

    /// <summary>
    /// Gets available audio input devices.
    /// </summary>
    public static List<AudioInputDevice> GetInputDevices()
    {
        var devices = new List<AudioInputDevice>();

        for (int i = 0; i < WaveInEvent.DeviceCount; i++)
        {
            var caps = WaveInEvent.GetCapabilities(i);
            devices.Add(new AudioInputDevice
            {
                Id = i,
                Name = caps.ProductName,
                Channels = caps.Channels
            });
        }

        return devices;
    }

    /// <summary>
    /// Starts recording from the specified device.
    /// </summary>
    /// <param name="deviceId">Device ID, or -1 for default device</param>
    public void StartRecording(int deviceId = -1)
    {
        if (_isRecording)
        {
            return;
        }

        try
        {
            // Create recording file in configured directory
            Directory.CreateDirectory(_recordingsDirectory);
            _currentFilePath = Path.Combine(_recordingsDirectory, $"recording_{DateTime.Now:yyyyMMdd_HHmmss}.wav");

            // Setup wave input
            _waveIn = new WaveInEvent
            {
                DeviceNumber = deviceId < 0 ? 0 : deviceId,
                WaveFormat = _targetFormat,
                BufferMilliseconds = 50
            };

            _waveIn.DataAvailable += OnDataAvailable;
            _waveIn.RecordingStopped += OnRecordingStoppedInternal;

            // Create file writer
            _writer = new WaveFileWriter(_currentFilePath, _targetFormat);

            // Start recording
            _waveIn.StartRecording();
            _isRecording = true;
            _recordingStartTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            Cleanup();
            RecordingError?.Invoke(this, ex.Message);
        }
    }

    /// <summary>
    /// Stops recording and returns the path to the recorded file.
    /// </summary>
    public string? StopRecording()
    {
        if (!_isRecording)
        {
            return null;
        }

        lock (_lock)
        {
            _isRecording = false;

            try
            {
                _waveIn?.StopRecording();
            }
#if DEBUG
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"StopRecording: {ex.Message}"); }
#else
            catch { }
#endif

            var filePath = _currentFilePath;

            // Cleanup will be called by OnRecordingStoppedInternal
            return filePath;
        }
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        lock (_lock)
        {
            if (!_isRecording || _writer == null)
            {
                return;
            }

            try
            {
                _writer.Write(e.Buffer, 0, e.BytesRecorded);

                // Calculate audio level for visualization
                float maxLevel = 0;
                for (int i = 0; i < e.BytesRecorded; i += 2)
                {
                    short sample = (short)(e.Buffer[i] | (e.Buffer[i + 1] << 8));
                    float level = Math.Abs(sample / 32768f);
                    if (level > maxLevel)
                    {
                        maxLevel = level;
                    }
                }

                LevelChanged?.Invoke(this, maxLevel);
                DurationChanged?.Invoke(this, Duration);
            }
#if DEBUG
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"OnDataAvailable: {ex.Message}"); }
#else
            catch { }
#endif
        }
    }

    private void OnRecordingStoppedInternal(object? sender, StoppedEventArgs e)
    {
        var filePath = _currentFilePath;
        Cleanup();

        if (e.Exception != null)
        {
            RecordingError?.Invoke(this, e.Exception.Message);
        }
        else if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
        {
            RecordingStopped?.Invoke(this, filePath);
        }
    }

    private void Cleanup()
    {
        lock (_lock)
        {
            try
            {
                _writer?.Dispose();
                _writer = null;
            }
#if DEBUG
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Cleanup writer: {ex.Message}"); }
#else
            catch { }
#endif

            try
            {
                if (_waveIn != null)
                {
                    _waveIn.DataAvailable -= OnDataAvailable;
                    _waveIn.RecordingStopped -= OnRecordingStoppedInternal;
                    _waveIn.Dispose();
                    _waveIn = null;
                }
            }
#if DEBUG
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Cleanup waveIn: {ex.Message}"); }
#else
            catch { }
#endif
        }
    }

    public void Dispose()
    {
        if (_isRecording)
        {
            StopRecording();
        }

        Cleanup();
    }
}

public class AudioInputDevice
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int Channels { get; set; }

    public override string ToString() => Name;
}
