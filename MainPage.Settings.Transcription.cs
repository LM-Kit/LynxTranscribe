using LMKit.Speech.Dictation;
using LynxTranscribe.Localization;
using L = LynxTranscribe.Localization.LocalizationService;

namespace LynxTranscribe;

/// <summary>
/// Settings: Transcription options (model mode, VAD, dictation help)
/// </summary>
public partial class MainPage
{
    #region Model Mode Selection

    private void OnTurboModeSelected(object? sender, TappedEventArgs e)
    {
        if (_isTranscribing || _isModelLoading)
        {
            return;
        }

        _useAccurateMode = false;
        _settingsService.UseAccurateMode = false;
        UpdateModelModeUI();
        UpdateSettingsModelModeUI();
    }

    private void OnAccurateModeSelected(object? sender, TappedEventArgs e)
    {
        if (_isTranscribing || _isModelLoading)
        {
            return;
        }

        _useAccurateMode = true;
        _settingsService.UseAccurateMode = true;
        UpdateModelModeUI();
        UpdateSettingsModelModeUI();
    }

    private void OnSettingsTurboClicked(object? sender, TappedEventArgs e)
    {
        if (_isTranscribing || _isModelLoading)
        {
            return;
        }

        _useAccurateMode = false;
        _settingsService.UseAccurateMode = false;
        UpdateModelModeUI();
        UpdateSettingsModelModeUI();
    }

    private void OnSettingsAccurateClicked(object? sender, TappedEventArgs e)
    {
        if (_isTranscribing || _isModelLoading)
        {
            return;
        }

        _useAccurateMode = true;
        _settingsService.UseAccurateMode = true;
        UpdateModelModeUI();
        UpdateSettingsModelModeUI();
    }

    private void OnSettingsTurboHoverEnter(object? sender, PointerEventArgs e)
    {
        if (_isTranscribing || _isModelLoading)
        {
            return;
        }

        if (_useAccurateMode)
        {
            SettingsTurboButton.BackgroundColor = (Color)Resources["SurfaceColor"]!;
        }
    }

    private void OnSettingsTurboHoverExit(object? sender, PointerEventArgs e)
    {
        if (_useAccurateMode)
        {
            SettingsTurboButton.BackgroundColor = (Color)Resources["BackgroundTertiary"]!;
        }
    }

    private void OnSettingsAccurateHoverEnter(object? sender, PointerEventArgs e)
    {
        if (_isTranscribing || _isModelLoading)
        {
            return;
        }

        if (!_useAccurateMode)
        {
            SettingsAccurateButton.BackgroundColor = (Color)Resources["SurfaceColor"]!;
        }
    }

    private void OnSettingsAccurateHoverExit(object? sender, PointerEventArgs e)
    {
        if (!_useAccurateMode)
        {
            SettingsAccurateButton.BackgroundColor = (Color)Resources["BackgroundTertiary"]!;
        }
    }

    private void UpdateModelModeUI()
    {
        // Use centralized theme system for colors
        ApplyStyle(AccurateButton, _useAccurateMode ? ControlStyle.ModeSelected : ControlStyle.ModeUnselected, AccurateButtonLabel);
        ApplyStyle(TurboButton, _useAccurateMode ? ControlStyle.ModeUnselected : ControlStyle.ModeSelected, TurboButtonLabel);

        // Handle font attributes
        AccurateButtonLabel.FontAttributes = _useAccurateMode ? FontAttributes.Bold : FontAttributes.None;
        TurboButtonLabel.FontAttributes = _useAccurateMode ? FontAttributes.None : FontAttributes.Bold;
    }

    private void UpdateSettingsModelModeUI()
    {
        if (_useAccurateMode)
        {
            // Accurate selected
            SettingsAccurateButton.BackgroundColor = (Color)Resources["AccentSurface"]!;
            SettingsAccurateButton.Stroke = (Color)Resources["AccentPrimary"]!;
            SettingsAccurateIcon.Text = "✓";
            SettingsAccurateIcon.TextColor = (Color)Resources["AccentText"]!;
            SettingsAccurateLabel.TextColor = (Color)Resources["AccentText"]!;
            SettingsAccurateLabel.FontAttributes = FontAttributes.Bold;

            SettingsTurboButton.BackgroundColor = (Color)Resources["BackgroundTertiary"]!;
            SettingsTurboButton.Stroke = (Color)Resources["SurfaceBorder"]!;
            SettingsTurboLabel.TextColor = (Color)Resources["TextSecondary"]!;
            SettingsTurboLabel.FontAttributes = FontAttributes.None;
        }
        else
        {
            // Turbo selected
            SettingsTurboButton.BackgroundColor = (Color)Resources["AccentSurface"]!;
            SettingsTurboButton.Stroke = (Color)Resources["AccentPrimary"]!;
            SettingsTurboLabel.TextColor = (Color)Resources["AccentText"]!;
            SettingsTurboLabel.FontAttributes = FontAttributes.Bold;

            SettingsAccurateButton.BackgroundColor = (Color)Resources["BackgroundTertiary"]!;
            SettingsAccurateButton.Stroke = (Color)Resources["SurfaceBorder"]!;
            SettingsAccurateIcon.Text = "✓";
            SettingsAccurateIcon.TextColor = (Color)Resources["TextSecondary"]!;
            SettingsAccurateLabel.TextColor = (Color)Resources["TextSecondary"]!;
            SettingsAccurateLabel.FontAttributes = FontAttributes.None;
        }
    }

    #endregion

    #region VAD Toggle

    private void OnVadToggleClicked(object? sender, TappedEventArgs e)
    {
        if (_isTranscribing || _isModelLoading)
        {
            return;
        }

        _enableVoiceActivityDetection = !_enableVoiceActivityDetection;
        _settingsService.EnableVoiceActivityDetection = _enableVoiceActivityDetection;

        // Sync with settings panel switch
        VadToggle.IsToggled = _enableVoiceActivityDetection;

        // Update all VAD UI elements
        UpdateVadToggleUI();
        UpdateVadStatus();
    }

    private void UpdateVadToggleUI()
    {
        if (_enableVoiceActivityDetection)
        {
            // On state: accent colors, knob to the right
            VadToggleButton.BackgroundColor = (Color)Resources["AccentPrimary"]!;
            VadToggleButton.Stroke = (Color)Resources["AccentPrimary"]!;
            VadToggleKnob.BackgroundColor = Colors.White;
            VadToggleKnob.HorizontalOptions = LayoutOptions.End;
            VadToggleLabel.TextColor = (Color)Resources["AccentText"]!;
            VadToggleLabel.FontAttributes = FontAttributes.Bold;
        }
        else
        {
            // Off state: muted colors, knob to the left
            VadToggleButton.BackgroundColor = (Color)Resources["BackgroundTertiary"]!;
            VadToggleButton.Stroke = (Color)Resources["SurfaceBorder"]!;
            VadToggleKnob.BackgroundColor = (Color)Resources["TextMuted"]!;
            VadToggleKnob.HorizontalOptions = LayoutOptions.Start;
            VadToggleLabel.TextColor = (Color)Resources["TextSecondary"]!;
            VadToggleLabel.FontAttributes = FontAttributes.None;
        }
    }

    private void OnVadToggleHoverEnter(object? sender, PointerEventArgs e)
    {
        if (_isTranscribing || _isModelLoading)
        {
            return;
        }

        if (!_enableVoiceActivityDetection)
        {
            VadToggleButton.BackgroundColor = (Color)Resources["SurfaceColor"]!;
        }
    }

    private void OnVadToggleHoverExit(object? sender, PointerEventArgs e)
    {
        if (!_enableVoiceActivityDetection)
        {
            VadToggleButton.BackgroundColor = (Color)Resources["BackgroundTertiary"]!;
        }
    }

    #endregion

    #region Dictation Help

    private void OnDictationHelpClicked(object? sender, TappedEventArgs e)
    {
        PopulateDictationCommandsList();
        DictationHelpOverlay.IsVisible = true;
    }

    private void OnDictationHelpOverlayClicked(object? sender, TappedEventArgs e)
    {
        DictationHelpOverlay.IsVisible = false;
    }

    private void OnDictationHelpCloseClicked(object? sender, TappedEventArgs e)
    {
        DictationHelpOverlay.IsVisible = false;
    }

    private void OnDictationHelpToolbarHoverEnter(object? sender, PointerEventArgs e)
    {
        DictationHelpToolbarButton.BackgroundColor = (Color)Resources["AccentSurface"]!;
        DictationHelpToolbarButton.Stroke = (Color)Resources["AccentMuted"]!;
        DictationHelpToolbarButtonLabel.TextColor = (Color)Resources["AccentText"]!;
    }

    private void OnDictationHelpToolbarHoverExit(object? sender, PointerEventArgs e)
    {
        DictationHelpToolbarButton.BackgroundColor = (Color)Resources["BackgroundTertiary"]!;
        DictationHelpToolbarButton.Stroke = (Color)Resources["SurfaceBorder"]!;
        DictationHelpToolbarButtonLabel.TextColor = (Color)Resources["TextTertiary"]!;
    }

    private void OnDictationHelpHoverEnter(object? sender, PointerEventArgs e)
    {
        DictationHelpButton.BackgroundColor = (Color)Resources["SurfaceColor"]!;
    }

    private void OnDictationHelpHoverExit(object? sender, PointerEventArgs e)
    {
        DictationHelpButton.BackgroundColor = Colors.Transparent;
    }

    private void OnDictationHelpCloseHoverEnter(object? sender, PointerEventArgs e)
    {
        DictationHelpCloseButton.BackgroundColor = (Color)Resources["DangerSurface"]!;
    }

    private void OnDictationHelpCloseHoverExit(object? sender, PointerEventArgs e)
    {
        DictationHelpCloseButton.BackgroundColor = Colors.Transparent;
    }

    private void PopulateDictationCommandsList()
    {
        DictationCommandsList.Children.Clear();

        // Get commands for current language
        var currentLang = L.Instance.CurrentLanguage;
        var commands = Formatter.GetAllCommandsForDisplay(currentLang);
        var textPrimary = (Color)Resources["TextPrimary"]!;
        var textSecondary = (Color)Resources["TextSecondary"]!;
        var accentText = (Color)Resources["AccentText"]!;
        var backgroundTertiary = (Color)Resources["BackgroundTertiary"]!;
        var surfaceBorder = (Color)Resources["SurfaceBorder"]!;

        string? lastCategory = null;
        int rowIndex = 0;

        foreach (var command in commands)
        {
            // Add category header if changed
            if (command.Category != lastCategory)
            {
                var categoryLabel = new Label
                {
                    Text = GetCategoryDisplayName(command.Category),
                    FontSize = 11,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = accentText,
                    Margin = new Thickness(0, lastCategory != null ? 20 : 0, 0, 8)
                };
                DictationCommandsList.Children.Add(categoryLabel);
                lastCategory = command.Category;
                rowIndex = 0;
            }

            // Command row with alternating background
            var rowBorder = new Border
            {
                BackgroundColor = rowIndex % 2 == 0 ? Colors.Transparent : backgroundTertiary,
                StrokeThickness = 0,
                Padding = new Thickness(8, 6),
                Margin = new Thickness(-8, 0)
            };

            var row = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition(new GridLength(1, GridUnitType.Star)),
                    new ColumnDefinition(new GridLength(60, GridUnitType.Absolute))
                }
            };

            var spokenLabel = new Label
            {
                Text = command.SpokenExample,
                FontSize = 13,
                TextColor = textPrimary,
                VerticalOptions = LayoutOptions.Center
            };
            row.Add(spokenLabel, 0);

            var resultLabel = new Label
            {
                Text = command.DisplayResult,
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
                TextColor = accentText,
                HorizontalOptions = LayoutOptions.End,
                VerticalOptions = LayoutOptions.Center
            };
            row.Add(resultLabel, 1);

            rowBorder.Content = row;
            DictationCommandsList.Children.Add(rowBorder);
            rowIndex++;
        }
    }

    private string GetCategoryDisplayName(string category)
    {
        return category switch
        {
            "LineBreaks" => L.Localize(StringKeys.DictationCategoryLineBreaks),
            "PunctuationWithLineBreak" => L.Localize(StringKeys.DictationCategoryPunctuationLineBreak),
            "Punctuation" => L.Localize(StringKeys.DictationCategoryPunctuation),
            "Brackets" => L.Localize(StringKeys.DictationCategoryBrackets),
            "Quotes" => L.Localize(StringKeys.DictationCategoryQuotes),
            "Symbols" => L.Localize(StringKeys.DictationCategorySymbols),
            "TextFormatting" => L.Localize(StringKeys.DictationCategoryTextFormatting),
            _ => category
        };
    }

    #endregion
}
