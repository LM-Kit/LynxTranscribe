using LynxTranscribe.Helpers;
using LynxTranscribe.Localization;

#if !WINDOWS
using CommunityToolkit.Maui.Storage;
#endif
using L = LynxTranscribe.Localization.LocalizationService;

namespace LynxTranscribe;

/// <summary>
/// Settings: Storage path configuration (model, history, recordings directories)
/// </summary>
public partial class MainPage
{
    #region Storage Path Settings

    private async void OnBrowseModelDirClicked(object? sender, TappedEventArgs e)
    {
        try
        {
            var currentDir = _settingsService.ModelStorageDirectory;
            if (!Directory.Exists(currentDir))
            {
                Directory.CreateDirectory(currentDir);
            }

            var selectedPath = await PickFolderAsync(currentDir);
            if (!string.IsNullOrEmpty(selectedPath))
            {
                _settingsService.ModelStorageDirectory = selectedPath;
                LMKit.Global.Configuration.ModelStorageDirectory = selectedPath;
                ModelDirLabel.Text = selectedPath;
                ShowToast(L.Localize(StringKeys.ModelDirectoryUpdated), ToastType.Success);
            }
        }
        catch (Exception ex)
        {
            ShowToast(L.Localize(StringKeys.Error, ex.Message), ToastType.Error);
        }
    }

    private async void OnBrowseRecordingsDirClicked(object? sender, TappedEventArgs e)
    {
        try
        {
            var currentDir = _settingsService.RecordingsDirectory;
            if (!Directory.Exists(currentDir))
            {
                Directory.CreateDirectory(currentDir);
            }

            var selectedPath = await PickFolderAsync(currentDir);
            if (!string.IsNullOrEmpty(selectedPath))
            {
                _settingsService.RecordingsDirectory = selectedPath;
                _audioRecorder.SetRecordingsDirectory(selectedPath);
                RecordingsDirLabel.Text = selectedPath;
                ShowToast(L.Localize(StringKeys.RecordingsDirectoryUpdated), ToastType.Success);
            }
        }
        catch (Exception ex)
        {
            ShowToast(L.Localize(StringKeys.Error, ex.Message), ToastType.Error);
        }
    }

    private async void OnBrowseHistoryDirClicked(object? sender, TappedEventArgs e)
    {
        try
        {
            var currentDir = _settingsService.HistoryDirectory;
            if (!Directory.Exists(currentDir))
            {
                Directory.CreateDirectory(currentDir);
            }

            var selectedPath = await PickFolderAsync(currentDir);
            if (!string.IsNullOrEmpty(selectedPath))
            {
                _settingsService.HistoryDirectory = selectedPath;
                _historyService.SetHistoryDirectory(selectedPath);
                HistoryDirLabel.Text = selectedPath;
                RefreshHistoryList();
                ShowToast(L.Localize(StringKeys.HistoryDirectoryUpdated), ToastType.Success);
            }
        }
        catch (Exception ex)
        {
            ShowToast(L.Localize(StringKeys.Error, ex.Message), ToastType.Error);
        }
    }

    private Task<string?> PickFolderAsync(string initialDirectory)
    {
#if WINDOWS
        var tcs = new TaskCompletionSource<string?>();

        var thread = new Thread(() =>
        {
            try
            {
                var result = NativeFolderPicker.ShowDialog(initialDirectory);
                tcs.SetResult(result);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        return tcs.Task;
#else
        try
        {
            var result = await FolderPicker.Default.PickAsync(CancellationToken.None);
            if (result.IsSuccessful && result.Folder != null)
            {
                return result.Folder.Path;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"FolderPicker error: {ex.Message}");
        }
        return null;
#endif
    }

    private void OnOpenModelDirClicked(object? sender, TappedEventArgs e)
    {
        OpenFolder(_settingsService.ModelStorageDirectory);
    }

    private void OnOpenRecordingsDirClicked(object? sender, TappedEventArgs e)
    {
        OpenFolder(_settingsService.RecordingsDirectory);
    }

    private void OnOpenHistoryDirClicked(object? sender, TappedEventArgs e)
    {
        OpenFolder(_settingsService.HistoryDirectory);
    }

    // Hover effects for directory buttons
    private void OnDirButtonHoverEnter(object? sender, PointerEventArgs e)
    {
        if (sender is Border border)
        {
            var r = Resources;
            border.BackgroundColor = (Color)r["BackgroundTertiary"]!;
        }
    }

    private void OnDirButtonHoverExit(object? sender, PointerEventArgs e)
    {
        if (sender is Border border)
        {
            border.BackgroundColor = Colors.Transparent;
        }
    }

    private void OpenFolder(string path)
    {
        try
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
#if WINDOWS
            System.Diagnostics.Process.Start("explorer.exe", path);
#elif MACCATALYST
            System.Diagnostics.Process.Start("open", path);
#endif
        }
        catch (Exception ex)
        {
            ShowToast(L.Localize(StringKeys.CouldNotOpenFolder, ex.Message), ToastType.Error);
        }
    }

    #endregion
}
