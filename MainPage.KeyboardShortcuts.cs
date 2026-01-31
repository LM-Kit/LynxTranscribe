using LynxTranscribe.Localization;
using L = LynxTranscribe.Localization.LocalizationService;

namespace LynxTranscribe;

/// <summary>
/// Keyboard Shortcuts UI: Display and manage keyboard shortcuts reference panel
/// </summary>
public partial class MainPage
{
    #region Keyboard Shortcuts Panel

    private void ShowKeyboardShortcutsPanel()
    {
        KeyboardShortcutsOverlay.IsVisible = true;
    }

    private void HideKeyboardShortcutsPanel()
    {
        KeyboardShortcutsOverlay.IsVisible = false;
    }

    private void ToggleKeyboardShortcutsPanel()
    {
        if (KeyboardShortcutsOverlay.IsVisible)
        {
            HideKeyboardShortcutsPanel();
        }
        else
        {
            ShowKeyboardShortcutsPanel();
        }
    }

    private void OnKeyboardShortcutsOverlayClicked(object? sender, TappedEventArgs e)
    {
        HideKeyboardShortcutsPanel();
    }

    private void OnCloseShortcutsClicked(object? sender, TappedEventArgs e)
    {
        HideKeyboardShortcutsPanel();
    }

    private void OnCloseShortcutsHoverEnter(object? sender, PointerEventArgs e)
    {
        ApplyStyle(CloseShortcutsButton, ControlStyle.ButtonTransparentHover);
    }

    private void OnCloseShortcutsHoverExit(object? sender, PointerEventArgs e)
    {
        ApplyStyle(CloseShortcutsButton, ControlStyle.ButtonTransparent);
    }

    private void OnShortcutsButtonClicked(object? sender, TappedEventArgs e)
    {
        ShowKeyboardShortcutsPanel();
    }

    private void OnShortcutsButtonHoverEnter(object? sender, PointerEventArgs e)
    {
        var accentSurface = (Color)Resources["AccentSurface"]!;
        var accentMuted = (Color)Resources["AccentMuted"]!;
        var accentText = (Color)Resources["AccentText"]!;

        ShortcutsButtonBorder.BackgroundColor = accentSurface;
        ShortcutsButtonBorder.Stroke = accentMuted;
        ShortcutsButtonLabel.TextColor = accentText;
    }

    private void OnShortcutsButtonHoverExit(object? sender, PointerEventArgs e)
    {
        var backgroundTertiary = (Color)Resources["BackgroundTertiary"]!;
        var surfaceBorder = (Color)Resources["SurfaceBorder"]!;
        var textSecondary = (Color)Resources["TextSecondary"]!;

        ShortcutsButtonBorder.BackgroundColor = backgroundTertiary;
        ShortcutsButtonBorder.Stroke = surfaceBorder;
        ShortcutsButtonLabel.TextColor = textSecondary;
    }

    /// <summary>
    /// Refreshes localized strings for keyboard shortcuts panel
    /// </summary>
    private void RefreshKeyboardShortcutsStrings()
    {
        KeyboardShortcutsTitle.Text = L.Localize(StringKeys.KeyboardShortcuts);
        KeyboardShortcutsDescription.Text = L.Localize(StringKeys.KeyboardShortcutsDescription);

        // Category labels
        ShortcutCategoryGeneralLabel.Text = L.Localize(StringKeys.ShortcutCategoryGeneral);
        ShortcutCategoryPlaybackLabel.Text = L.Localize(StringKeys.ShortcutCategoryPlayback);
        ShortcutCategoryTranscriptLabel.Text = L.Localize(StringKeys.ShortcutCategoryTranscript);

        // Shortcut descriptions - General
        ShortcutOpenFileLabel.Text = L.Localize(StringKeys.ShortcutOpenFile);
        ShortcutExportLabel.Text = L.Localize(StringKeys.ShortcutExport);
        ShortcutClosePanelLabel.Text = L.Localize(StringKeys.ShortcutClosePanel);
        ShortcutShowShortcutsLabel.Text = L.Localize(StringKeys.ShortcutShowShortcuts);
        ShortcutStartTranscriptionLabel.Text = L.Localize(StringKeys.ShortcutStartTranscription);
        ShortcutStartRecordingLabel.Text = L.Localize(StringKeys.ShortcutStartRecording);
        ShortcutSwitchTabsLabel.Text = L.Localize(StringKeys.ShortcutSwitchTabs);
        ShortcutClearFileLabel.Text = L.Localize(StringKeys.ShortcutClearFile);

        // Shortcut descriptions - Playback
        ShortcutPlayPauseLabel.Text = L.Localize(StringKeys.ShortcutPlayPause);
        ShortcutSeekBackLabel.Text = L.Localize(StringKeys.ShortcutSeekBack);
        ShortcutSeekForwardLabel.Text = L.Localize(StringKeys.ShortcutSeekForward);
        ShortcutJumpStartLabel.Text = L.Localize(StringKeys.ShortcutJumpStart);
        ShortcutJumpEndLabel.Text = L.Localize(StringKeys.ShortcutJumpEnd);
        ShortcutSpeedUpLabel.Text = L.Localize(StringKeys.ShortcutSpeedUp);
        ShortcutSpeedDownLabel.Text = L.Localize(StringKeys.ShortcutSpeedDown);

        // Shortcut descriptions - Transcript
        ShortcutSearchLabel.Text = L.Localize(StringKeys.ShortcutSearch);
        ShortcutNextMatchLabel.Text = L.Localize(StringKeys.ShortcutNextMatch);
        ShortcutPrevMatchLabel.Text = L.Localize(StringKeys.ShortcutPrevMatch);
        ShortcutCopyTranscriptLabel.Text = L.Localize(StringKeys.ShortcutCopyTranscript);
        ShortcutToggleViewLabel.Text = L.Localize(StringKeys.ShortcutToggleView);

        // Footer
        ShortcutFooterHint.Text = L.Localize(StringKeys.PressEscToClose);
    }

    #endregion
}
