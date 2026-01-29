using LMKit.Speech.Dictation;
using LynxTranscribe.Helpers;
using LynxTranscribe.Localization;
using LynxTranscribe.Models;
using System.Diagnostics;
using L = LynxTranscribe.Localization.LocalizationService;

namespace LynxTranscribe;

/// <summary>
/// Export actions: Copy, save to various formats (TXT, DOCX, RTF, SRT, VTT)
/// </summary>
public partial class MainPage
{
    #region Export Actions

    private string GetCurrentTranscriptText()
    {
        string text;

        if (!string.IsNullOrEmpty(_currentRecordId))
        {
            var record = _historyService.GetById(_currentRecordId);
            text = record?.TranscriptText ?? TranscriptionRecord.FormatAsPlainText(_currentSegments);
        }
        else
        {
            text = TranscriptionRecord.FormatAsPlainText(_currentSegments);
        }

        // Apply dictation formatting if enabled
        if (_settingsService.EnableDictationFormatting)
        {
            text = Formatter.Format(text);
        }

        return text;
    }

    private async Task CopyToClipboard()
    {
        var text = GetCurrentTranscriptText();
        if (!string.IsNullOrEmpty(text))
        {
            await Clipboard.Default.SetTextAsync(text);

            CopyButtonLabel.Text = L.Localize(StringKeys.Copied);
            CopyButton.BackgroundColor = (Color)Resources["SuccessSurface"]!;
            CopyButton.Stroke = (Color)Resources["SuccessColor"]!;

            await Task.Delay(2000);

            CopyButtonLabel.Text = L.Localize(StringKeys.Copy);
            CopyButton.BackgroundColor = (Color)Resources["BackgroundTertiary"]!;
            CopyButton.Stroke = (Color)Resources["SurfaceBorder"]!;
        }
    }

    private async void OnCopyClicked(object? sender, TappedEventArgs e)
    {
        await CopyToClipboard();
    }

    private void OnCopyButtonHoverEnter(object? sender, PointerEventArgs e)
    {
        ApplyStyle(CopyButton, ControlStyle.ButtonHover, CopyButtonLabel);
    }

    private void OnCopyButtonHoverExit(object? sender, PointerEventArgs e)
    {
        ApplyStyle(CopyButton, ControlStyle.ButtonDefault, CopyButtonLabel);
    }

    private void OnSaveButtonHoverEnter(object? sender, PointerEventArgs e)
    {
        ApplyStyle(SaveButton, ControlStyle.ButtonHover, null);
    }

    private void OnSaveButtonHoverExit(object? sender, PointerEventArgs e)
    {
        ApplyStyle(SaveButton, ControlStyle.ButtonDefault, null);
    }

    private void OnSaveClicked(object? sender, TappedEventArgs e)
    {
        if (string.IsNullOrEmpty(GetCurrentTranscriptText()))
        {
            return;
        }

        ShowExportPanel();
    }

    #endregion

    #region Export Panel

    private void ShowExportPanel()
    {
        // Show/hide subtitle options based on whether we have segments
        var hasSegments = _currentSegments.Count > 0;
        ExportOptionSrt.IsVisible = hasSegments;
        ExportOptionVtt.IsVisible = hasSegments;

        // Sync checkboxes with current settings
        UpdateExportAutoOpenUI(_settingsService.OpenFilesAfterExport);
        UpdateExportDictationUI(_settingsService.EnableDictationFormatting);

        ExportPanelOverlay.IsVisible = true;
    }

    private void HideExportPanel()
    {
        ExportPanelOverlay.IsVisible = false;
    }

    private void OnExportPanelCancel(object? sender, TappedEventArgs e)
    {
        HideExportPanel();
    }

    private void UpdateExportAutoOpenUI(bool isChecked)
    {
        if (isChecked)
        {
            ExportAutoOpenBox.BackgroundColor = (Color)Resources["AccentPrimary"]!;
            ExportAutoOpenKnob.HorizontalOptions = LayoutOptions.End;
        }
        else
        {
            ExportAutoOpenBox.BackgroundColor = (Color)Resources["SurfaceBorder"]!;
            ExportAutoOpenKnob.HorizontalOptions = LayoutOptions.Start;
        }
    }

    private void OnExportAutoOpenToggle(object? sender, TappedEventArgs e)
    {
        var newValue = !_settingsService.OpenFilesAfterExport;
        _settingsService.OpenFilesAfterExport = newValue;
        UpdateExportAutoOpenUI(newValue);
        // Also sync the settings panel switch
        OpenAfterExportSwitch.IsToggled = newValue;
    }

    private void OnExportAutoOpenHoverEnter(object? sender, PointerEventArgs e)
    {
        ExportAutoOpenRow.BackgroundColor = (Color)Resources["BackgroundTertiary"]!;
    }

    private void OnExportAutoOpenHoverExit(object? sender, PointerEventArgs e)
    {
        ExportAutoOpenRow.BackgroundColor = Colors.Transparent;
    }

    private void UpdateExportDictationUI(bool isChecked)
    {
        if (isChecked)
        {
            ExportDictationBox.BackgroundColor = (Color)Resources["AccentPrimary"]!;
            ExportDictationKnob.HorizontalOptions = LayoutOptions.End;
        }
        else
        {
            ExportDictationBox.BackgroundColor = (Color)Resources["SurfaceBorder"]!;
            ExportDictationKnob.HorizontalOptions = LayoutOptions.Start;
        }
    }

    private void OnExportDictationToggle(object? sender, TappedEventArgs e)
    {
        var newValue = !_settingsService.EnableDictationFormatting;
        _settingsService.EnableDictationFormatting = newValue;
        UpdateExportDictationUI(newValue);
        // Also sync the settings panel switch
        DictationFormattingSwitch.IsToggled = newValue;

        // Update toggle indicator styling
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

    private void OnExportDictationHoverEnter(object? sender, PointerEventArgs e)
    {
        ExportDictationRow.BackgroundColor = (Color)Resources["BackgroundTertiary"]!;
    }

    private void OnExportDictationHoverExit(object? sender, PointerEventArgs e)
    {
        ExportDictationRow.BackgroundColor = Colors.Transparent;
    }

    private void OnExportCloseHoverEnter(object? sender, PointerEventArgs e)
    {
        ApplyStyle(ExportPanelCloseButton, ControlStyle.ButtonTransparentHover);
    }

    private void OnExportCloseHoverExit(object? sender, PointerEventArgs e)
    {
        ApplyStyle(ExportPanelCloseButton, ControlStyle.ButtonTransparent);
    }

    private void OnExportOptionHoverEnter(object? sender, PointerEventArgs e)
    {
        if (sender is Border border)
        {
            border.BackgroundColor = (Color)Resources["SurfaceColor"]!;
            border.Stroke = (Color)Resources["AccentMuted"]!;
        }
    }

    private void OnExportOptionHoverExit(object? sender, PointerEventArgs e)
    {
        if (sender is Border border)
        {
            border.BackgroundColor = (Color)Resources["BackgroundTertiary"]!;
            border.Stroke = (Color)Resources["SurfaceBorder"]!;
        }
    }

    /// <summary>
    /// Gets the base filename for export, using the original audio file name or history record display name.
    /// </summary>
    private string GetExportBaseFileName()
    {
        // First try to get from selected file path (current transcription)
        if (!string.IsNullOrEmpty(_selectedFilePath))
        {
            var fileName = System.IO.Path.GetFileNameWithoutExtension(_selectedFilePath);
            if (!string.IsNullOrEmpty(fileName))
            {
                return fileName + "_transcript";
            }
        }

        // Then try from history record
        if (!string.IsNullOrEmpty(_currentRecordId))
        {
            var record = _historyService.GetById(_currentRecordId);
            if (record != null)
            {
                // Use display name (without extension if present)
                var displayName = record.DisplayName;
                if (!string.IsNullOrEmpty(displayName))
                {
                    // Remove media extensions if present in display name
                    foreach (var ext in AppConstants.SupportedMediaExtensions)
                    {
                        if (displayName.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                        {
                            displayName = displayName.Substring(0, displayName.Length - ext.Length);
                            break;
                        }
                    }
                    return displayName + "_transcript";
                }
            }
        }

        // Fallback with timestamp
        return $"transcript_{DateTime.Now:yyyyMMdd_HHmmss}";
    }

    private async void OnExportTxtClicked(object? sender, TappedEventArgs e)
    {
        HideExportPanel();
        try
        {
            var baseFileName = GetExportBaseFileName();
            await ExportAsTxt(baseFileName);
        }
        catch (Exception ex)
        {
            ShowError("Export Failed", ex.Message);
        }
    }

    private async void OnExportDocxClicked(object? sender, TappedEventArgs e)
    {
        HideExportPanel();
        try
        {
            var baseFileName = GetExportBaseFileName();
            await ExportAsDocx(baseFileName);
        }
        catch (Exception ex)
        {
            ShowError("Export Failed", ex.Message);
        }
    }

    private async void OnExportRtfClicked(object? sender, TappedEventArgs e)
    {
        HideExportPanel();
        try
        {
            var baseFileName = GetExportBaseFileName();
            await ExportAsRtf(baseFileName);
        }
        catch (Exception ex)
        {
            ShowError("Export Failed", ex.Message);
        }
    }

    private async void OnExportSrtClicked(object? sender, TappedEventArgs e)
    {
        HideExportPanel();
        try
        {
            var baseFileName = GetExportBaseFileName();
            await ExportAsSrt(baseFileName);
        }
        catch (Exception ex)
        {
            ShowError("Export Failed", ex.Message);
        }
    }

    private async void OnExportVttClicked(object? sender, TappedEventArgs e)
    {
        HideExportPanel();
        try
        {
            var baseFileName = GetExportBaseFileName();
            await ExportAsVtt(baseFileName);
        }
        catch (Exception ex)
        {
            ShowError("Export Failed", ex.Message);
        }
    }

    #endregion

    #region Export Methods

    private async Task ExportAsTxt(string baseFileName)
    {
        var fileName = baseFileName + ".txt";
        var content = GetCurrentTranscriptText();

#if WINDOWS
        var savePicker = new Windows.Storage.Pickers.FileSavePicker
        {
            SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary,
            SuggestedFileName = fileName
        };
        savePicker.FileTypeChoices.Add("Text File", new List<string> { ".txt" });

        var windowHandle = ((MauiWinUIWindow)Application.Current!.Windows[0].Handler.PlatformView!).WindowHandle;
        WinRT.Interop.InitializeWithWindow.Initialize(savePicker, windowHandle);

        var file = await savePicker.PickSaveFileAsync();
        if (file != null)
        {
            await Windows.Storage.FileIO.WriteTextAsync(file, content);
            ShowToast(L.Localize(StringKeys.SavedTo, file.Name), ToastType.Success);
            OpenFileIfEnabled(file.Path);
        }
#else
        var filePath = TranscriptExporter.GetUniqueFilePath(fileName);
        await File.WriteAllTextAsync(filePath, content);
        ShowToast(L.Localize(StringKeys.SavedTo, System.IO.Path.GetFileName(filePath)), ToastType.Success);
        OpenFileIfEnabled(filePath);
#endif
    }

    private async Task ExportAsDocx(string baseFileName)
    {
        var fileName = baseFileName + ".docx";
        var content = GetCurrentTranscriptText();
        var sourceFileName = System.IO.Path.GetFileName(_selectedFilePath);

#if WINDOWS
        var savePicker = new Windows.Storage.Pickers.FileSavePicker
        {
            SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary,
            SuggestedFileName = fileName
        };
        savePicker.FileTypeChoices.Add("Word Document", new List<string> { ".docx" });

        var windowHandle = ((MauiWinUIWindow)Application.Current!.Windows[0].Handler.PlatformView!).WindowHandle;
        WinRT.Interop.InitializeWithWindow.Initialize(savePicker, windowHandle);

        var file = await savePicker.PickSaveFileAsync();
        if (file != null)
        {
            var tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{Guid.NewGuid()}.docx");
            TranscriptExporter.ExportToDocx(tempPath, content, sourceFileName);

            var bytes = await File.ReadAllBytesAsync(tempPath);
            await Windows.Storage.FileIO.WriteBytesAsync(file, bytes);
            File.Delete(tempPath);

            ShowToast(L.Localize(StringKeys.SavedTo, file.Name), ToastType.Success);
            OpenFileIfEnabled(file.Path);
        }
#else
        var filePath = TranscriptExporter.GetUniqueFilePath(fileName);
        TranscriptExporter.ExportToDocx(filePath, content, sourceFileName);
        ShowToast(L.Localize(StringKeys.SavedTo, System.IO.Path.GetFileName(filePath)), ToastType.Success);
        OpenFileIfEnabled(filePath);
#endif
    }

    private async Task ExportAsRtf(string baseFileName)
    {
        var fileName = baseFileName + ".rtf";
        var sourceFileName = System.IO.Path.GetFileName(_selectedFilePath);
        var rtfContent = TranscriptExporter.CreateRtfContent(GetCurrentTranscriptText(), sourceFileName);

#if WINDOWS
        var savePicker = new Windows.Storage.Pickers.FileSavePicker
        {
            SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary,
            SuggestedFileName = fileName
        };
        savePicker.FileTypeChoices.Add("Rich Text Format", new List<string> { ".rtf" });

        var windowHandle = ((MauiWinUIWindow)Application.Current!.Windows[0].Handler.PlatformView!).WindowHandle;
        WinRT.Interop.InitializeWithWindow.Initialize(savePicker, windowHandle);

        var file = await savePicker.PickSaveFileAsync();
        if (file != null)
        {
            await Windows.Storage.FileIO.WriteTextAsync(file, rtfContent);
            ShowToast(L.Localize(StringKeys.SavedTo, file.Name), ToastType.Success);
            OpenFileIfEnabled(file.Path);
        }
#else
        var filePath = TranscriptExporter.GetUniqueFilePath(fileName);
        await File.WriteAllTextAsync(filePath, rtfContent);
        ShowToast(L.Localize(StringKeys.SavedTo, System.IO.Path.GetFileName(filePath)), ToastType.Success);
        OpenFileIfEnabled(filePath);
#endif
    }

    private async Task ExportAsSrt(string baseFileName)
    {
        var segments = _currentSegments;
        if (segments.Count == 0 && !string.IsNullOrEmpty(_currentRecordId))
        {
            var record = _historyService.GetById(_currentRecordId);
            if (record?.Segments != null && record.Segments.Count > 0)
            {
                segments = record.Segments;
                _currentSegments = segments;
            }
        }

        if (segments.Count == 0)
        {
            ShowError("Export Failed", "No timestamp data available for SRT export.");
            return;
        }

        var fileName = baseFileName + ".srt";
        var srtContent = TranscriptExporter.CreateSrtContent(segments);

#if WINDOWS
        var savePicker = new Windows.Storage.Pickers.FileSavePicker
        {
            SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary,
            SuggestedFileName = fileName
        };
        savePicker.FileTypeChoices.Add("SubRip Subtitle", new List<string> { ".srt" });

        var windowHandle = ((MauiWinUIWindow)Application.Current!.Windows[0].Handler.PlatformView!).WindowHandle;
        WinRT.Interop.InitializeWithWindow.Initialize(savePicker, windowHandle);

        var file = await savePicker.PickSaveFileAsync();
        if (file != null)
        {
            await Windows.Storage.FileIO.WriteTextAsync(file, srtContent);
            ShowToast(L.Localize(StringKeys.SavedTo, file.Name), ToastType.Success);
            OpenFileIfEnabled(file.Path);
        }
#else
        var filePath = TranscriptExporter.GetUniqueFilePath(fileName);
        await File.WriteAllTextAsync(filePath, srtContent);
        ShowToast(L.Localize(StringKeys.SavedTo, System.IO.Path.GetFileName(filePath)), ToastType.Success);
        OpenFileIfEnabled(filePath);
#endif
    }

    private async Task ExportAsVtt(string baseFileName)
    {
        var segments = _currentSegments;
        if (segments.Count == 0 && !string.IsNullOrEmpty(_currentRecordId))
        {
            var record = _historyService.GetById(_currentRecordId);
            if (record?.Segments != null && record.Segments.Count > 0)
            {
                segments = record.Segments;
                _currentSegments = segments;
            }
        }

        if (segments.Count == 0)
        {
            ShowError("Export Failed", "No timestamp data available for VTT export.");
            return;
        }

        var fileName = baseFileName + ".vtt";
        var vttContent = TranscriptExporter.CreateVttContent(segments);

#if WINDOWS
        var savePicker = new Windows.Storage.Pickers.FileSavePicker
        {
            SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary,
            SuggestedFileName = fileName
        };
        savePicker.FileTypeChoices.Add("WebVTT Subtitle", new List<string> { ".vtt" });

        var windowHandle = ((MauiWinUIWindow)Application.Current!.Windows[0].Handler.PlatformView!).WindowHandle;
        WinRT.Interop.InitializeWithWindow.Initialize(savePicker, windowHandle);

        var file = await savePicker.PickSaveFileAsync();
        if (file != null)
        {
            await Windows.Storage.FileIO.WriteTextAsync(file, vttContent);
            ShowToast(L.Localize(StringKeys.SavedTo, file.Name), ToastType.Success);
            OpenFileIfEnabled(file.Path);
        }
#else
        var filePath = TranscriptExporter.GetUniqueFilePath(fileName);
        await File.WriteAllTextAsync(filePath, vttContent);
        ShowToast(L.Localize(StringKeys.SavedTo, System.IO.Path.GetFileName(filePath)), ToastType.Success);
        OpenFileIfEnabled(filePath);
#endif
    }

    /// <summary>
    /// Opens the file with the default application if the setting is enabled.
    /// </summary>
    private async void OpenFileIfEnabled(string filePath)
    {
        try
        {
            if (!_settingsService.OpenFilesAfterExport)
            {
                return;
            }

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                return;
            }

#if WINDOWS
            Process.Start(new ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true
            });
#else
            await Launcher.OpenAsync(new OpenFileRequest
            {
                File = new ReadOnlyFile(filePath)
            });
#endif
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to open file: {ex.Message}");
        }
    }

    #endregion
}
