using LynxTranscribe.Helpers;
using LynxTranscribe.Localization;
using L = LynxTranscribe.Localization.LocalizationService;

namespace LynxTranscribe;

/// <summary>
/// Settings: UI preferences (view mode toggle, resource usage)
/// </summary>
public partial class MainPage
{
    #region View Mode Toggle

    private void OnViewModeSegmentsClicked(object? sender, TappedEventArgs e)
    {
        if (!_isDocumentView)
        {
            return;
        }

        _isDocumentView = false;
        UpdateViewModeUI();
        UpdateViewModeContent();
    }

    private void OnViewModeDocumentClicked(object? sender, TappedEventArgs e)
    {
        if (_isDocumentView)
        {
            return;
        }

        // Exit edit mode when switching to document view
        if (_isEditMode)
        {
            _isEditMode = false;
            UpdateEditToggleUI();
            UpdateSegmentEditability();
        }

        _isDocumentView = true;
        UpdateViewModeUI();
        UpdateViewModeContent();
    }

    private void OnViewModeSegmentsHoverEnter(object? sender, PointerEventArgs e)
    {
        if (_isDocumentView)
        {
            ViewModeSegments.BackgroundColor = (Color)Resources["SurfaceBorder"]!;
        }
    }

    private void OnViewModeSegmentsHoverExit(object? sender, PointerEventArgs e)
    {
        if (_isDocumentView)
        {
            ViewModeSegments.BackgroundColor = Colors.Transparent;
        }
    }

    private void OnViewModeDocumentHoverEnter(object? sender, PointerEventArgs e)
    {
        if (!_isDocumentView)
        {
            ViewModeDocument.BackgroundColor = (Color)Resources["SurfaceBorder"]!;
        }
    }

    private void OnViewModeDocumentHoverExit(object? sender, PointerEventArgs e)
    {
        if (!_isDocumentView)
        {
            ViewModeDocument.BackgroundColor = Colors.Transparent;
        }
    }

    private void UpdateViewModeUI()
    {
        var accentPrimary = (Color)Resources["AccentPrimary"]!;
        var textSecondary = (Color)Resources["TextSecondary"]!;
        var activeTextColor = Color.FromArgb("#1C1508"); // Dark brown for good contrast on amber

        if (_isDocumentView)
        {
            // Segments is inactive
            ViewModeSegments.BackgroundColor = Colors.Transparent;
            ViewModeSegmentsLabel.TextColor = textSecondary;
            ViewModeSegmentsLabel.FontAttributes = FontAttributes.None;

            // Document is active - solid pill
            ViewModeDocument.BackgroundColor = accentPrimary;
            ViewModeDocumentLabel.TextColor = activeTextColor;
            ViewModeDocumentLabel.FontAttributes = FontAttributes.Bold;

            // Disable Edit and Search in document view
            EditToggle.Opacity = 0.4;
            EditToggle.InputTransparent = true;
            SearchButton.Opacity = 0.4;
            SearchButton.InputTransparent = true;
        }
        else
        {
            // Segments is active - solid pill
            ViewModeSegments.BackgroundColor = accentPrimary;
            ViewModeSegmentsLabel.TextColor = activeTextColor;
            ViewModeSegmentsLabel.FontAttributes = FontAttributes.Bold;

            // Document is inactive
            ViewModeDocument.BackgroundColor = Colors.Transparent;
            ViewModeDocumentLabel.TextColor = textSecondary;
            ViewModeDocumentLabel.FontAttributes = FontAttributes.None;

            // Enable Edit and Search in segments view
            EditToggle.Opacity = 1.0;
            EditToggle.InputTransparent = false;
            SearchButton.Opacity = 1.0;
            SearchButton.InputTransparent = false;
        }

        // Toggle view visibility
        SegmentsScrollView.IsVisible = !_isDocumentView;
        DocumentScrollView.IsVisible = _isDocumentView;
    }

    private void UpdateViewModeContent()
    {
        if (_isDocumentView)
        {
            // Generate formatted document text
            var formattedText = GenerateFormattedDocument();
            DocumentLabel.Text = formattedText;

            // Apply current font size
            DocumentLabel.FontSize = _transcriptFontSize;
        }
    }

    private string GenerateFormattedDocument()
    {
        // Reuse the same logic as copy/export
        return GetCurrentTranscriptText();
    }

    #endregion

    #region Resource Usage Settings

    private void OnResourceLevel1Clicked(object? sender, TappedEventArgs e)
    {
        SetResourceUsageLevel(1);
    }

    private void OnResourceLevel2Clicked(object? sender, TappedEventArgs e)
    {
        SetResourceUsageLevel(2);
    }

    private void OnResourceLevel3Clicked(object? sender, TappedEventArgs e)
    {
        SetResourceUsageLevel(3);
    }

    private void OnResourceLevel4Clicked(object? sender, TappedEventArgs e)
    {
        SetResourceUsageLevel(4);
    }

    private void SetResourceUsageLevel(int level)
    {
        _settingsService.ResourceUsageLevel = level;
        SystemEx.ApplyResourceUsage(_settingsService);
        UpdateResourceUsageLevelUI();
    }

    private void OnResourceLevelHoverEnter(object? sender, PointerEventArgs e)
    {
        if (sender is Border border)
        {
            // Only apply hover if not selected
            var currentLevel = _settingsService.ResourceUsageLevel;
            var borderLevel = GetBorderLevel(border);
            if (borderLevel != currentLevel)
            {
                border.BackgroundColor = (Color)Resources["SurfaceColor"]!;
            }
        }
    }

    private void OnResourceLevelHoverExit(object? sender, PointerEventArgs e)
    {
        if (sender is Border border)
        {
            var currentLevel = _settingsService.ResourceUsageLevel;
            var borderLevel = GetBorderLevel(border);
            if (borderLevel != currentLevel)
            {
                border.BackgroundColor = (Color)Resources["BackgroundTertiary"]!;
            }
        }
    }

    private int GetBorderLevel(Border border)
    {
        if (border == ResourceLevel1Button)
        {
            return 1;
        }

        if (border == ResourceLevel2Button)
        {
            return 2;
        }

        if (border == ResourceLevel3Button)
        {
            return 3;
        }

        if (border == ResourceLevel4Button)
        {
            return 4;
        }

        return 0;
    }

    private void UpdateResourceUsageLevelUI()
    {
        var level = _settingsService.ResourceUsageLevel;
        var r = Resources;

        var backgroundTertiary = (Color)r["BackgroundTertiary"]!;
        var surfaceBorder = (Color)r["SurfaceBorder"]!;
        var accentSurface = (Color)r["AccentSurface"]!;
        var accentPrimary = (Color)r["AccentPrimary"]!;
        var accentText = (Color)r["AccentText"]!;
        var textSecondary = (Color)r["TextSecondary"]!;
        var textMuted = (Color)r["TextMuted"]!;

        // Reset all buttons
        var buttons = new[] { ResourceLevel1Button, ResourceLevel2Button, ResourceLevel3Button, ResourceLevel4Button };
        var labels = new[] { ResourceLevel1Label, ResourceLevel2Label, ResourceLevel3Label, ResourceLevel4Label };
        var percents = new[] { ResourceLevel1Percent, ResourceLevel2Percent, ResourceLevel3Percent, ResourceLevel4Percent };

        for (int i = 0; i < 4; i++)
        {
            bool isSelected = (i + 1) == level;

            buttons[i].BackgroundColor = isSelected ? accentSurface : backgroundTertiary;
            buttons[i].Stroke = isSelected ? accentPrimary : surfaceBorder;
            labels[i].TextColor = isSelected ? accentText : textSecondary;
            labels[i].FontAttributes = isSelected ? FontAttributes.Bold : FontAttributes.None;
            percents[i].TextColor = isSelected ? accentText : textMuted;
        }

        // Update thread count indicator
        int currentThreads = SystemEx.GetCurrentThreadCount(_settingsService);
        int maxThreads = SystemEx.PhysicalCoreCount;
        ThreadCountLabel.Text = L.Localize(StringKeys.UsingThreads, currentThreads, maxThreads);
    }

    #endregion
}
