using LMKit.Speech;
using LMKit.Speech.Dictation;
using LynxTranscribe.Localization;
using L = LynxTranscribe.Localization.LocalizationService;

namespace LynxTranscribe;

/// <summary>
/// Settings: General settings panel (edit toggle, dark mode, language selection)
/// Split into partial classes:
/// - MainPage.Settings.cs (this file) - General settings
/// - MainPage.Settings.Theme.cs - Theme application
/// - MainPage.Settings.Storage.cs - Storage paths
/// - MainPage.Settings.Transcription.cs - Model mode, VAD, dictation
/// - MainPage.Settings.UI.cs - View mode, resource usage
/// </summary>
public partial class MainPage
{
    #region Edit Toggle

    private void OnEditToggleClicked(object? sender, TappedEventArgs e)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"Edit toggle clicked. Current mode: {_isEditMode}");
            _isEditMode = !_isEditMode;
            System.Diagnostics.Debug.WriteLine($"Edit mode changed to: {_isEditMode}");

            // Close search bar when entering edit mode - they're mutually exclusive
            if (_isEditMode && SearchBar.IsVisible)
            {
                CloseSearch();
            }

            // Force UI update on main thread
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                UpdateEditToggleUI();
                UpdateSegmentEditability();

                // Scroll to selected segment to maintain position
                if (_selectedSegmentIndex >= 0 && _selectedSegmentIndex < _segmentViews.Count)
                {
                    await Task.Delay(50); // Let UI settle
                    var border = _segmentViews[_selectedSegmentIndex];
                    await SegmentsScrollView.ScrollToAsync(border, ScrollToPosition.Center, false);
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OnEditToggleClicked error: {ex.Message}");
        }
    }

    private void UpdateEditToggleUI()
    {
        // Use centralized theme system for colors
        ApplyStyle(EditToggle, _isEditMode ? ControlStyle.ModeSelected : ControlStyle.ButtonDefault, EditToggleLabel);

        // Handle text
        EditToggleLabel.Text = _isEditMode ? L.Localize(StringKeys.Editing) : L.Localize(StringKeys.Edit);
    }

    private void OnEditToggleHoverEnter(object? sender, PointerEventArgs e)
    {
        if (!_isEditMode)
        {
            ApplyStyle(EditToggle, ControlStyle.ModeHover, EditToggleLabel);
        }
    }

    private void OnEditToggleHoverExit(object? sender, PointerEventArgs e)
    {
        if (!_isEditMode)
        {
            ApplyStyle(EditToggle, ControlStyle.ButtonDefault, EditToggleLabel);
        }
    }

    private void RefreshTranscriptDisplay()
    {
        if (string.IsNullOrEmpty(_currentRecordId))
        {
            return;
        }

        var record = _historyService.GetById(_currentRecordId);
        if (record == null)
        {
            return;
        }

        _currentSegments = record.Segments ?? new();
        BuildSegmentView();
    }

    #endregion

    #region Settings Panel

    private void OnSettingsClicked(object? sender, TappedEventArgs e)
    {
        SettingsOverlay.IsVisible = true;
    }

    private void OnSettingsOverlayClicked(object? sender, TappedEventArgs e)
    {
        // Close language dropdown if open
        if (LanguageDropdownMenu.IsVisible)
        {
            LanguageDropdownMenu.IsVisible = false;
        }
    }

    private void OnCloseSettingsClicked(object? sender, TappedEventArgs e)
    {
        LanguageDropdownMenu.IsVisible = false;
        SettingsOverlay.IsVisible = false;
    }

    private void OnVadToggled(object? sender, ToggledEventArgs e)
    {
        _enableVoiceActivityDetection = e.Value;
        _settingsService.EnableVoiceActivityDetection = e.Value;
        UpdateVadStatus();
        UpdateVadToggleUI();
    }

    private void OnAutoTranscribeToggled(object? sender, ToggledEventArgs e)
    {
        _settingsService.AutoTranscribeOnImport = e.Value;
        UpdateAutoTranscribeInlineUI();
    }

    private void OnAutoTranscribeInlineToggleClicked(object? sender, TappedEventArgs e)
    {
        var newValue = !_settingsService.AutoTranscribeOnImport;
        _settingsService.AutoTranscribeOnImport = newValue;
        AutoTranscribeSwitch.IsToggled = newValue;
        UpdateAutoTranscribeInlineUI();
    }

    private void UpdateAutoTranscribeInlineUI()
    {
        var enabled = _settingsService.AutoTranscribeOnImport;

        if (enabled)
        {
            // On state: accent colors, knob to the right
            AutoTranscribeInlineToggle.BackgroundColor = (Color)Resources["AccentPrimary"]!;
            AutoTranscribeInlineToggle.Stroke = (Color)Resources["AccentPrimary"]!;
            AutoTranscribeKnob.BackgroundColor = Colors.White;
            AutoTranscribeKnob.HorizontalOptions = LayoutOptions.End;
            AutoTranscribeInlineLabel.TextColor = (Color)Resources["AccentText"]!;
            AutoTranscribeInlineLabel.FontAttributes = FontAttributes.Bold;
        }
        else
        {
            // Off state: muted colors, knob to the left
            AutoTranscribeInlineToggle.BackgroundColor = (Color)Resources["BackgroundTertiary"]!;
            AutoTranscribeInlineToggle.Stroke = (Color)Resources["SurfaceBorder"]!;
            AutoTranscribeKnob.BackgroundColor = (Color)Resources["TextMuted"]!;
            AutoTranscribeKnob.HorizontalOptions = LayoutOptions.Start;
            AutoTranscribeInlineLabel.TextColor = (Color)Resources["TextSecondary"]!;
            AutoTranscribeInlineLabel.FontAttributes = FontAttributes.None;
        }
    }

    private void OnAutoTranscribeInlineHoverEnter(object? sender, PointerEventArgs e)
    {
        if (!_settingsService.AutoTranscribeOnImport)
        {
            AutoTranscribeInlineToggle.BackgroundColor = (Color)Resources["SurfaceColor"]!;
        }
    }

    private void OnAutoTranscribeInlineHoverExit(object? sender, PointerEventArgs e)
    {
        if (!_settingsService.AutoTranscribeOnImport)
        {
            AutoTranscribeInlineToggle.BackgroundColor = (Color)Resources["BackgroundTertiary"]!;
        }
    }

    private void OnOpenAfterExportToggled(object? sender, ToggledEventArgs e)
    {
        _settingsService.OpenFilesAfterExport = e.Value;
    }

    private void OnDictationFormattingToggled(object? sender, ToggledEventArgs e)
    {
        _settingsService.EnableDictationFormatting = e.Value;
        UpdateDictationIndicator();

        // Update highlighting on existing segments without rebuilding
        if (_currentSegments.Count > 0 && !_isTranscribing)
        {
            UpdateDictationHighlightingInPlace();
            UpdateFormattedWordCount();

            // Update document view if visible
            if (_isDocumentView)
            {
                UpdateViewModeContent();
            }
        }
    }

    private void UpdateDictationIndicator()
    {
        var isEnabled = _settingsService.EnableDictationFormatting;

        // Update toggle button styling based on state
        if (isEnabled)
        {
            ApplyStyle(DictationToggle, ControlStyle.ModeSelected);
            DictationToggleLabel.TextColor = (Color)Resources["TextPrimary"]!;
        }
        else
        {
            ApplyStyle(DictationToggle, ControlStyle.ButtonDefault);
            DictationToggleLabel.TextColor = (Color)Resources["TextPrimary"]!;
        }
    }

    private void OnDictationToggleClicked(object? sender, TappedEventArgs e)
    {
        // Toggle the switch - OnDictationFormattingToggled handles all updates
        DictationFormattingSwitch.IsToggled = !DictationFormattingSwitch.IsToggled;
    }

    /// <summary>
    /// Updates the word count label based on current segments and dictation formatting setting.
    /// </summary>
    private void UpdateFormattedWordCount()
    {
        if (_currentSegments == null || _currentSegments.Count == 0)
        {
            return;
        }

        var wordCount = CalculateFormattedWordCount(_currentSegments, _settingsService.EnableDictationFormatting);
        WordCountLabel.Text = L.Localize(StringKeys.Words, wordCount);
    }

    /// <summary>
    /// Calculates word count, optionally applying dictation formatting first.
    /// </summary>
    private static int CalculateFormattedWordCount(List<AudioSegment> segments, bool applyDictationFormatting)
    {
        if (segments == null || segments.Count == 0)
        {
            return 0;
        }

        var combinedText = string.Join(" ", segments.Select(s => s.Text));
        if (applyDictationFormatting)
        {
            combinedText = Formatter.Format(combinedText);
        }
        return combinedText.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
    }

    /// <summary>
    /// Updates dictation highlighting on existing segments without rebuilding the view.
    /// Preserves selection and scroll position.
    /// Always uses FormattedText to maintain consistent line height.
    /// </summary>
    private void UpdateDictationHighlightingInPlace()
    {
        var textSecondary = (Color)Resources["TextSecondary"]!;
        var textPrimary = (Color)Resources["TextPrimary"]!;
        var accentText = (Color)Resources["AccentText"]!;
        var dictationEnabled = _settingsService.EnableDictationFormatting;

        for (int i = 0; i < _segmentViews.Count && i < _currentSegments.Count; i++)
        {
            var border = _segmentViews[i];
            var segment = _currentSegments[i];
            var segmentText = segment.Text.Trim();

            if (border.Content is Grid grid)
            {
                foreach (var child in grid.Children)
                {
                    if (Grid.GetColumn((BindableObject)child) == 1 && child is Label label && label.ClassId == "displayLabel")
                    {
                        // Determine color based on selection state
                        var isSelected = i == _selectedSegmentIndex;
                        var normalColor = isSelected ? textPrimary : textSecondary;

                        // Always use FormattedText to maintain consistent line height
                        label.Text = null;
                        if (dictationEnabled)
                        {
                            label.FormattedText = BuildHighlightedText(segmentText, normalColor, accentText);
                        }
                        else
                        {
                            // Single span with no highlighting
                            label.FormattedText = new FormattedString
                            {
                                Spans = { new Span { Text = segmentText, TextColor = normalColor } }
                            };
                        }
                        break;
                    }
                }
            }
        }
    }

    private void OnDictationToggleHoverEnter(object? sender, PointerEventArgs e)
    {
        if (!_settingsService.EnableDictationFormatting)
        {
            ApplyStyle(DictationToggle, ControlStyle.ButtonHover);
        }
    }

    private void OnDictationToggleHoverExit(object? sender, PointerEventArgs e)
    {
        UpdateDictationIndicator();
    }

    private void OnDarkModeToggled(object? sender, ToggledEventArgs e)
    {
        _settingsService.DarkMode = e.Value;
        ApplyTheme(e.Value);
        UpdateThemeToggleUI(e.Value);
    }

    #region Language Selection

    private List<Localization.LanguageOption> _languageOptions = Localization.LocalizationService.SupportedLanguages;

    private void InitializeLanguagePicker()
    {
        // Update the custom dropdown to show current language
        UpdateLanguageDropdownUI();
    }

    private void OnLanguageDropdownClicked(object? sender, TappedEventArgs e)
    {
        // Toggle dropdown visibility
        LanguageDropdownMenu.IsVisible = !LanguageDropdownMenu.IsVisible;
    }

    private void OnEnglishSelected(object? sender, TappedEventArgs e)
    {
        SetLanguage("en");
        LanguageDropdownMenu.IsVisible = false;
        UpdateLanguageDropdownUI();
    }

    private void OnFrenchSelected(object? sender, TappedEventArgs e)
    {
        SetLanguage("fr");
        LanguageDropdownMenu.IsVisible = false;
        UpdateLanguageDropdownUI();
    }

    private void OnLanguageOptionHoverEnter(object? sender, PointerEventArgs e)
    {
        if (sender is Border border)
        {
            var r = Resources;
            border.BackgroundColor = (Color)r["BackgroundTertiary"]!;
        }
    }

    private void OnLanguageOptionHoverExit(object? sender, PointerEventArgs e)
    {
        if (sender is Border border)
        {
            border.BackgroundColor = Colors.Transparent;
        }
    }

    private void OnLanguageButtonHoverEnter(object? sender, PointerEventArgs e)
    {
        if (sender is Border border)
        {
            var r = Resources;
            border.BackgroundColor = (Color)r["AccentSurface"]!;
            border.Stroke = (Color)r["AccentMuted"]!;
        }
    }

    private void OnLanguageButtonHoverExit(object? sender, PointerEventArgs e)
    {
        if (sender is Border border)
        {
            var r = Resources;
            border.BackgroundColor = (Color)r["BackgroundTertiary"]!;
            border.Stroke = (Color)r["SurfaceBorder"]!;
        }
    }

    private void SetLanguage(string languageCode)
    {
        _settingsService.Language = languageCode;
        Localization.LocalizationService.Instance.SetLanguage(languageCode);
    }

    private void UpdateLanguageDropdownUI()
    {
        var currentLang = _settingsService.Language;
        bool isEnglish = currentLang == "en";

        // Update selected language display
        SelectedLanguageLabel.Text = isEnglish ? "English" : "Français";
        UKFlagSelected.IsVisible = isEnglish;
        FRFlagSelected.IsVisible = !isEnglish;

        // Close dropdown if open
        LanguageDropdownMenu.IsVisible = false;
    }

    private void UpdateLanguagePickerSelection()
    {
        UpdateLanguageDropdownUI();
    }

    #endregion

    #endregion
}
