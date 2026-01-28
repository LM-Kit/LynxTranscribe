using LynxTranscribe.Models;
using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace LynxTranscribe.Services;

/// <summary>
/// Service for managing transcription history persistence.
/// Uses one JSON file per record for better performance and reliability.
/// </summary>
public class TranscriptionHistoryService
{
    private string _historyDirectory;
    private readonly ConcurrentDictionary<string, RecordMetadata> _metadataCache = new();
    private DateTime _lastDirectoryScan = DateTime.MinValue;
    private static readonly TimeSpan CacheExpiry = TimeSpan.FromSeconds(5);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Lightweight metadata for fast listing without loading full records.
    /// </summary>
    private class RecordMetadata
    {
        public string Id { get; set; } = "";
        public string? CustomName { get; set; }
        public string AudioFileName { get; set; } = "";
        public string Preview { get; set; } = "";
        public DateTime TranscribedAt { get; set; }
        public string ModelMode { get; set; } = "";
        public bool VadEnabled { get; set; }
        public int WordCount { get; set; }
        public int SegmentCount { get; set; }
        public string? AudioFilePath { get; set; }
        public string TranscriptionLanguage { get; set; } = "auto";
        public string FilePath { get; set; } = "";

        public static RecordMetadata FromRecord(TranscriptionRecord record, string filePath) => new()
        {
            Id = record.Id,
            CustomName = record.CustomName,
            AudioFileName = record.AudioFileName,
            Preview = record.Preview,
            TranscribedAt = record.TranscribedAt,
            ModelMode = record.ModelMode,
            VadEnabled = record.VadEnabled,
            WordCount = record.WordCount,
            SegmentCount = record.Segments?.Count ?? 0,
            AudioFilePath = record.AudioFilePath,
            TranscriptionLanguage = record.TranscriptionLanguage,
            FilePath = filePath
        };

        public TranscriptionRecord ToLightweightRecord()
        {
            var record = new TranscriptionRecord
            {
                Id = Id,
                CustomName = CustomName,
                AudioFileName = AudioFileName,
                TranscribedAt = TranscribedAt,
                ModelMode = ModelMode,
                VadEnabled = VadEnabled,
                AudioFilePath = AudioFilePath,
                TranscriptionLanguage = TranscriptionLanguage,
                Segments = new() // Empty - call GetById for full data
            };
            record.SetCachedValues(Preview, WordCount);
            return record;
        }
    }

    public TranscriptionHistoryService()
    {
        _historyDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LynxTranscribe",
            "History");

        Directory.CreateDirectory(_historyDirectory);
    }

    /// <summary>
    /// Updates the history storage directory.
    /// </summary>
    public void SetHistoryDirectory(string directory)
    {
        if (string.IsNullOrEmpty(directory))
        {
            return;
        }

        try
        {
            Directory.CreateDirectory(directory);
            _historyDirectory = directory;
            InvalidateCache();
        }
        catch
        {
            // Keep using current path if new path is invalid
        }
    }

    /// <summary>
    /// Gets the current history directory path.
    /// </summary>
    public string HistoryDirectory => _historyDirectory;

    /// <summary>
    /// Gets all transcription records with full segment data, newest first.
    /// Consider using GetAllLightweight() for UI listing.
    /// </summary>
    public IReadOnlyList<TranscriptionRecord> GetAll()
    {
        RefreshCacheIfNeeded();

        return _metadataCache.Values
            .OrderByDescending(m => m.TranscribedAt)
            .Select(m => LoadRecord(m.FilePath))
            .Where(r => r != null)
            .Cast<TranscriptionRecord>()
            .ToList();
    }

    /// <summary>
    /// Gets lightweight record list for UI display (without loading full segment data).
    /// Much faster than GetAll() for history list rendering.
    /// </summary>
    public IReadOnlyList<TranscriptionRecord> GetAllLightweight()
    {
        RefreshCacheIfNeeded();

        return _metadataCache.Values
            .OrderByDescending(m => m.TranscribedAt)
            .Select(m => m.ToLightweightRecord())
            .ToList();
    }

    /// <summary>
    /// Gets records grouped by date category (lightweight).
    /// </summary>
    public IEnumerable<IGrouping<string, TranscriptionRecord>> GetGroupedByDate()
    {
        var records = GetAllLightweight();
        var order = new[] { "Today", "Yesterday", "This Week", "This Month", "Older" };
        return records
            .OrderByDescending(r => r.TranscribedAt)
            .GroupBy(r => r.DateGroup)
            .OrderBy(g => Array.IndexOf(order, g.Key));
    }

    /// <summary>
    /// Gets a specific record by ID with full segment data.
    /// </summary>
    public TranscriptionRecord? GetById(string id)
    {
        var filePath = GetRecordFilePath(id);
        return LoadRecord(filePath);
    }

    /// <summary>
    /// Adds a new transcription record.
    /// </summary>
    public void Add(TranscriptionRecord record)
    {
        var filePath = GetRecordFilePath(record.Id);
        SaveRecord(record, filePath);
        _metadataCache[record.Id] = RecordMetadata.FromRecord(record, filePath);
    }

    /// <summary>
    /// Updates an existing record.
    /// </summary>
    public void Update(TranscriptionRecord record)
    {
        var filePath = GetRecordFilePath(record.Id);

        // Preserve segments if incoming record has none
        if (record.Segments == null || record.Segments.Count == 0)
        {
            var existing = LoadRecord(filePath);
            if (existing?.Segments != null && existing.Segments.Count > 0)
            {
                record.Segments = existing.Segments;
            }
        }

        SaveRecord(record, filePath);
        _metadataCache[record.Id] = RecordMetadata.FromRecord(record, filePath);
    }

    /// <summary>
    /// Deletes a record by ID.
    /// </summary>
    public bool Delete(string id)
    {
        var filePath = GetRecordFilePath(id);
        _metadataCache.TryRemove(id, out _);

        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                return true;
            }
        }
        catch
        {
            // Silently fail
        }

        return false;
    }

    /// <summary>
    /// Clears all history.
    /// </summary>
    public void ClearAll()
    {
        try
        {
            foreach (var file in Directory.GetFiles(_historyDirectory, "*.json"))
            {
#if DEBUG
                try { File.Delete(file); }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"ClearAll delete {file}: {ex.Message}"); }
#else
                try { File.Delete(file); } catch { }
#endif
            }
            _metadataCache.Clear();
        }
#if DEBUG
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"ClearAll: {ex.Message}"); }
#else
        catch { }
#endif
    }

    /// <summary>
    /// Searches records by filename or content.
    /// Uses accent-insensitive comparison.
    /// </summary>
    public IReadOnlyList<TranscriptionRecord> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return GetAllLightweight();
        }

        var normalizedQuery = RemoveDiacritics(query.ToLowerInvariant());
        var results = new List<TranscriptionRecord>();

        RefreshCacheIfNeeded();

        foreach (var meta in _metadataCache.Values.OrderByDescending(m => m.TranscribedAt))
        {
            // Quick check on cached metadata first
            if (RemoveDiacritics(meta.AudioFileName.ToLowerInvariant()).Contains(normalizedQuery) ||
                RemoveDiacritics(meta.CustomName?.ToLowerInvariant() ?? "").Contains(normalizedQuery) ||
                RemoveDiacritics(meta.Preview.ToLowerInvariant()).Contains(normalizedQuery))
            {
                var record = LoadRecord(meta.FilePath);
                if (record != null)
                {
                    results.Add(record);
                }
                continue;
            }

            // Full text search requires loading the record
            var fullRecord = LoadRecord(meta.FilePath);
            if (fullRecord != null &&
                RemoveDiacritics(fullRecord.TranscriptText.ToLowerInvariant()).Contains(normalizedQuery))
            {
                results.Add(fullRecord);
            }
        }

        return results;
    }

    /// <summary>
    /// Gets the total count of records.
    /// </summary>
    public int Count
    {
        get
        {
            RefreshCacheIfNeeded();
            return _metadataCache.Count;
        }
    }

    private string GetRecordFilePath(string id) => Path.Combine(_historyDirectory, $"{id}.json");

    private TranscriptionRecord? LoadRecord(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<TranscriptionRecord>(json, JsonOptions);
            }
        }
        catch
        {
            // If load fails, return null
        }
        return null;
    }

    private void SaveRecord(TranscriptionRecord record, string filePath)
    {
        try
        {
            var json = JsonSerializer.Serialize(record, JsonOptions);
            File.WriteAllText(filePath, json);
        }
        catch
        {
            // Silently fail
        }
    }

    private void InvalidateCache()
    {
        _metadataCache.Clear();
        _lastDirectoryScan = DateTime.MinValue;
    }

    private void RefreshCacheIfNeeded()
    {
        if (DateTime.Now - _lastDirectoryScan < CacheExpiry && _metadataCache.Count > 0)
        {
            return;
        }

        try
        {
            var existingIds = new HashSet<string>();

            foreach (var filePath in Directory.GetFiles(_historyDirectory, "*.json"))
            {
                var id = Path.GetFileNameWithoutExtension(filePath);
                existingIds.Add(id);

                // Skip if already cached
                if (_metadataCache.ContainsKey(id))
                {
                    continue;
                }

                // Load record to build metadata cache
                var record = LoadRecord(filePath);
                if (record != null)
                {
                    _metadataCache[id] = RecordMetadata.FromRecord(record, filePath);
                }
            }

            // Remove deleted records from cache
            var toRemove = _metadataCache.Keys.Where(k => !existingIds.Contains(k)).ToList();
            foreach (var id in toRemove)
            {
                _metadataCache.TryRemove(id, out _);
            }

            _lastDirectoryScan = DateTime.Now;
        }
        catch
        {
            // Directory access failed
        }
    }

    private static string RemoveDiacritics(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);

        foreach (var c in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}
