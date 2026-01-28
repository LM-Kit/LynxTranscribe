using NAudio.Wave;

namespace LynxTranscribe.Helpers;

/// <summary>
/// Sample provider that changes playback speed using linear interpolation.
/// Note: This changes both speed and pitch (like playing a record faster/slower).
/// </summary>
public class VarispeedSampleProvider : ISampleProvider
{
    private readonly ISampleProvider _source;
    private readonly int _channels;
    private float _speed = 1.0f;

    // Buffer for source samples
    private readonly float[] _sourceBuffer;
    private int _sourceBufferCount = 0;
    private double _readPosition = 0;

    public VarispeedSampleProvider(ISampleProvider source, int bufferSizeFrames = 16384)
    {
        _source = source;
        _channels = source.WaveFormat.Channels;
        _sourceBuffer = new float[bufferSizeFrames * _channels];
    }

    public WaveFormat WaveFormat => _source.WaveFormat;

    public float Speed
    {
        get => _speed;
        set => _speed = Math.Clamp(value, 0.25f, 4.0f);
    }

    public int Read(float[] buffer, int offset, int count)
    {
        int framesRequested = count / _channels;
        int framesWritten = 0;

        while (framesWritten < framesRequested)
        {
            // Calculate how many source frames we need for this output frame
            int currentFrame = (int)_readPosition;
            int nextFrame = currentFrame + 1;

            // Check if we need to refill buffer
            int framesInBuffer = _sourceBufferCount / _channels;
            if (nextFrame >= framesInBuffer)
            {
                // Shift remaining data to start
                int framesToKeep = framesInBuffer - currentFrame;
                if (framesToKeep > 0 && currentFrame > 0)
                {
                    int samplesToKeep = framesToKeep * _channels;
                    int sourceStart = currentFrame * _channels;
                    Array.Copy(_sourceBuffer, sourceStart, _sourceBuffer, 0, samplesToKeep);
                    _sourceBufferCount = samplesToKeep;
                    _readPosition -= currentFrame;
                }
                else if (currentFrame > 0)
                {
                    _sourceBufferCount = 0;
                    _readPosition = 0;
                }

                // Read more data from source
                int spaceAvailable = _sourceBuffer.Length - _sourceBufferCount;
                if (spaceAvailable > 0)
                {
                    int read = _source.Read(_sourceBuffer, _sourceBufferCount, spaceAvailable);
                    if (read == 0 && _sourceBufferCount < _channels * 2)
                    {
                        break; // End of stream
                    }

                    _sourceBufferCount += read;
                }

                // Recalculate positions
                currentFrame = (int)_readPosition;
                nextFrame = currentFrame + 1;
                framesInBuffer = _sourceBufferCount / _channels;
            }

            // Check bounds again
            if (nextFrame >= framesInBuffer)
            {
                break;
            }

            // Get interpolation factor
            float frac = (float)(_readPosition - currentFrame);

            int idx0 = currentFrame * _channels;
            int idx1 = nextFrame * _channels;

            // Linear interpolation for each channel
            int outIdx = offset + (framesWritten * _channels);
            for (int ch = 0; ch < _channels; ch++)
            {
                float s0 = _sourceBuffer[idx0 + ch];
                float s1 = _sourceBuffer[idx1 + ch];
                buffer[outIdx + ch] = s0 + ((s1 - s0) * frac);
            }

            framesWritten++;
            _readPosition += _speed;
        }

        return framesWritten * _channels;
    }

    /// <summary>
    /// Resets the internal buffer (call when seeking).
    /// </summary>
    public void Reset()
    {
        _readPosition = 0;
        _sourceBufferCount = 0;
    }
}
