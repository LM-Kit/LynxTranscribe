namespace LynxTranscribe.Helpers;

/// <summary>
/// Draws an audio waveform visualization with playback position indicator.
/// </summary>
public class WaveformDrawable : IDrawable
{
    private float[] _waveformData = Array.Empty<float>();
    private double _playbackPosition = 0; // 0 to 1
    private Color _waveformColor = Colors.Gray;
    private Color _playedColor = Colors.Orange;
    private Color _positionColor = Colors.Orange;

    public float[] WaveformData
    {
        get => _waveformData;
        set => _waveformData = value ?? Array.Empty<float>();
    }

    public double PlaybackPosition
    {
        get => _playbackPosition;
        set => _playbackPosition = Math.Clamp(value, 0, 1);
    }

    public Color WaveformColor
    {
        get => _waveformColor;
        set => _waveformColor = value;
    }

    public Color PlayedColor
    {
        get => _playedColor;
        set => _playedColor = value;
    }

    public Color PositionColor
    {
        get => _positionColor;
        set => _positionColor = value;
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var width = dirtyRect.Width;
        var height = dirtyRect.Height;
        var centerY = height / 2;
        var positionX = (float)(width * _playbackPosition);

        // If no waveform data, draw a simple line with bars
        if (_waveformData.Length == 0)
        {
            DrawPlaceholderWaveform(canvas, width, height, centerY, positionX);
            return;
        }

        // Draw waveform bars
        var barWidth = Math.Max(2, width / _waveformData.Length);
        var gap = 1f;

        for (int i = 0; i < _waveformData.Length; i++)
        {
            var x = i * (barWidth + gap);
            if (x > width)
            {
                break;
            }

            var amplitude = _waveformData[i];
            var barHeight = Math.Max(2, amplitude * (height - 4));
            var y = centerY - (barHeight / 2);

            // Use played color for bars before position, waveform color for after
            canvas.FillColor = x < positionX ? _playedColor : _waveformColor;
            canvas.FillRoundedRectangle(x, y, barWidth, barHeight, barWidth / 2);
        }

        // Draw position indicator line
        canvas.StrokeColor = _positionColor;
        canvas.StrokeSize = 2;
        canvas.DrawLine(positionX, 4, positionX, height - 4);

        // Draw position handle
        canvas.FillColor = _positionColor;
        canvas.FillCircle(positionX, centerY, 5);
    }

    private void DrawPlaceholderWaveform(ICanvas canvas, float width, float height, float centerY, float positionX)
    {
        // Generate a simple placeholder pattern
        var barCount = 60;
        var barWidth = (width / barCount) - 1;
        var gap = 1f;

        for (int i = 0; i < barCount; i++)
        {
            var x = i * (barWidth + gap);

            // Create a simple wave pattern
            var progress = (float)i / barCount;
            var wave1 = ((float)Math.Sin(progress * Math.PI * 4) * 0.3f) + 0.5f;
            var wave2 = (float)Math.Sin((progress * Math.PI * 7) + 1) * 0.2f;
            var amplitude = Math.Abs(wave1 + wave2);

            var barHeight = Math.Max(4, amplitude * (height - 8));
            var y = centerY - (barHeight / 2);

            canvas.FillColor = x < positionX ? _playedColor : _waveformColor;
            canvas.FillRoundedRectangle(x, y, barWidth, barHeight, barWidth / 2);
        }

        // Draw position indicator line
        canvas.StrokeColor = _positionColor;
        canvas.StrokeSize = 2;
        canvas.DrawLine(positionX, 4, positionX, height - 4);

        // Draw position handle
        canvas.FillColor = _positionColor;
        canvas.FillCircle(positionX, centerY, 5);
    }

    /// <summary>
    /// Generates waveform data from audio samples.
    /// </summary>
    public static float[] GenerateWaveformData(float[] audioSamples, int targetBars = 100)
    {
        if (audioSamples == null || audioSamples.Length == 0)
        {
            return Array.Empty<float>();
        }

        var samplesPerBar = audioSamples.Length / targetBars;
        if (samplesPerBar < 1)
        {
            samplesPerBar = 1;
        }

        var waveform = new float[targetBars];

        for (int i = 0; i < targetBars; i++)
        {
            var startIndex = i * samplesPerBar;
            var endIndex = Math.Min(startIndex + samplesPerBar, audioSamples.Length);

            float maxAmplitude = 0;
            for (int j = startIndex; j < endIndex; j++)
            {
                var amplitude = Math.Abs(audioSamples[j]);
                if (amplitude > maxAmplitude)
                {
                    maxAmplitude = amplitude;
                }
            }

            waveform[i] = maxAmplitude;
        }

        // Normalize to 0-1 range
        var max = waveform.Max();
        if (max > 0)
        {
            for (int i = 0; i < waveform.Length; i++)
            {
                waveform[i] /= max;
            }
        }

        return waveform;
    }
}
