using LynxTranscribe.Localization;
using L = LynxTranscribe.Localization.LocalizationService;

namespace LynxTranscribe;

/// <summary>
/// Search: Find in transcript with highlighting and navigation
/// </summary>
public partial class MainPage
{
    #region Search State

    private string _searchQuery = string.Empty;
    private List<SearchMatch> _searchMatches = new();
    private int _currentMatchIndex = -1;
    private Dictionary<Border, (string OriginalText, Label TextLabel)> _segmentOriginalTexts = new();

    private class SearchMatch
    {
        public int SegmentIndex { get; set; }
        public Border SegmentBorder { get; set; } = null!;
        public Label TextLabel { get; set; } = null!;
        public int StartIndex { get; set; }
        public int Length { get; set; }
    }

    #endregion

    #region Search UI Events

    private void ToggleSearch()
    {
        if (SearchBar.IsVisible)
        {
            CloseSearch();
        }
        else
        {
            OpenSearch();
        }
    }

    private void OpenSearch()
    {
        if (!TranscriptContainer.IsVisible || SegmentsList.Children.Count == 0)
        {
            ShowToast(L.Localize(StringKeys.NoTranscriptToSearch), ToastType.Info);
            return;
        }

        // Exit edit mode if active - search only works with display labels
        if (_isEditMode)
        {
            _isEditMode = false;
            UpdateEditToggleUI();
            UpdateSegmentEditability();
        }

        SearchBar.IsVisible = true;
        UpdateSearchButtonState();
        // Add top padding to segments list so content isn't hidden behind search bar
        SegmentsList.Padding = new Thickness(12, 52, 12, 12);
        SearchEntry.Focus();

        // If there's already a query, re-run search
        if (!string.IsNullOrEmpty(SearchEntry.Text))
        {
            PerformSearch(SearchEntry.Text);
        }
    }

    private void CloseSearch()
    {
        SearchBar.IsVisible = false;
        UpdateSearchButtonState();
        // Restore original padding
        SegmentsList.Padding = new Thickness(12, 12, 12, 12);
        ClearSearchHighlights();
        _searchMatches.Clear();
        _currentMatchIndex = -1;
        _segmentOriginalTexts.Clear();
        SearchMatchCount.Text = "0/0";
        SearchEntry.Text = "";
    }

    private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        PerformSearch(e.NewTextValue);
    }

    private void OnSearchEntryCompleted(object? sender, EventArgs e)
    {
        // Enter key - go to next match
        NavigateToNextMatch();
    }

    private void OnSearchPreviousClicked(object? sender, TappedEventArgs e)
    {
        NavigateToPreviousMatch();
    }

    private void OnSearchNextClicked(object? sender, TappedEventArgs e)
    {
        NavigateToNextMatch();
    }

    private void OnSearchCloseClicked(object? sender, TappedEventArgs e)
    {
        CloseSearch();
    }

    private void OnSearchButtonHoverEnter(object? sender, PointerEventArgs e)
    {
        // Don't change hover if search is active
        if (SearchBar.IsVisible)
        {
            return;
        }

        ApplyStyle(SearchButton, ControlStyle.ButtonHover);
        if (SearchButton.Content is Label label)
        {
            label.TextColor = (Color)Resources["AccentText"]!;
        }
    }

    private void OnSearchButtonHoverExit(object? sender, PointerEventArgs e)
    {
        // Don't change hover if search is active
        if (SearchBar.IsVisible)
        {
            return;
        }

        ApplyStyle(SearchButton, ControlStyle.ButtonDefault);
        if (SearchButton.Content is Label label)
        {
            label.TextColor = (Color)Resources["TextPrimary"]!;
        }
    }

    private void UpdateSearchButtonState()
    {
        if (SearchBar.IsVisible)
        {
            // Active state - same as Edit button when editing
            ApplyStyle(SearchButton, ControlStyle.ModeSelected);
            if (SearchButton.Content is Label label)
            {
                label.Text = L.Localize(StringKeys.Searching);
                label.TextColor = (Color)Resources["AccentText"]!;
            }
        }
        else
        {
            // Normal state
            ApplyStyle(SearchButton, ControlStyle.ButtonDefault);
            if (SearchButton.Content is Label label)
            {
                label.Text = L.Localize(StringKeys.Search);
                label.TextColor = (Color)Resources["TextPrimary"]!;
            }
        }
    }

    private void OnSearchButtonClicked(object? sender, TappedEventArgs e)
    {
        ToggleSearch();
    }

    /// <summary>
    /// Updates visibility of transcript toolbar buttons (Search, Edit, Dictation)
    /// </summary>
    private void UpdateTranscriptToolbarVisibility(bool visible)
    {
        SearchButton.IsVisible = visible;
        EditToggle.IsVisible = visible;
        DictationGroup.IsVisible = visible;
    }

    private void OnSearchNavButtonHoverEnter(object? sender, PointerEventArgs e)
    {
        if (sender is Border border)
        {
            ApplyStyle(border, ControlStyle.ButtonTransparentHover);
        }
    }

    private void OnSearchNavButtonHoverExit(object? sender, PointerEventArgs e)
    {
        if (sender is Border border)
        {
            ApplyStyle(border, ControlStyle.ButtonTransparent);
        }
    }

    #endregion

    #region Search Logic

    private void PerformSearch(string? query)
    {
        _searchQuery = query?.Trim() ?? string.Empty;
        ClearSearchHighlights();
        _searchMatches.Clear();
        _currentMatchIndex = -1;

        if (string.IsNullOrEmpty(_searchQuery) || _searchQuery.Length < 1)
        {
            SearchMatchCount.Text = "0/0";
            SearchMatchCount.TextColor = (Color)Resources["TextTertiary"]!;
            return;
        }

        // Find all matches in segments
        int segmentIndex = 0;
        foreach (var child in SegmentsList.Children)
        {
            if (child is Border border && border.Content is Grid grid)
            {
                // Find the display label using ClassId
                Label? textLabel = null;
                foreach (var gridChild in grid.Children)
                {
                    if (gridChild is Label label && label.ClassId == "displayLabel")
                    {
                        textLabel = label;
                        break;
                    }
                }

                if (textLabel != null && textLabel.IsVisible)
                {
                    // Get text from either Text property or FormattedText spans
                    var text = textLabel.Text;
                    if (string.IsNullOrEmpty(text) && textLabel.FormattedText != null)
                    {
                        // Extract text from FormattedText spans (used when dictation highlighting is enabled)
                        var sb = new System.Text.StringBuilder();
                        foreach (var span in textLabel.FormattedText.Spans)
                        {
                            sb.Append(span.Text);
                        }
                        text = sb.ToString();
                    }
                    text ??= string.Empty;

                    // Store original text for later restoration
                    if (!_segmentOriginalTexts.ContainsKey(border))
                    {
                        _segmentOriginalTexts[border] = (text, textLabel);
                    }
                    else
                    {
                        // Update with current text (might have changed)
                        _segmentOriginalTexts[border] = (text, textLabel);
                    }

                    // Find all occurrences in this segment
                    int index = 0;
                    while ((index = text.IndexOf(_searchQuery, index, StringComparison.OrdinalIgnoreCase)) != -1)
                    {
                        _searchMatches.Add(new SearchMatch
                        {
                            SegmentIndex = segmentIndex,
                            SegmentBorder = border,
                            TextLabel = textLabel,
                            StartIndex = index,
                            Length = _searchQuery.Length
                        });
                        index += _searchQuery.Length;
                    }
                }
            }
            segmentIndex++;
        }

        // Update UI
        if (_searchMatches.Count > 0)
        {
            _currentMatchIndex = 0;
            SearchMatchCount.Text = $"1/{_searchMatches.Count}";
            SearchMatchCount.TextColor = (Color)Resources["TextSecondary"]!;
            HighlightAllMatches();
            ScrollToCurrentMatch();
        }
        else
        {
            SearchMatchCount.Text = "0/0";
            SearchMatchCount.TextColor = Color.FromArgb("#ef4444"); // Red for no matches
        }
    }

    private void HighlightAllMatches()
    {
        // Group matches by segment
        var matchesBySegment = _searchMatches.GroupBy(m => m.SegmentBorder);

        foreach (var group in matchesBySegment)
        {
            var border = group.Key;
            var textLabel = group.First().TextLabel;
            var originalText = _segmentOriginalTexts.ContainsKey(border)
                ? _segmentOriginalTexts[border].OriginalText
                : textLabel.Text ?? string.Empty;

            // Create FormattedString with highlights
            var formattedString = new FormattedString();
            var matches = group.OrderBy(m => m.StartIndex).ToList();

            int lastEnd = 0;
            int matchIndexInSegment = 0;

            foreach (var match in matches)
            {
                // Add text before the match
                if (match.StartIndex > lastEnd)
                {
                    formattedString.Spans.Add(new Span
                    {
                        Text = originalText.Substring(lastEnd, match.StartIndex - lastEnd),
                        TextColor = (Color)Resources["TextPrimary"]!,
                        FontSize = _transcriptFontSize
                    });
                }

                // Determine if this is the current match
                var globalMatchIndex = _searchMatches.IndexOf(match);
                var isCurrentMatch = globalMatchIndex == _currentMatchIndex;

                // Add the highlighted match
                formattedString.Spans.Add(new Span
                {
                    Text = originalText.Substring(match.StartIndex, match.Length),
                    BackgroundColor = isCurrentMatch
                        ? Color.FromArgb("#f97316") // Orange for current
                        : Color.FromArgb("#fbbf24"), // Yellow for others
                    TextColor = Colors.Black,
                    FontAttributes = isCurrentMatch ? FontAttributes.Bold : FontAttributes.None,
                    FontSize = _transcriptFontSize
                });

                lastEnd = match.StartIndex + match.Length;
                matchIndexInSegment++;
            }

            // Add remaining text after last match
            if (lastEnd < originalText.Length)
            {
                formattedString.Spans.Add(new Span
                {
                    Text = originalText.Substring(lastEnd),
                    TextColor = (Color)Resources["TextPrimary"]!,
                    FontSize = _transcriptFontSize
                });
            }

            textLabel.FormattedText = formattedString;
        }
    }

    private void ClearSearchHighlights()
    {
        var textSecondary = (Color)Resources["TextSecondary"]!;
        var accentText = (Color)Resources["AccentText"]!;

        foreach (var kvp in _segmentOriginalTexts)
        {
            var (originalText, textLabel) = kvp.Value;

            // Always use FormattedText for consistent line height
            if (_settingsService.EnableDictationFormatting)
            {
                textLabel.FormattedText = BuildHighlightedText(originalText, textSecondary, accentText);
            }
            else
            {
                // Single span with no highlighting
                textLabel.FormattedText = new FormattedString
                {
                    Spans = { new Span { Text = originalText, TextColor = textSecondary } }
                };
            }
        }
    }

    private void NavigateToNextMatch()
    {
        if (_searchMatches.Count == 0)
        {
            return;
        }

        _currentMatchIndex = (_currentMatchIndex + 1) % _searchMatches.Count;
        UpdateMatchNavigation();
    }

    private void NavigateToPreviousMatch()
    {
        if (_searchMatches.Count == 0)
        {
            return;
        }

        _currentMatchIndex = (_currentMatchIndex - 1 + _searchMatches.Count) % _searchMatches.Count;
        UpdateMatchNavigation();
    }

    private void UpdateMatchNavigation()
    {
        SearchMatchCount.Text = $"{_currentMatchIndex + 1}/{_searchMatches.Count}";
        HighlightAllMatches(); // Re-highlight to update current match
        ScrollToCurrentMatch();
    }

    private async void ScrollToCurrentMatch()
    {
        if (_currentMatchIndex < 0 || _currentMatchIndex >= _searchMatches.Count)
        {
            return;
        }

        var match = _searchMatches[_currentMatchIndex];

        // Scroll to the segment
        await Task.Delay(50);
        try
        {
            await SegmentsScrollView.ScrollToAsync(match.SegmentBorder, ScrollToPosition.Center, true);
        }
        catch
        {
            // Ignore scroll errors
        }
    }

    #endregion

    #region Keyboard Shortcuts

    private void SetupKeyboardShortcuts()
    {
#if WINDOWS
        this.Loaded += (s, e) =>
        {
            var window = this.Window;
            if (window?.Handler?.PlatformView is Microsoft.UI.Xaml.Window nativeWindow)
            {
                nativeWindow.Content.KeyDown += OnWindowKeyDown;
            }
        };
#endif
    }

#if WINDOWS
    private void OnWindowKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        var ctrlPressed = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control)
            .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
        var shiftPressed = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Shift)
            .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);

        // Ctrl+F - Open search
        if (ctrlPressed && e.Key == Windows.System.VirtualKey.F)
        {
            MainThread.BeginInvokeOnMainThread(() => OpenSearch());
            e.Handled = true;
        }
        // Ctrl+O - Open/Browse file
        else if (ctrlPressed && e.Key == Windows.System.VirtualKey.O)
        {
            MainThread.BeginInvokeOnMainThread(() => OnSelectFileClicked(null, null!));
            e.Handled = true;
        }
        // Ctrl+S - Export/Save transcript
        else if (ctrlPressed && e.Key == Windows.System.VirtualKey.S)
        {
            if (_currentSegments.Count > 0)
            {
                MainThread.BeginInvokeOnMainThread(() => ShowExportPanel());
            }
            e.Handled = true;
        }
        // Space - Play/Pause audio (when not editing and not in text entry)
        else if (e.Key == Windows.System.VirtualKey.Space && !_isEditMode && !SearchEntry.IsFocused)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                // Determine which player to control based on context
                // Priority: Playing player > Main player with loaded audio > History player with loaded audio
                if (_audioPlayer.IsPlaying)
                {
                    OnPlayPauseClicked(null, null!);
                }
                else if (_historyAudioPlayer.IsPlaying)
                {
                    OnHistoryPlayPauseClicked(null, null!);
                }
                else if (PlayerPanel.IsVisible && _selectedFilePath != null)
                {
                    // Main player has audio loaded - start playback
                    OnPlayPauseClicked(null, null!);
                }
                else if (!string.IsNullOrEmpty(_historyAudioFilePath))
                {
                    // History player has audio loaded - start playback
                    OnHistoryPlayPauseClicked(null, null!);
                }
            });
            e.Handled = true;
        }
        // Left Arrow - Seek backward 5 seconds
        else if (e.Key == Windows.System.VirtualKey.Left && !_isEditMode && !SearchEntry.IsFocused)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (PlayerPanel.IsVisible && _selectedFilePath != null)
                {
                    SeekRelative(-5);
                }
                else if (!string.IsNullOrEmpty(_historyAudioFilePath))
                {
                    SeekHistoryRelative(-5);
                }
            });
            e.Handled = true;
        }
        // Right Arrow - Seek forward 5 seconds
        else if (e.Key == Windows.System.VirtualKey.Right && !_isEditMode && !SearchEntry.IsFocused)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (PlayerPanel.IsVisible && _selectedFilePath != null)
                {
                    SeekRelative(5);
                }
                else if (!string.IsNullOrEmpty(_historyAudioFilePath))
                {
                    SeekHistoryRelative(5);
                }
            });
            e.Handled = true;
        }
        // Escape - Close settings/search/dropdown/shortcuts
        else if (e.Key == Windows.System.VirtualKey.Escape)
        {
            if (KeyboardShortcutsOverlay.IsVisible)
            {
                MainThread.BeginInvokeOnMainThread(() => HideKeyboardShortcutsPanel());
                e.Handled = true;
            }
            else if (LanguageDropdownMenu.IsVisible)
            {
                MainThread.BeginInvokeOnMainThread(() => LanguageDropdownMenu.IsVisible = false);
                e.Handled = true;
            }
            else if (SettingsOverlay.IsVisible)
            {
                MainThread.BeginInvokeOnMainThread(() => SettingsOverlay.IsVisible = false);
                e.Handled = true;
            }
            else if (SearchBar.IsVisible)
            {
                MainThread.BeginInvokeOnMainThread(() => CloseSearch());
                e.Handled = true;
            }
        }
        // F3 or Ctrl+G - Next match
        else if ((e.Key == Windows.System.VirtualKey.F3 || (ctrlPressed && e.Key == Windows.System.VirtualKey.G)) && SearchBar.IsVisible)
        {
            if (shiftPressed)
            {
                MainThread.BeginInvokeOnMainThread(() => NavigateToPreviousMatch());
            }
            else
            {
                MainThread.BeginInvokeOnMainThread(() => NavigateToNextMatch());
            }

            e.Handled = true;
        }
        // F1 - Toggle keyboard shortcuts panel
        else if (e.Key == Windows.System.VirtualKey.F1)
        {
            MainThread.BeginInvokeOnMainThread(() => ToggleKeyboardShortcutsPanel());
            e.Handled = true;
        }
        // Ctrl+T - Start transcription
        else if (ctrlPressed && e.Key == Windows.System.VirtualKey.T)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                // Only start transcription if we have a file loaded and are on Audio tab
                if (_selectedFilePath != null && _isFileTabActive && TranscribeButton.IsVisible)
                {
                    OnTranscribeClicked(null, EventArgs.Empty);
                }
            });
            e.Handled = true;
        }
        // Ctrl+R - Start/Stop recording
        else if (ctrlPressed && e.Key == Windows.System.VirtualKey.R)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                // Switch to Audio tab if needed, then toggle recording
                if (!_isFileTabActive)
                {
                    OnFileTabClicked(null, null!);
                }
                OnRecordClicked(null, null!);
            });
            e.Handled = true;
        }
        // Tab - Switch between Audio and History tabs (when not in text entry)
        else if (e.Key == Windows.System.VirtualKey.Tab && !SearchEntry.IsFocused && !_isEditMode)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (_isFileTabActive)
                {
                    OnHistoryTabClicked(null, null!);
                }
                else
                {
                    OnFileTabClicked(null, null!);
                }
            });
            e.Handled = true;
        }
        // Ctrl+Shift+C - Copy transcript to clipboard
        else if (ctrlPressed && shiftPressed && e.Key == Windows.System.VirtualKey.C)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (_currentSegments.Count > 0)
                {
                    OnCopyClicked(null, null!);
                }
            });
            e.Handled = true;
        }
        // Home - Jump to beginning of audio
        else if (e.Key == Windows.System.VirtualKey.Home && !SearchEntry.IsFocused && !_isEditMode)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (PlayerPanel.IsVisible && _selectedFilePath != null)
                {
                    _audioPlayer.Seek(TimeSpan.Zero);
                }
                else if (!string.IsNullOrEmpty(_historyAudioFilePath))
                {
                    _historyAudioPlayer.Seek(TimeSpan.Zero);
                }
            });
            e.Handled = true;
        }
        // End - Jump to end of audio
        else if (e.Key == Windows.System.VirtualKey.End && !SearchEntry.IsFocused && !_isEditMode)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (PlayerPanel.IsVisible && _selectedFilePath != null)
                {
                    _audioPlayer.Seek(_audioPlayer.Duration);
                }
                else if (!string.IsNullOrEmpty(_historyAudioFilePath))
                {
                    _historyAudioPlayer.Seek(_historyAudioPlayer.Duration);
                }
            });
            e.Handled = true;
        }
        // ] - Increase playback speed
        else if (e.Key == Windows.System.VirtualKey.Number221 && !SearchEntry.IsFocused) // ] key
        {
            MainThread.BeginInvokeOnMainThread(() => OnSpeedIncreaseClicked(null, null!));
            e.Handled = true;
        }
        // [ - Decrease playback speed
        else if (e.Key == Windows.System.VirtualKey.Number219 && !SearchEntry.IsFocused) // [ key
        {
            MainThread.BeginInvokeOnMainThread(() => OnSpeedDecreaseClicked(null, null!));
            e.Handled = true;
        }
        // Ctrl+, - Open settings
        else if (ctrlPressed && e.Key == Windows.System.VirtualKey.Number188) // , key
        {
            MainThread.BeginInvokeOnMainThread(() => OnSettingsClicked(null, null!));
            e.Handled = true;
        }
        // Ctrl+D - Toggle view mode (Segments/Document)
        else if (ctrlPressed && e.Key == Windows.System.VirtualKey.D)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (_currentSegments.Count > 0)
                {
                    if (_isDocumentView)
                    {
                        OnViewModeSegmentsClicked(null, null!);
                    }
                    else
                    {
                        OnViewModeDocumentClicked(null, null!);
                    }
                }
            });
            e.Handled = true;
        }
        // Delete - Clear current file
        else if (e.Key == Windows.System.VirtualKey.Delete && !SearchEntry.IsFocused && !_isEditMode)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (_selectedFilePath != null && _isFileTabActive)
                {
                    OnClearFileClicked(null, null!);
                }
            });
            e.Handled = true;
        }
    }
#endif

    #endregion
}
