using LMKit.Media.Audio;
using LMKit.Model;
using LMKit.Speech;
#if WINDOWS
using NAudio.Wave;
#endif

namespace LynxTranscribe.Services;

public class LMKitService : IDisposable
{
    public const string ModelIdAccurate = "whisper-large3";
    public const string ModelIdTurbo = "whisper-large-turbo3";

    private static readonly HashSet<string> MediaFoundationFormats = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".m4a", ".aac", ".avi", ".mov", ".wma"
    };

    private WaveFile? _loadedAudio;
    private SpeechToText? _speechToText;
    private LM? _model;
    private string? _lastLoadedModelId;

    private bool _isModelLoading;
    private bool _modelDownloadCancelRequest;
    private bool _disposed;

    public WaveFile? LoadedAudio => _loadedAudio;
    public bool IsModelLoading => _isModelLoading;
    public bool ModelDownloadCancelRequest => _modelDownloadCancelRequest;
    public bool HasLoadedAudio => _loadedAudio != null;
    public bool HasLoadedModel => _model != null;
    public string? CurrentModelId => _lastLoadedModelId;
    public LM? Model => _model;

    #region Audio Loading

    public async Task LoadAudioAsync(string filePath)
    {
        DisposeAudio();

        await Task.Run(() =>
        {
            if (WaveFile.IsValidWaveFile(filePath))
            {
                _loadedAudio = new WaveFile(filePath);
                return;
            }

            string tempFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.wav");

            try
            {
                ConvertToWav(filePath, tempFilePath);
                _loadedAudio = new WaveFile(File.ReadAllBytes(tempFilePath));
            }
            finally
            {
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
            }
        });
    }

#if WINDOWS
    private static void ConvertToWav(string inputPath, string outputPath)
    {
        string extension = Path.GetExtension(inputPath);

        if (MediaFoundationFormats.Contains(extension))
        {
            using var reader = new MediaFoundationReader(inputPath);
            WaveFileWriter.CreateWaveFile16(outputPath, reader.ToSampleProvider());
        }
        else
        {
            using var reader = new AudioFileReader(inputPath);
            WaveFileWriter.CreateWaveFile16(outputPath, reader.ToSampleProvider());
        }
    }
#elif MACCATALYST || IOS
    private static void ConvertToWav(string inputPath, string outputPath)
    {
        var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "/usr/bin/afconvert",
                Arguments = $"-f WAVE -d LEI16@16000 -c 1 \"{inputPath}\" \"{outputPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        process.WaitForExit(60000);

        if (process.ExitCode != 0)
        {
            var error = process.StandardError.ReadToEnd();
            throw new Exception($"Audio conversion failed: {error}");
        }

        if (!File.Exists(outputPath))
        {
            throw new Exception("Audio conversion failed: output file not created");
        }
    }
#endif

    public void DisposeAudio()
    {
        _loadedAudio?.Dispose();
        _loadedAudio = null;
    }

    #endregion

    #region Model Loading

    /// <summary>
    /// Ensures the requested model is loaded.
    /// Downloads the model if not locally available.
    /// </summary>
    /// <param name="useAccurateMode">True for accurate model, false for turbo model</param>
    /// <param name="downloadProgress">Optional callback for download progress (path, contentLength, bytesRead) => shouldContinue</param>
    /// <returns>Task that completes when model is loaded</returns>
    public async Task EnsureModelLoadedAsync(bool useAccurateMode, LM.ModelDownloadingProgressCallback? downloadProgress = null)
    {
        var requestedModelId = useAccurateMode ? ModelIdAccurate : ModelIdTurbo;

        if (_model != null && _lastLoadedModelId == requestedModelId)
        {
            return;
        }

        DisposeModel();

        _isModelLoading = true;
        _modelDownloadCancelRequest = false;

        LM.DeviceConfiguration deviceConfiguration = new LM.DeviceConfiguration()
        {
            GpuLayerCount = int.MaxValue
        };

        try
        {
            _model = await Task.Run(() =>
                LM.LoadFromModelID(requestedModelId, deviceConfiguration: deviceConfiguration, downloadingProgress: downloadProgress)
            );

            if (_modelDownloadCancelRequest)
            {
                _model?.Dispose();
                _model = null;
                _lastLoadedModelId = null;
                throw new OperationCanceledException("Model download was cancelled");
            }

            _lastLoadedModelId = requestedModelId;
        }
        catch (Exception) when (_modelDownloadCancelRequest)
        {
            _model?.Dispose();
            _model = null;
            _lastLoadedModelId = null;
            throw new OperationCanceledException("Model download was cancelled");
        }
        finally
        {
            _isModelLoading = false;
        }
    }

    /// <summary>
    /// Gets the model card for the specified mode.
    /// </summary>
    public ModelCard GetModelCard(bool useAccurateMode)
    {
        var modelId = useAccurateMode ? ModelIdAccurate : ModelIdTurbo;
        return ModelCard.GetPredefinedModelCardByModelID(modelId);
    }

    /// <summary>
    /// Gets the display name for the model.
    /// </summary>
    public string GetModelDisplayName(bool useAccurateMode)
    {
        return useAccurateMode ? "Whisper Large (Accurate)" : "Whisper Large Turbo (Fast)";
    }

    /// <summary>
    /// Requests cancellation of the current model download.
    /// </summary>
    public void RequestModelDownloadCancel()
    {
        _modelDownloadCancelRequest = true;
    }

    /// <summary>
    /// Disposes the currently loaded model.
    /// </summary>
    public void DisposeModel()
    {
        _model?.Dispose();
        _model = null;
        _lastLoadedModelId = null;
    }

    #endregion

    #region Transcription

    /// <summary>
    /// Creates a new SpeechToText instance for transcription.
    /// Requires a model to be loaded first.
    /// </summary>
    /// <param name="enableVad">Whether to enable Voice Activity Detection</param>
    /// <returns>The configured SpeechToText instance</returns>
    /// <exception cref="InvalidOperationException">If no model is loaded</exception>
    public SpeechToText CreateSpeechToText(bool enableVad)
    {
        if (_model == null)
        {
            throw new InvalidOperationException("No model loaded. Call EnsureModelLoadedAsync first.");
        }

        DisposeSpeechToText();

        _speechToText = new SpeechToText(_model)
        {
            EnableVoiceActivityDetection = enableVad
        };

        return _speechToText;
    }

    /// <summary>
    /// Gets the currently active SpeechToText instance.
    /// </summary>
    public SpeechToText? CurrentSpeechToText => _speechToText;

    /// <summary>
    /// Disposes the current SpeechToText instance.
    /// </summary>
    public void DisposeSpeechToText()
    {
        _speechToText = null;
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            DisposeSpeechToText();
            DisposeModel();
            DisposeAudio();
        }

        _disposed = true;
    }

    ~LMKitService()
    {
        Dispose(false);
    }

    #endregion
}
