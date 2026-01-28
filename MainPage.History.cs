using LMKit.Speech;
using LynxTranscribe.Helpers;
using LynxTranscribe.Localization;
using LynxTranscribe.Models;
using L = LynxTranscribe.Localization.LocalizationService;

namespace LynxTranscribe;

/// <summary>
/// History management: Tab navigation, history list, load/save records
/// </summary>
public partial class MainPage
{
    // Double-click tracking at class level - survives RefreshHistoryList
    private string? _lastClickedRecordId = null;
    private DateTime _lastClickTime = DateTime.MinValue;

    // Flag to prevent hover effects during list refresh
    private bool _isRefreshingHistoryList = false;

    // Map record IDs to their Border elements for efficient selection updates
    private readonly Dictionary<string, Border> _historyItemCache = new();
    private string? _lastSearchQuery = null;

    #region Tab Navigation

    private void OnFileTabClicked(object? sender, TappedEventArgs e)
    {
        if (_isFileTabActive)
        {
            return;
        }

        _isFileTabActive = true;
        UpdateTabUI();

        // Just hide transcript UI when on Audio tab (keep selection intact)
        HideTranscriptUI();
    }

    private void OnHistoryTabClicked(object? sender, TappedEventArgs e)
    {
        if (!_isFileTabActive)
        {
            return;
        }

        _isFileTabActive = false;
        UpdateTabUI();
        RefreshHistoryList();

        // Restore transcript UI if we have a current record
        if (!string.IsNullOrEmpty(_currentRecordId))
        {
            ShowTranscriptUI();
        }
        else
        {
            // Auto-select the most recent transcription if none is selected
            var records = _historyService.GetAllLightweight();
            if (records.Count > 0)
            {
                LoadHistoryRecord(records[0], autoPlay: false);
            }
        }
    }

    private void HideTranscriptUI()
    {
        // Just hide UI elements, don't clear any data
        TranscriptContainer.IsVisible = false;
        TranscriptHeaderLabel.IsVisible = false;
        StatsPanel.IsVisible = false;
        ResultActions.IsVisible = false;
        HistoryPlaybackPanel.IsVisible = false;
        HistoryDateLabel.IsVisible = false;

        // Hide toolbar buttons
        EditToggle.IsVisible = false;
        SearchButton.IsVisible = false;
        DictationGroup.IsVisible = false;
        ViewModeToggle.IsVisible = false;

        // Show empty state with watermark
        EmptyTranscriptState.IsVisible = true;
        WatermarkLogo.IsVisible = true;

        // Update transcription controls (will show if audio loaded on Audio tab)
        UpdateTranscribeButtonState();
    }

    private void ShowTranscriptUI()
    {
        // Restore UI visibility for current transcript
        EmptyTranscriptState.IsVisible = false;
        WatermarkLogo.IsVisible = false;
        TranscriptContainer.IsVisible = true;
        TranscriptHeaderLabel.IsVisible = true;
        StatsPanel.IsVisible = true;
        ResultActions.IsVisible = true;

        // Show toolbar buttons if we have segments
        var hasSegments = _currentSegments.Count > 0;
        EditToggle.IsVisible = hasSegments;
        SearchButton.IsVisible = hasSegments;
        DictationGroup.IsVisible = hasSegments;
        ViewModeToggle.IsVisible = hasSegments;

        // Show history playback if audio file exists
        if (!string.IsNullOrEmpty(_historyAudioFilePath) && File.Exists(_historyAudioFilePath))
        {
            HistoryPlaybackPanel.IsVisible = true;
        }

        HistoryDateLabel.IsVisible = true;

        // Hide transcription controls when viewing history
        TranscriptionControlsPanel.IsVisible = false;
    }

    private void UpdateTabUI()
    {
        // Use centralized theme system for colors
        ApplyStyle(FileTabButton, _isFileTabActive ? ControlStyle.TabActive : ControlStyle.TabInactive, FileTabLabel);
        ApplyStyle(HistoryTabButton, _isFileTabActive ? ControlStyle.TabInactive : ControlStyle.TabActive, HistoryTabLabel);

        // Handle font attributes
        FileTabLabel.FontAttributes = _isFileTabActive ? FontAttributes.Bold : FontAttributes.None;
        HistoryTabLabel.FontAttributes = _isFileTabActive ? FontAttributes.None : FontAttributes.Bold;

        // Handle active tab stroke
        if (_isFileTabActive)
        {
            FileTabButton.Stroke = (Color)Resources["AccentMuted"]!;
        }
        else
        {
            HistoryTabButton.Stroke = (Color)Resources["AccentMuted"]!;
        }

        // Handle content visibility
        FileTabContent.IsVisible = _isFileTabActive;
        HistoryTabContent.IsVisible = !_isFileTabActive;
    }

    #endregion

    #region History Management

    private void UpdateHistoryBadge()
    {
        var count = _historyService.Count;
        // Hide badge when panel is narrow (< 290px) to prevent overlap with collapse button
        var showBadge = count > 0 && _leftPanelWidth >= 290;
        HistoryCountBadge.IsVisible = showBadge;
        HistoryCountLabel.Text = count > 99 ? "99+" : count.ToString();
    }

    private void RefreshHistoryList()
    {
        _isRefreshingHistoryList = true;

        try
        {
            var query = HistorySearchEntry.Text?.Trim() ?? "";
            // Use lightweight records for display (no segment data needed)
            var records = string.IsNullOrEmpty(query)
                ? _historyService.GetAllLightweight()
                : _historyService.Search(query);

            // If search query changed, we need a full rebuild
            bool queryChanged = query != _lastSearchQuery;
            _lastSearchQuery = query;

            EmptyHistoryState.IsVisible = records.Count == 0;
            ClearHistoryButton.IsVisible = records.Count > 0 && string.IsNullOrEmpty(query);

            if (records.Count == 0)
            {
                // Clear all items except EmptyHistoryState
                ClearHistoryListUI();
                return;
            }

            // Build set of current record IDs for quick lookup
            var currentRecordIds = new HashSet<string>(records.Select(r => r.Id));

            // Remove items from cache that no longer exist
            var idsToRemove = _historyItemCache.Keys.Where(id => !currentRecordIds.Contains(id)).ToList();
            foreach (var id in idsToRemove)
            {
                if (_historyItemCache.TryGetValue(id, out var item))
                {
                    CleanupHistoryItem(item);
                    _historyItemCache.Remove(id);
                }
            }

            // Clear UI and rebuild structure (headers need to be recreated for grouping)
            ClearHistoryListUI();

            if (!string.IsNullOrEmpty(query))
            {
                // Search results - no grouping
                foreach (var record in records)
                {
                    var item = GetOrCreateHistoryItem(record);
                    UpdateHistoryItemState(item, record);
                    HistoryList.Children.Add(item);
                }
            }
            else
            {
                // Grouped by date
                var groups = records.GroupBy(r => r.DateGroup);
                var groupOrder = new[] { "Today", "Yesterday", "This Week", "This Month", "Older" };
                var textTertiary = (Color)Resources["TextTertiary"]!;

                foreach (var group in groups.OrderBy(g => Array.IndexOf(groupOrder, g.Key)))
                {
                    var header = new Label
                    {
                        Text = group.Key,
                        FontSize = 11,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = textTertiary,
                        Margin = new Thickness(4, 12, 0, 6)
                    };
                    HistoryList.Children.Add(header);

                    foreach (var record in group)
                    {
                        var item = GetOrCreateHistoryItem(record);
                        UpdateHistoryItemState(item, record);
                        HistoryList.Children.Add(item);
                    }
                }
            }

            UpdateHistoryItemScale(_leftPanelWidth);
        }
        finally
        {
            Dispatcher.Dispatch(() => _isRefreshingHistoryList = false);
        }
    }

    private void ClearHistoryListUI()
    {
        // Remove all children except EmptyHistoryState
        for (int i = HistoryList.Children.Count - 1; i >= 0; i--)
        {
            if (HistoryList.Children[i] != EmptyHistoryState)
            {
                HistoryList.Children.RemoveAt(i);
            }
        }
    }

    private Border GetOrCreateHistoryItem(TranscriptionRecord record)
    {
        if (_historyItemCache.TryGetValue(record.Id, out var existingItem))
        {
            return existingItem;
        }

        var newItem = CreateHistoryItem(record);
        _historyItemCache[record.Id] = newItem;
        return newItem;
    }

    private void UpdateHistoryItemState(Border border, TranscriptionRecord record)
    {
        var isSelected = record.Id == _currentRecordId;

        var textPrimary = (Color)Resources["TextPrimary"]!;
        var textSecondary = (Color)Resources["TextSecondary"]!;
        var textTertiary = (Color)Resources["TextTertiary"]!;
        var surfaceBorder = (Color)Resources["SurfaceBorder"]!;
        var backgroundTertiary = (Color)Resources["BackgroundTertiary"]!;
        var accentSurface = (Color)Resources["AccentSurface"]!;
        var accentPrimary = (Color)Resources["AccentPrimary"]!;
        var accentText = (Color)Resources["AccentText"]!;

        // Update border styling based on selection
        border.BackgroundColor = isSelected ? accentSurface : backgroundTertiary;
        border.Stroke = isSelected ? accentPrimary : surfaceBorder;
        border.StrokeThickness = isSelected ? 2 : 1;

        // Update text colors in grid
        if (border.Content is Grid grid)
        {
            UpdateHistoryItemColors(grid, isSelected, textPrimary, textSecondary, textTertiary, accentText);
        }
    }

    /// <summary>
    /// Recursively updates all label colors in a history item grid.
    /// </summary>
    private void UpdateHistoryItemColors(Layout container, bool isSelected, Color textPrimary, Color textSecondary, Color textTertiary, Color accentText)
    {
        foreach (var child in container.Children)
        {
            if (child is Label label)
            {
                switch (label.ClassId)
                {
                    case "title":
                        label.TextColor = isSelected ? accentText : textPrimary;
                        break;
                    case "preview":
                        label.TextColor = textSecondary;
                        break;
                    case "meta":
                        label.TextColor = textTertiary;
                        break;
                        // Note: "icon" labels keep their specific colors (green, purple, orange, etc.)
                }
            }
            else if (child is Layout nestedLayout)
            {
                UpdateHistoryItemColors(nestedLayout, isSelected, textPrimary, textSecondary, textTertiary, accentText);
            }
        }
    }

    /// <summary>
    /// Efficiently update only the selection styling without rebuilding the entire list.
    /// Call this when changing selection instead of RefreshHistoryList.
    /// </summary>
    private void UpdateHistorySelection(string? previousRecordId, string? newRecordId)
    {
        if (previousRecordId == newRecordId)
        {
            return;
        }

        var textPrimary = (Color)Resources["TextPrimary"]!;
        var textSecondary = (Color)Resources["TextSecondary"]!;
        var textTertiary = (Color)Resources["TextTertiary"]!;
        var surfaceBorder = (Color)Resources["SurfaceBorder"]!;
        var backgroundTertiary = (Color)Resources["BackgroundTertiary"]!;
        var accentSurface = (Color)Resources["AccentSurface"]!;
        var accentPrimary = (Color)Resources["AccentPrimary"]!;
        var accentText = (Color)Resources["AccentText"]!;

        // Deselect previous item
        if (!string.IsNullOrEmpty(previousRecordId) && _historyItemCache.TryGetValue(previousRecordId, out var prevBorder))
        {
            prevBorder.BackgroundColor = backgroundTertiary;
            prevBorder.Stroke = surfaceBorder;
            prevBorder.StrokeThickness = 1;

            if (prevBorder.Content is Grid prevGrid)
            {
                UpdateHistoryItemColors(prevGrid, false, textPrimary, textSecondary, textTertiary, accentText);
            }
        }

        // Select new item
        if (!string.IsNullOrEmpty(newRecordId) && _historyItemCache.TryGetValue(newRecordId, out var newBorder))
        {
            newBorder.BackgroundColor = accentSurface;
            newBorder.Stroke = accentPrimary;
            newBorder.StrokeThickness = 2;

            if (newBorder.Content is Grid newGrid)
            {
                UpdateHistoryItemColors(newGrid, true, textPrimary, textSecondary, textTertiary, accentText);
            }
        }
    }

    private void CleanupHistoryItem(Border border)
    {
        // Clear gesture recognizers from border
        border.GestureRecognizers.Clear();

        // Clear gesture recognizers from child elements
        if (border.Content is Grid grid)
        {
            foreach (var child in grid.Children)
            {
                if (child is View view)
                {
                    view.GestureRecognizers.Clear();
                }
                if (child is Layout layout)
                {
                    foreach (var layoutChild in layout.Children)
                    {
                        if (layoutChild is View v)
                        {
                            v.GestureRecognizers.Clear();
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Clear all cached history items (call when closing or major state change)
    /// </summary>
    private void ClearHistoryCache()
    {
        foreach (var item in _historyItemCache.Values)
        {
            CleanupHistoryItem(item);
        }
        _historyItemCache.Clear();
        _lastSearchQuery = null;
    }

    private Border CreateHistoryItem(TranscriptionRecord record)
    {
        var isSelected = record.Id == _currentRecordId;
        var recordId = record.Id; // Capture only the ID, not the full record

        var textPrimary = (Color)Resources["TextPrimary"]!;
        var textSecondary = (Color)Resources["TextSecondary"]!;
        var textTertiary = (Color)Resources["TextTertiary"]!;
        var surfaceBorder = (Color)Resources["SurfaceBorder"]!;
        var backgroundTertiary = (Color)Resources["BackgroundTertiary"]!;
        var accentSurface = (Color)Resources["AccentSurface"]!;
        var accentPrimary = (Color)Resources["AccentPrimary"]!;
        var accentText = (Color)Resources["AccentText"]!;

        // Selected: accent background with primary border (strong)
        // Normal: tertiary background with surface border (subtle)
        var border = new Border
        {
            BackgroundColor = isSelected ? accentSurface : backgroundTertiary,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
            Stroke = isSelected ? accentPrimary : surfaceBorder,
            StrokeThickness = isSelected ? 2 : 1,
            Padding = new Thickness(12, 10),
            Margin = new Thickness(0, 0, 0, 4),
            ClassId = recordId // Store record ID for lookups
        };

        var grid = new Grid
        {
            RowDefinitions = new RowDefinitionCollection
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto)
            },
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };

        var fileName = new Label
        {
            Text = record.DisplayName,
            FontSize = 12,
            FontAttributes = FontAttributes.Bold,
            TextColor = isSelected ? accentText : textPrimary,
            LineBreakMode = LineBreakMode.TailTruncation,
            MaxLines = 1,
            ClassId = "title"
        };
        grid.Children.Add(fileName);

        var renameBtn = new Label { Text = "✎", FontSize = 11, TextColor = textSecondary, VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.End };
        var renameTap = new TapGestureRecognizer();
        renameTap.Tapped += async (s, e) =>
        {
            var rec = _historyService.GetById(recordId);
            if (rec != null)
            {
                await RenameHistoryItem(rec);
            }
        };
        renameBtn.GestureRecognizers.Add(renameTap);
        var renameHover = new PointerGestureRecognizer();
        renameHover.PointerEntered += (s, e) => renameBtn.TextColor = accentPrimary;
        renameHover.PointerExited += (s, e) => renameBtn.TextColor = textSecondary;
        renameBtn.GestureRecognizers.Add(renameHover);
        Grid.SetColumn(renameBtn, 1);
        grid.Children.Add(renameBtn);

        var preview = new Label
        {
            Text = record.Preview,
            FontSize = 11,
            TextColor = textSecondary,
            LineBreakMode = LineBreakMode.TailTruncation,
            MaxLines = 1,
            Margin = new Thickness(0, 4, 0, 0),
            ClassId = "preview"
        };
        Grid.SetRow(preview, 1);
        Grid.SetColumnSpan(preview, 2);
        grid.Children.Add(preview);

        var metaStack = new HorizontalStackLayout { Spacing = 8, Margin = new Thickness(0, 6, 0, 0), ClassId = "metaRow", VerticalOptions = LayoutOptions.Center };
        metaStack.Children.Add(new Label { Text = $"{record.FriendlyDate} · {record.WordCount:N0}w", FontSize = 11, TextColor = textTertiary, ClassId = "meta", VerticalOptions = LayoutOptions.Center });

        var iconsStack = new HorizontalStackLayout { Spacing = 8, ClassId = "iconsRow", VerticalOptions = LayoutOptions.Center };

        var modeLabel = new Label { Text = record.ModelMode == "Accurate" ? "◆" : "⚡", FontSize = 12, TextColor = accentText, ClassId = "icon", VerticalOptions = LayoutOptions.Center };
        ToolTipProperties.SetText(modeLabel, record.ModelMode == "Accurate" ? L.Localize(StringKeys.AccurateModeTooltip) : L.Localize(StringKeys.TurboModeTooltip));
        iconsStack.Children.Add(modeLabel);

        if (record.Segments != null && record.Segments.Count > 0)
        {
            var timestampLabel = new Label { Text = "◷", FontSize = 12, TextColor = Color.FromArgb("#22C55E"), ClassId = "icon", VerticalOptions = LayoutOptions.Center };
            ToolTipProperties.SetText(timestampLabel, L.Localize(StringKeys.TimestampsCount, record.Segments.Count));
            iconsStack.Children.Add(timestampLabel);
        }

        var vadLabel = new Label { Text = record.VadEnabled ? "◉" : "○", FontSize = 12, TextColor = record.VadEnabled ? Color.FromArgb("#8B5CF6") : textTertiary, ClassId = "icon", VerticalOptions = LayoutOptions.Center };
        ToolTipProperties.SetText(vadLabel, record.VadEnabled ? L.Localize(StringKeys.VadEnabled) : L.Localize(StringKeys.VadDisabled));
        iconsStack.Children.Add(vadLabel);

        // Language badge (always show, default to auto if not set)
        var langCode = !string.IsNullOrEmpty(record.TranscriptionLanguage)
            ? record.TranscriptionLanguage
            : "auto";
        var countryCode = WhisperLanguages.GetCountryCodeFromCode(langCode);
        var langLabel = new Label
        {
            Text = countryCode,
            FontSize = 11,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#8B5CF6"),
            VerticalOptions = LayoutOptions.Center,
            ClassId = "langBadge"
        };
        ToolTipProperties.SetText(langLabel, WhisperLanguages.GetDisplayNameFromCode(langCode));
        iconsStack.Children.Add(langLabel);

        bool hasAudio = !string.IsNullOrEmpty(record.AudioFilePath) && File.Exists(record.AudioFilePath);
        if (hasAudio)
        {
            var audioLabel = new Label { Text = "♫", FontSize = 12, TextColor = Color.FromArgb("#F59E0B"), ClassId = "icon", VerticalOptions = LayoutOptions.Center };
            ToolTipProperties.SetText(audioLabel, L.Localize(StringKeys.DoubleClickToPlay));
            iconsStack.Children.Add(audioLabel);
        }

        metaStack.Children.Add(iconsStack);
        Grid.SetRow(metaStack, 2);
        grid.Children.Add(metaStack);

        var deleteBtn = new Label { Text = "✕", FontSize = 12, TextColor = textSecondary, VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.End };
        var deleteTap = new TapGestureRecognizer();
        deleteTap.Tapped += async (s, e) => await DeleteHistoryItem(recordId);
        deleteBtn.GestureRecognizers.Add(deleteTap);
        var deleteHover = new PointerGestureRecognizer();
        deleteHover.PointerEntered += (s, e) => deleteBtn.TextColor = (Color)Resources["DangerColor"]!;
        deleteHover.PointerExited += (s, e) => deleteBtn.TextColor = textSecondary;
        deleteBtn.GestureRecognizers.Add(deleteHover);
        Grid.SetRow(deleteBtn, 2);
        Grid.SetColumn(deleteBtn, 1);
        grid.Children.Add(deleteBtn);

        border.Content = grid;

        // SIMPLE: Class-level double-click detection using recordId
        var tap = new TapGestureRecognizer();
        tap.Tapped += (s, e) =>
        {
            var now = DateTime.Now;
            bool isDoubleClick = (_lastClickedRecordId == recordId) &&
                                 ((now - _lastClickTime).TotalMilliseconds < 500);

            _lastClickedRecordId = recordId;
            _lastClickTime = now;

            // Fetch fresh record and load - autoPlay if double-click
            var rec = _historyService.GetById(recordId);
            if (rec != null)
            {
                LoadHistoryRecord(rec, autoPlay: isDoubleClick);
            }
        };
        border.GestureRecognizers.Add(tap);

        // Hover effect - disabled during list refresh to prevent visual glitches
        // Use subtle hover style (different from selected which uses AccentSurface/AccentPrimary)
        var hover = new PointerGestureRecognizer();
        hover.PointerEntered += (s, e) =>
        {
            if (!_isRefreshingHistoryList && recordId != _currentRecordId)
            {
                // Subtle hover: slightly lighter background, muted border
                border.BackgroundColor = (Color)Resources["SurfaceColor"]!;
                border.Stroke = (Color)Resources["AccentMuted"]!;
            }
        };
        hover.PointerExited += (s, e) =>
        {
            if (!_isRefreshingHistoryList && recordId != _currentRecordId)
            {
                border.BackgroundColor = (Color)Resources["BackgroundTertiary"]!;
                border.Stroke = (Color)Resources["SurfaceBorder"]!;
            }
        };
        border.GestureRecognizers.Add(hover);

        return border;
    }

    private async Task RenameHistoryItem(TranscriptionRecord record)
    {
        var freshRecord = _historyService.GetById(record.Id);
        if (freshRecord == null)
        {
            return;
        }

        var currentName = freshRecord.CustomName ?? freshRecord.AudioFileName;
        var newName = await this.DisplayPromptAsync(
            L.Localize(StringKeys.RenameTitle),
            L.Localize(StringKeys.RenameMessage),
            accept: L.Localize(StringKeys.OK),
            cancel: L.Localize(StringKeys.Cancel),
            initialValue: currentName,
            maxLength: 100,
            keyboard: Keyboard.Text);

        if (!string.IsNullOrWhiteSpace(newName) && newName != currentName)
        {
            freshRecord.CustomName = newName;
            _historyService.Update(freshRecord);
            RefreshHistoryList();
        }
    }

    private async void LoadHistoryRecord(TranscriptionRecord record, bool autoPlay = false)
    {
        _isLoadingHistoryRecord = true;

        // Close search bar when switching records
        if (SearchBar.IsVisible)
        {
            CloseSearch();
        }

        try
        {
            var freshRecord = _historyService.GetById(record.Id) ?? record;

            // Store previous record ID to detect actual selection change
            var previousRecordId = _currentRecordId;

            _currentRecordId = freshRecord.Id;

            // Update selection styling IMMEDIATELY for instant visual feedback
            UpdateHistorySelection(previousRecordId, _currentRecordId);

            _currentSegments = freshRecord.Segments ?? new();

            // Reset edit mode when loading a different record
            if (previousRecordId != _currentRecordId)
            {
                _isEditMode = false;
            }

            // Set visibility IMMEDIATELY - instant feedback
            EmptyTranscriptState.IsVisible = false; WatermarkLogo.IsVisible = false;
            TranscriptContainer.IsVisible = true;
            TranscriptHeaderLabel.IsVisible = true;
            StatsPanel.IsVisible = true;
            ResultActions.IsVisible = true;

            // Update stats IMMEDIATELY - use formatted word count if dictation enabled
            var wordCount = CalculateFormattedWordCount(_currentSegments, _settingsService.EnableDictationFormatting);
            WordCountLabel.Text = L.Localize(StringKeys.Words, wordCount);
            SegmentCountLabel.Text = L.Localize(StringKeys.Segments, _currentSegments.Count);
            SegmentCountLabel.IsVisible = true;
            SegmentCountSeparator.IsVisible = true;
            ProcessingTimeLabel.IsVisible = false;
            ProcessingTimeSeparator.IsVisible = false;
            UpdateModelModeDisplay(freshRecord.ModelMode == "Accurate");
            VadBadge.IsVisible = freshRecord.VadEnabled;

            // Display language badge from record (default to auto if not set)
            var recordLanguage = !string.IsNullOrEmpty(freshRecord.TranscriptionLanguage)
                ? freshRecord.TranscriptionLanguage
                : "auto";
            LanguageNameLabel.Text = WhisperLanguages.GetDisplayNameFromCode(recordLanguage);
            LanguageBadge.IsVisible = true;

            EditToggle.IsVisible = _currentSegments.Count > 0;
            SearchButton.IsVisible = _currentSegments.Count > 0;
            DictationGroup.IsVisible = _currentSegments.Count > 0;
            ViewModeToggle.IsVisible = _currentSegments.Count > 0;

            // Reset to segments view when loading a new record
            if (_isDocumentView)
            {
                _isDocumentView = false;
                UpdateViewModeUI();
            }

            UpdateEditToggleUI();
            UpdateDictationIndicator();
            HistoryDateLabel.Text = freshRecord.TranscribedAt.ToString("MMM d, yyyy");

            // Stop any previous playback
            _historyAudioPlayer.Stop();
            _historyPlaybackTimer?.Stop();
            ClearSegmentHighlight();

            // Reset playback panel
            _historyAudioFilePath = null;
            HistoryPlaybackPanel.IsVisible = false;

            // Build segments (async) - scroll will happen after build completes
            BuildSegmentView();

            // Wait for segment building to complete before scrolling (max 2 seconds)
            var timeout = DateTime.Now.AddSeconds(2);
            while (_isBuildingSegments && DateTime.Now < timeout)
            {
                await Task.Delay(50);
            }

            // Scroll to start after segments are built
            try { _ = SegmentsScrollView.ScrollToAsync(0, 0, false); } catch { }

            // DEFER audio loading - yield first so UI updates
            await Task.Delay(1);

            // Check if user switched to different record
            if (_currentRecordId != freshRecord.Id)
            {
                return;
            }

            // Now load audio in background
            bool hasAudioFile = !string.IsNullOrEmpty(freshRecord.AudioFilePath) && File.Exists(freshRecord.AudioFilePath);

            if (hasAudioFile)
            {
                _historyAudioFilePath = freshRecord.AudioFilePath;

                bool audioLoaded = _historyAudioPlayer.Load(freshRecord.AudioFilePath!);

                // Check again if user switched
                if (_currentRecordId != freshRecord.Id)
                {
                    return;
                }

                if (audioLoaded)
                {
                    HistoryTotalTimeLabel.Text = TranscriptExporter.FormatDisplayTime(_historyAudioPlayer.TotalDuration);
                    HistoryCurrentTimeLabel.Text = "0:00";
                    HistoryPlaybackSlider.Value = 0;
                    HistoryAudioSourceLabel.Text = System.IO.Path.GetFileName(freshRecord.AudioFilePath);
                    HistoryPlayPauseIcon.Text = "▶";
                    HistoryPlaybackPanel.IsVisible = true;

                    if (autoPlay)
                    {
                        _historyAudioPlayer.Play();
                        HistoryPlayPauseIcon.Text = "▌▌";
                        _historyPlaybackTimer?.Start();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LoadHistoryRecord error: {ex.Message}");
        }
        finally
        {
            _isLoadingHistoryRecord = false;
        }
    }

    private async Task DeleteHistoryItem(string id)
    {
        var confirm = await this.DisplayAlertAsync(
            L.Localize(StringKeys.DeleteConfirmTitle),
            L.Localize(StringKeys.DeleteConfirmMessage),
            L.Localize(StringKeys.Delete),
            L.Localize(StringKeys.Cancel));

        if (confirm)
        {
            // Remove from cache first
            if (_historyItemCache.TryGetValue(id, out var item))
            {
                CleanupHistoryItem(item);
                _historyItemCache.Remove(id);
            }

            _historyService.Delete(id);

            if (_currentRecordId == id)
            {
                _currentRecordId = null;
                _currentSegments.Clear();
                TranscriptContainer.IsVisible = false;
                TranscriptHeaderLabel.IsVisible = false;
                EmptyTranscriptState.IsVisible = true; WatermarkLogo.IsVisible = true;
                EditToggle.IsVisible = false;
                SearchButton.IsVisible = false;
                DictationGroup.IsVisible = false;
                ViewModeToggle.IsVisible = false;
                StatsPanel.IsVisible = false;
                ResultActions.IsVisible = false;
                HistoryPlaybackPanel.IsVisible = false;
                _historyAudioPlayer.Stop();
                _historyPlaybackTimer?.Stop();
                SegmentsList.Children.Clear();
                _segmentViews.Clear();
                _segmentEditors.Clear();
            }

            UpdateHistoryBadge();
            RefreshHistoryList();
        }
    }

    private async void OnClearHistoryClicked(object? sender, TappedEventArgs e)
    {
        var confirm = await this.DisplayAlertAsync(
            L.Localize(StringKeys.ClearHistoryTitle),
            L.Localize(StringKeys.ClearHistoryMessage),
            L.Localize(StringKeys.ClearAll),
            L.Localize(StringKeys.Cancel));

        if (confirm)
        {
            // Clear cache first to release memory
            ClearHistoryCache();

            _historyService.ClearAll();
            _currentRecordId = null;
            _currentSegments.Clear();
            _historyAudioPlayer.Stop();

            // Clear transcript display
            TranscriptContainer.IsVisible = false;
            TranscriptHeaderLabel.IsVisible = false;
            EmptyTranscriptState.IsVisible = true; WatermarkLogo.IsVisible = true;
            EditToggle.IsVisible = false;
            SearchButton.IsVisible = false;
            DictationGroup.IsVisible = false;
            ViewModeToggle.IsVisible = false;
            StatsPanel.IsVisible = false;
            ResultActions.IsVisible = false;
            HistoryPlaybackPanel.IsVisible = false;

            SegmentsList.Children.Clear();
            _segmentViews.Clear();
            _segmentEditors.Clear();
            _currentSegments.Clear();

            UpdateHistoryBadge();
            RefreshHistoryList();
        }
    }

    private void OnHistorySearchChanged(object? sender, TextChangedEventArgs e)
    {
        RefreshHistoryList();
    }

    private void SaveToHistory(List<AudioSegment> segments, string transcriptionLanguage = "auto")
    {
        var segmentsCopy = segments.Select(s => new AudioSegment(s.Text, s.Language ?? "en", s.Start, s.End, s.Confidence)).ToList();

        var record = new TranscriptionRecord
        {
            AudioFileName = System.IO.Path.GetFileName(_selectedFilePath) ?? "Unknown",
            AudioFilePath = _selectedFilePath,
            Segments = segmentsCopy,
            ProcessingTimeSeconds = _stopwatch.Elapsed.TotalSeconds,
            ModelMode = _useAccurateMode ? "Accurate" : "Turbo",
            VadEnabled = _enableVoiceActivityDetection,
            AudioDuration = _lmKitService.LoadedAudio?.Duration,
            TranscriptionLanguage = transcriptionLanguage
        };

        _historyService.Add(record);
        _currentRecordId = record.Id;
        _currentSegments = segmentsCopy;
        UpdateHistoryBadge();
    }

    #endregion
}
