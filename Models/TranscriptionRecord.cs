namespace LynxTranscribe.Models;

/// <summary>
/// Represents a saved transcription record.
/// </summary>
public class TranscriptionRecord
{
    /// <summary>
    /// Unique identifier for the record.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Custom name for the transcription (user-editable).
    /// </summary>
    public string? CustomName { get; set; }

    /// <summary>
    /// Original audio file name (not full path for privacy).
    /// </summary>
    public string AudioFileName { get; set; } = "";

    /// <summary>
    /// Full path to the original audio file (for playback if still available).
    /// </summary>
    public string? AudioFilePath { get; set; }

    /// <summary>
    /// When the transcription was performed.
    /// </summary>
    public DateTime TranscribedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Timestamped segments - the source of truth for transcript content.
    /// </summary>
    private List<LMKit.Speech.AudioSegment> _segments = new();

    public List<LMKit.Speech.AudioSegment> Segments
    {
        get => _segments;
        set
        {
            _segments = value ?? new();
            InvalidateCache();
        }
    }

    // Cached computed values
    private string? _cachedTranscriptText;
    private int? _cachedWordCount;
    private string? _cachedPreview;

    private void InvalidateCache()
    {
        _cachedTranscriptText = null;
        _cachedWordCount = null;
        _cachedPreview = null;
    }

    /// <summary>
    /// Sets cached values for lightweight records (without loading segments).
    /// </summary>
    internal void SetCachedValues(string preview, int wordCount)
    {
        _cachedPreview = preview;
        _cachedWordCount = wordCount;
    }

    /// <summary>
    /// Formats segments as plain text.
    /// This is the single source of truth for text output formatting.
    /// </summary>
    public static string FormatAsPlainText(IEnumerable<LMKit.Speech.AudioSegment> segments, string separator = " ")
    {
        var list = segments as IList<LMKit.Speech.AudioSegment> ?? segments.ToList();
        if (list.Count == 0)
        {
            return "";
        }

        return string.Join(separator, list.Select(s => s.Text.Trim()));
    }

    /// <summary>
    /// The full transcript text - computed from Segments, one line per segment. Cached.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string TranscriptText
    {
        get
        {
            if (_cachedTranscriptText == null)
            {
                _cachedTranscriptText = FormatAsPlainText(_segments);
            }
            return _cachedTranscriptText;
        }
    }

    /// <summary>
    /// Number of words in the transcript - computed from Segments or cached.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public int WordCount
    {
        get
        {
            if (_cachedWordCount == null)
            {
                if (_segments.Count == 0)
                {
                    _cachedWordCount = 0;
                }
                else
                {
                    _cachedWordCount = _segments.Sum(s =>
                        s.Text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length);
                }
            }
            return _cachedWordCount.Value;
        }
    }

    /// <summary>
    /// How long the transcription took in seconds.
    /// </summary>
    public double ProcessingTimeSeconds { get; set; }

    /// <summary>
    /// Model mode used: "Accurate" or "Turbo".
    /// </summary>
    public string ModelMode { get; set; } = "Accurate";

    /// <summary>
    /// Whether Voice Activity Detection was enabled.
    /// </summary>
    public bool VadEnabled { get; set; } = true;

    /// <summary>
    /// Language code used for transcription (e.g., "en", "fr", "auto").
    /// </summary>
    public string TranscriptionLanguage { get; set; } = "auto";

    /// <summary>
    /// Audio file duration if available.
    /// </summary>
    public TimeSpan? AudioDuration { get; set; }

    /// <summary>
    /// Gets the display name (custom name or filename).
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string DisplayName => !string.IsNullOrWhiteSpace(CustomName) ? CustomName : AudioFileName;

    /// <summary>
    /// Gets a preview of the transcript (first ~80 characters) - computed or cached.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string Preview
    {
        get
        {
            if (_cachedPreview != null)
            {
                return _cachedPreview;
            }

            var text = TranscriptText;
            if (string.IsNullOrEmpty(text))
            {
                return "(No content)";
            }

            var cleaned = text.Replace("\r", " ").Replace("\n", " ");
            _cachedPreview = cleaned.Length <= 80 ? cleaned : cleaned.Substring(0, 80) + "...";
            return _cachedPreview;
        }
    }

    /// <summary>
    /// Gets a friendly date string for display.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string FriendlyDate
    {
        get
        {
            var today = DateTime.Today;
            var date = TranscribedAt.Date;

            if (date == today)
            {
                return TranscribedAt.ToString("h:mm tt");
            }

            if (date == today.AddDays(-1))
            {
                return "Yesterday";
            }

            if (date > today.AddDays(-7))
            {
                return TranscribedAt.ToString("dddd");
            }

            if (date.Year == today.Year)
            {
                return TranscribedAt.ToString("MMM d");
            }

            return TranscribedAt.ToString("MMM d, yyyy");
        }
    }

    /// <summary>
    /// Gets the date group for organizing history.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string DateGroup
    {
        get
        {
            var today = DateTime.Today;
            var date = TranscribedAt.Date;

            if (date == today)
            {
                return "Today";
            }

            if (date == today.AddDays(-1))
            {
                return "Yesterday";
            }

            if (date > today.AddDays(-7))
            {
                return "This Week";
            }

            if (date > today.AddDays(-30))
            {
                return "This Month";
            }

            return "Older";
        }
    }

    /// <summary>
    /// Gets transcript text with timestamps.
    /// </summary>
    public string GetTimestampedText()
    {
        if (_segments.Count == 0)
        {
            return "";
        }

        var lines = new List<string>();
        foreach (var segment in _segments)
        {
            var startStr = FormatTime(segment.Start);
            var endStr = FormatTime(segment.End);
            lines.Add($"[{startStr} - {endStr}] {segment.Text}");
        }
        return string.Join(Environment.NewLine, lines);
    }

    private static string FormatTime(TimeSpan time)
    {
        if (time.TotalHours >= 1)
        {
            return time.ToString(@"h\:mm\:ss");
        }

        return time.ToString(@"m\:ss");
    }
}
