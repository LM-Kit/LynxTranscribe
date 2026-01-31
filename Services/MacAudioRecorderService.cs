#if MACCATALYST || IOS
using AVFoundation;
using Foundation;

namespace LynxTranscribe.Services;

public class MacAudioRecorderService : IDisposable
{
    private AVAudioRecorder? _recorder;
    private string? _currentFilePath;
    private bool _isRecording;
    private DateTime _recordingStartTime;
    private string _recordingsDirectory;
    private NSTimer? _levelTimer;

    public event EventHandler<TimeSpan>? DurationChanged;
    public event EventHandler<float>? LevelChanged;
    public event EventHandler<string>? RecordingStopped;
    public event EventHandler<string>? RecordingError;

    public bool IsRecording => _isRecording;
    public TimeSpan Duration => _isRecording ? DateTime.Now - _recordingStartTime : TimeSpan.Zero;

    public MacAudioRecorderService()
    {
        _recordingsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LynxTranscribe",
            "Recordings");
        Directory.CreateDirectory(_recordingsDirectory);
    }

    public void SetRecordingsDirectory(string directory)
    {
        if (string.IsNullOrEmpty(directory)) return;

        try
        {
            Directory.CreateDirectory(directory);
            _recordingsDirectory = directory;
        }
        catch
        {
        }
    }

    public string RecordingsDirectory => _recordingsDirectory;

    public static List<AudioInputDevice> GetInputDevices()
    {
        var devices = new List<AudioInputDevice>();
        var session = AVAudioSession.SharedInstance();

        try
        {
            NSError error;
            session.SetCategory(AVAudioSessionCategory.PlayAndRecord, AVAudioSessionCategoryOptions.DefaultToSpeaker | AVAudioSessionCategoryOptions.AllowBluetooth, out error);
            session.SetActive(true);

            var inputs = session.AvailableInputs;
            if (inputs != null)
            {
                int id = 0;
                foreach (var input in inputs)
                {
                    devices.Add(new AudioInputDevice
                    {
                        Id = id++,
                        Name = input.PortName ?? "Unknown",
                        Channels = (int)(input.DataSources?.Length ?? 1)
                    });
                }
            }

            if (devices.Count == 0)
            {
                devices.Add(new AudioInputDevice
                {
                    Id = 0,
                    Name = "Default Microphone",
                    Channels = 1
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetInputDevices error: {ex.Message}");
            devices.Add(new AudioInputDevice
            {
                Id = 0,
                Name = "Default Microphone",
                Channels = 1
            });
        }

        return devices;
    }

    public void RequestMicrophonePermission(Action<bool> callback)
    {
#pragma warning disable CA1422 // Validate platform compatibility
        var session = AVAudioSession.SharedInstance();
        session.RequestRecordPermission(granted => callback(granted));
#pragma warning restore CA1422
    }

    public void StartRecording(int deviceId = -1)
    {
        if (_isRecording) return;

        try
        {
            var session = AVAudioSession.SharedInstance();
            NSError sessionError;
            session.SetCategory(AVAudioSessionCategory.PlayAndRecord, 
                AVAudioSessionCategoryOptions.DefaultToSpeaker | AVAudioSessionCategoryOptions.AllowBluetooth,
                out sessionError);
            
            if (sessionError != null)
            {
                RecordingError?.Invoke(this, $"Audio session error: {sessionError.LocalizedDescription}");
                return;
            }

            session.SetActive(true, out sessionError);

            if (deviceId >= 0)
            {
                var inputs = session.AvailableInputs;
                if (inputs != null && deviceId < inputs.Length)
                {
                    NSError inputError;
                    session.SetPreferredInput(inputs[deviceId], out inputError);
                }
            }

            Directory.CreateDirectory(_recordingsDirectory);
            _currentFilePath = Path.Combine(_recordingsDirectory, $"recording_{DateTime.Now:yyyyMMdd_HHmmss}.wav");

            var url = NSUrl.FromFilename(_currentFilePath);

            var settings = new NSDictionary(
                AVAudioSettings.AVSampleRateKey, NSNumber.FromFloat(16000),
                AVAudioSettings.AVNumberOfChannelsKey, NSNumber.FromInt32(1),
                AVAudioSettings.AVFormatIDKey, NSNumber.FromInt32((int)AudioToolbox.AudioFormatType.LinearPCM),
                AVAudioSettings.AVLinearPCMBitDepthKey, NSNumber.FromInt32(16),
                AVAudioSettings.AVLinearPCMIsBigEndianKey, NSNumber.FromBoolean(false),
                AVAudioSettings.AVLinearPCMIsFloatKey, NSNumber.FromBoolean(false)
            );

            NSError? error;
            _recorder = AVAudioRecorder.Create(url, new AudioSettings(settings), out error);

            if (error != null || _recorder == null)
            {
                RecordingError?.Invoke(this, error?.LocalizedDescription ?? "Failed to create recorder");
                return;
            }

            _recorder.MeteringEnabled = true;
            _recorder.PrepareToRecord();
            
            if (!_recorder.Record())
            {
                RecordingError?.Invoke(this, "Failed to start recording");
                return;
            }

            _isRecording = true;
            _recordingStartTime = DateTime.Now;

            _levelTimer = NSTimer.CreateRepeatingScheduledTimer(0.1, timer =>
            {
                if (!_isRecording || _recorder == null) return;

                _recorder.UpdateMeters();
                var level = _recorder.AveragePower(0);
                var normalizedLevel = (float)Math.Pow(10, level / 20);
                normalizedLevel = Math.Min(1.0f, Math.Max(0.0f, normalizedLevel * 2));

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    LevelChanged?.Invoke(this, normalizedLevel);
                    DurationChanged?.Invoke(this, Duration);
                });
            });
        }
        catch (Exception ex)
        {
            _isRecording = false;
            RecordingError?.Invoke(this, ex.Message);
        }
    }

    public string? StopRecording()
    {
        if (!_isRecording) return null;

        _isRecording = false;
        _levelTimer?.Invalidate();
        _levelTimer = null;

        var filePath = _currentFilePath;

        try
        {
            _recorder?.Stop();

            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {
                RecordingStopped?.Invoke(this, filePath);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"StopRecording error: {ex.Message}");
        }

        Cleanup();
        return filePath;
    }

    private void Cleanup()
    {
        _levelTimer?.Invalidate();
        _levelTimer = null;
        _recorder?.Dispose();
        _recorder = null;
    }

    public void Dispose()
    {
        if (_isRecording) StopRecording();
        Cleanup();
    }
}
#endif
