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

        // Shortcut descriptions
        ShortcutOpenFileLabel.Text = L.Localize(StringKeys.ShortcutOpenFile);
        ShortcutExportLabel.Text = L.Localize(StringKeys.ShortcutExport);
        ShortcutClosePanelLabel.Text = L.Localize(StringKeys.ShortcutClosePanel);
        ShortcutShowShortcutsLabel.Text = L.Localize(StringKeys.ShortcutShowShortcuts);
        ShortcutPlayPauseLabel.Text = L.Localize(StringKeys.ShortcutPlayPause);
        ShortcutSeekBackLabel.Text = L.Localize(StringKeys.ShortcutSeekBack);
        ShortcutSeekForwardLabel.Text = L.Localize(StringKeys.ShortcutSeekForward);
        ShortcutSearchLabel.Text = L.Localize(StringKeys.ShortcutSearch);
        ShortcutNextMatchLabel.Text = L.Localize(StringKeys.ShortcutNextMatch);
        ShortcutPrevMatchLabel.Text = L.Localize(StringKeys.ShortcutPrevMatch);

        // Footer
        ShortcutFooterHint.Text = L.Localize(StringKeys.PressEscToClose);
    }

    #endregion
}
