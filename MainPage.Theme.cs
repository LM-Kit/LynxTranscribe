namespace LynxTranscribe;

/// <summary>
/// Theme management: Centralized control styling with automatic refresh on theme change.
/// All hover handlers and programmatic color assignments should use these methods.
/// </summary>
public partial class MainPage
{
    #region Theme Change Event System

    /// <summary>
    /// Event fired after theme resources are updated. Subscribe to this for custom refresh logic.
    /// This is the RECOMMENDED way to handle theme changes for new components.
    /// </summary>
    public event Action? ThemeChanged;

    /// <summary>
    /// List of registered theme refresh callbacks. Use RegisterThemeRefresh to add callbacks.
    /// </summary>
    private readonly List<Action> _themeRefreshCallbacks = new();

    /// <summary>
    /// Registers a callback to be invoked when theme changes.
    /// This is forward-compatible - any new component can register here.
    /// </summary>
    public void RegisterThemeRefresh(Action callback)
    {
        if (!_themeRefreshCallbacks.Contains(callback))
        {
            _themeRefreshCallbacks.Add(callback);
        }
    }

    /// <summary>
    /// Unregisters a theme refresh callback.
    /// </summary>
    public void UnregisterThemeRefresh(Action callback)
    {
        _themeRefreshCallbacks.Remove(callback);
    }

    /// <summary>
    /// Fires theme changed event and all registered callbacks.
    /// Called automatically after ApplyTheme updates resources.
    /// </summary>
    private void NotifyThemeChanged()
    {
        // Fire event for external subscribers
        ThemeChanged?.Invoke();

        // Invoke all registered callbacks
        foreach (var callback in _themeRefreshCallbacks.ToList())
        {
            try
            {
                callback();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Theme refresh callback error: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Forces the entire visual tree to re-evaluate its layout and bindings.
    /// Call this after updating resource dictionary values.
    /// </summary>
    private void ForceVisualTreeRefresh()
    {
        // Invalidate layout to force re-evaluation of DynamicResource bindings
        this.InvalidateMeasure();

        // Walk the visual tree and invalidate each element
        InvalidateVisualTree(this.Content);
    }

    /// <summary>
    /// Recursively invalidates all elements in the visual tree.
    /// </summary>
    private void InvalidateVisualTree(IView? view)
    {
        if (view == null)
        {
            return;
        }

        if (view is VisualElement ve)
        {
            ve.InvalidateMeasure();
        }

        // Recurse into children
        if (view is Layout layout)
        {
            foreach (var child in layout.Children)
            {
                InvalidateVisualTree(child);
            }
        }
        else if (view is ContentView cv && cv.Content != null)
        {
            InvalidateVisualTree(cv.Content);
        }
        else if (view is Border border && border.Content != null)
        {
            InvalidateVisualTree(border.Content);
        }
        else if (view is ScrollView sv && sv.Content != null)
        {
            InvalidateVisualTree(sv.Content);
        }
    }

    #endregion

    #region Theme-Aware Styling System

    /// <summary>
    /// Defines visual styles for controls. Each style maps to resource dictionary colors.
    /// </summary>
    public enum ControlStyle
    {
        // Button styles
        ButtonDefault,
        ButtonHover,
        ButtonAccent,
        ButtonAccentHover,
        ButtonDanger,
        ButtonDangerHover,
        ButtonTransparent,
        ButtonTransparentHover,

        // Tab styles
        TabActive,
        TabInactive,
        TabHover,

        // Mode button styles
        ModeSelected,
        ModeUnselected,
        ModeHover,

        // Special styles
        PlayButton,
        PlayButtonHover,
        AccentSurface,
        AccentSurfaceHover,
    }

    /// <summary>
    /// Applies a style to a Border control. Always reads current theme colors from Resources.
    /// This is the ONLY method that should be used to set colors on hoverable controls.
    /// </summary>
    public void ApplyStyle(Border border, ControlStyle style, Label? label = null)
    {
        var r = this.Resources;

        switch (style)
        {
            case ControlStyle.ButtonDefault:
                border.BackgroundColor = (Color)r["BackgroundTertiary"]!;
                border.Stroke = (Color)r["SurfaceBorder"]!;
                if (label != null)
                {
                    label.TextColor = (Color)r["TextPrimary"]!;
                }

                break;

            case ControlStyle.ButtonHover:
                border.BackgroundColor = (Color)r["AccentSurface"]!;
                border.Stroke = (Color)r["AccentMuted"]!;
                if (label != null)
                {
                    label.TextColor = (Color)r["AccentText"]!;
                }

                break;

            case ControlStyle.ButtonAccent:
                border.BackgroundColor = (Color)r["AccentPrimary"]!;
                border.Stroke = Colors.Transparent;
                if (label != null)
                {
                    label.TextColor = Colors.White;
                }

                break;

            case ControlStyle.ButtonAccentHover:
                border.BackgroundColor = (Color)r["AccentMuted"]!;
                border.Stroke = Colors.Transparent;
                if (label != null)
                {
                    label.TextColor = Colors.White;
                }

                break;

            case ControlStyle.ButtonDanger:
                border.BackgroundColor = (Color)r["DangerSurface"]!;
                border.Stroke = (Color)r["DangerColor"]!;
                if (label != null)
                {
                    label.TextColor = (Color)r["DangerColor"]!;
                }

                break;

            case ControlStyle.ButtonDangerHover:
                border.BackgroundColor = (Color)r["DangerColor"]!;
                border.Stroke = (Color)r["DangerColor"]!;
                if (label != null)
                {
                    label.TextColor = Colors.White;
                }

                break;

            case ControlStyle.ButtonTransparent:
                border.BackgroundColor = Colors.Transparent;
                border.Stroke = Colors.Transparent;
                if (label != null)
                {
                    label.TextColor = (Color)r["TextPrimary"]!;
                }

                break;

            case ControlStyle.ButtonTransparentHover:
                border.BackgroundColor = (Color)r["BackgroundTertiary"]!;
                border.Stroke = Colors.Transparent;
                if (label != null)
                {
                    label.TextColor = (Color)r["TextPrimary"]!;
                }

                break;

            case ControlStyle.TabActive:
                border.BackgroundColor = (Color)r["AccentSurface"]!;
                border.Stroke = Colors.Transparent;
                if (label != null)
                {
                    label.TextColor = (Color)r["AccentText"]!;
                }

                break;

            case ControlStyle.TabInactive:
                border.BackgroundColor = Colors.Transparent;
                border.Stroke = Colors.Transparent;
                if (label != null)
                {
                    label.TextColor = (Color)r["TextPrimary"]!;
                }

                break;

            case ControlStyle.TabHover:
                border.BackgroundColor = (Color)r["AccentSurface"]!;
                border.Stroke = (Color)r["AccentMuted"]!;
                if (label != null)
                {
                    label.TextColor = (Color)r["AccentText"]!;
                }

                break;

            case ControlStyle.ModeSelected:
                border.BackgroundColor = (Color)r["AccentSurface"]!;
                border.Stroke = (Color)r["AccentPrimary"]!;
                if (label != null)
                {
                    label.TextColor = (Color)r["AccentText"]!;
                }

                break;

            case ControlStyle.ModeUnselected:
                border.BackgroundColor = (Color)r["BackgroundTertiary"]!;
                border.Stroke = (Color)r["SurfaceBorder"]!;
                if (label != null)
                {
                    label.TextColor = (Color)r["TextPrimary"]!;
                }

                break;

            case ControlStyle.ModeHover:
                border.BackgroundColor = (Color)r["AccentSurface"]!;
                border.Stroke = (Color)r["AccentMuted"]!;
                if (label != null)
                {
                    label.TextColor = (Color)r["AccentText"]!;
                }

                break;

            case ControlStyle.PlayButton:
            case ControlStyle.AccentSurface:
                border.BackgroundColor = (Color)r["AccentPrimary"]!;
                border.Stroke = Colors.Transparent;
                if (label != null)
                {
                    label.TextColor = Colors.White;
                }

                break;

            case ControlStyle.PlayButtonHover:
            case ControlStyle.AccentSurfaceHover:
                border.BackgroundColor = (Color)r["AccentMuted"]!;
                border.Stroke = Colors.Transparent;
                if (label != null)
                {
                    label.TextColor = Colors.White;
                }

                break;
        }
    }

    /// <summary>
    /// Registry of all controls that need theme refresh.
    /// Maps control to a function that returns its current correct style.
    /// </summary>
    private readonly List<(Border border, Label? label, Func<ControlStyle> getStyle)> _themedControls = new();

    /// <summary>
    /// Registers a control for automatic theme refresh.
    /// The getStyle function should return the appropriate style based on current app state.
    /// </summary>
    public void RegisterThemedControl(Border border, Func<ControlStyle> getStyle, Label? label = null)
    {
        // Remove any existing registration for this control
        _themedControls.RemoveAll(x => x.border == border);
        _themedControls.Add((border, label, getStyle));
    }

    /// <summary>
    /// Refreshes all registered themed controls with current theme colors.
    /// Call this after theme change.
    /// </summary>
    private void RefreshAllThemedControls()
    {
        foreach (var (border, label, getStyle) in _themedControls)
        {
            try
            {
                var style = getStyle();
                ApplyStyle(border, style, label);
            }
            catch
            {
                // Control may have been disposed, ignore
            }
        }
    }

    /// <summary>
    /// Initializes theme-aware controls. Call this once during page initialization.
    /// </summary>
    private void InitializeThemedControls()
    {
        // Buttons with default/hover style
        RegisterThemedControl(CopyButton, () => ControlStyle.ButtonDefault, CopyButtonLabel);
        RegisterThemedControl(SaveButton, () => ControlStyle.ButtonDefault, null);
        RegisterThemedControl(BrowseAudioFileButton, () => ControlStyle.ButtonDefault, BrowseAudioFileIcon);

        // Accent buttons
        RegisterThemedControl(BrowseButton, () => ControlStyle.ButtonAccent);

        // Transparent buttons
        RegisterThemedControl(ClearButton, () => ControlStyle.ButtonTransparent);
        RegisterThemedControl(ClearHistoryButton, () => ControlStyle.ButtonTransparent);
        RegisterThemedControl(CloseSettingsButton, () => ControlStyle.ButtonTransparent);
        RegisterThemedControl(ResetButton, () => ControlStyle.ButtonTransparent);
        RegisterThemedControl(FontDecreaseButton, () => ControlStyle.ButtonTransparent);
        RegisterThemedControl(FontIncreaseButton, () => ControlStyle.ButtonTransparent);
        RegisterThemedControl(SearchButton, () => SearchBar.IsVisible ? ControlStyle.ModeSelected : ControlStyle.ButtonDefault, SearchButtonLabel);
        RegisterThemedControl(SearchPrevButton, () => ControlStyle.ButtonTransparent);
        RegisterThemedControl(SearchNextButton, () => ControlStyle.ButtonTransparent);
        RegisterThemedControl(SearchCloseButton, () => ControlStyle.ButtonTransparent);

        // Toolbar toggle buttons (state-dependent)
        RegisterThemedControl(DictationToggle, () => _settingsService.EnableDictationFormatting ? ControlStyle.ModeSelected : ControlStyle.ButtonDefault, DictationToggleLabel);
        RegisterThemedControl(DictationHelpToolbarButton, () => ControlStyle.ButtonDefault);

        // Play buttons
        RegisterThemedControl(PlayPauseButton, () => ControlStyle.PlayButton, PlayPauseIcon);
        RegisterThemedControl(HistoryPlayPauseButton, () => ControlStyle.PlayButton, HistoryPlayPauseIcon);

        // Record button (state-dependent)
        RegisterThemedControl(RecordButton, () => _isRecording ? ControlStyle.ButtonDanger : ControlStyle.ButtonTransparent);

        // Tabs (state-dependent)
        RegisterThemedControl(FileTabButton, () => _isFileTabActive ? ControlStyle.TabActive : ControlStyle.TabInactive, FileTabLabel);
        RegisterThemedControl(HistoryTabButton, () => _isFileTabActive ? ControlStyle.TabInactive : ControlStyle.TabActive, HistoryTabLabel);

        // Mode buttons (state-dependent)
        RegisterThemedControl(AccurateButton, () => _useAccurateMode ? ControlStyle.ModeSelected : ControlStyle.ModeUnselected, AccurateButtonLabel);
        RegisterThemedControl(TurboButton, () => _useAccurateMode ? ControlStyle.ModeUnselected : ControlStyle.ModeSelected, TurboButtonLabel);

        // Edit toggle (state-dependent)
        RegisterThemedControl(EditToggle, () => _isEditMode ? ControlStyle.ModeSelected : ControlStyle.ButtonDefault, EditToggleLabel);

        // Speed buttons (state-dependent based on _playbackSpeed)
        RegisterThemedControl(Speed05Button, () => Math.Abs(_playbackSpeed - 0.5) < 0.01 ? ControlStyle.ModeSelected : ControlStyle.ModeUnselected, Speed05Label);
        RegisterThemedControl(Speed1Button, () => Math.Abs(_playbackSpeed - 1.0) < 0.01 ? ControlStyle.ModeSelected : ControlStyle.ModeUnselected, Speed1Label);
        RegisterThemedControl(Speed15Button, () => Math.Abs(_playbackSpeed - 1.5) < 0.01 ? ControlStyle.ModeSelected : ControlStyle.ModeUnselected, Speed15Label);
        RegisterThemedControl(Speed2Button, () => Math.Abs(_playbackSpeed - 2.0) < 0.01 ? ControlStyle.ModeSelected : ControlStyle.ModeUnselected, Speed2Label);

        // Settings directory buttons
        RegisterThemedControl(OpenModelDirButton, () => ControlStyle.ButtonTransparent);
        RegisterThemedControl(BrowseModelDirButton, () => ControlStyle.ButtonTransparent);
        RegisterThemedControl(OpenRecordingsDirButton, () => ControlStyle.ButtonTransparent);
        RegisterThemedControl(BrowseRecordingsDirButton, () => ControlStyle.ButtonTransparent);
        RegisterThemedControl(OpenHistoryDirButton, () => ControlStyle.ButtonTransparent);
        RegisterThemedControl(BrowseHistoryDirButton, () => ControlStyle.ButtonTransparent);
    }

    #endregion
}
