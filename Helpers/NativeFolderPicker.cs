#if WINDOWS
using System.Runtime.InteropServices;

namespace LynxTranscribe.Helpers;

/// <summary>
/// Native Windows folder picker using IFileOpenDialog COM interface.
/// Supports setting an initial directory.
/// </summary>
public static class NativeFolderPicker
{
    public static string? ShowDialog(string? initialDirectory = null)
    {
        IFileOpenDialog? dialog = null;

        try
        {
            // Create the dialog
            dialog = (IFileOpenDialog)new FileOpenDialogClass();

            // Configure as folder picker
            dialog.SetOptions(FOS.PICKFOLDERS | FOS.FORCEFILESYSTEM | FOS.NOCHANGEDIR);

            // Set initial directory if provided and exists
            if (!string.IsNullOrEmpty(initialDirectory) && Directory.Exists(initialDirectory))
            {
                IShellItem? folderItem = null;
                var hr = SHCreateItemFromParsingName(initialDirectory, IntPtr.Zero, typeof(IShellItem).GUID, out folderItem);
                if (hr >= 0 && folderItem != null)
                {
                    dialog.SetFolder(folderItem);
                    Marshal.ReleaseComObject(folderItem);
                }
            }

            // Show dialog
            var result = dialog.Show(IntPtr.Zero);
            if (result < 0) // User cancelled or error
            {
                return null;
            }

            // Get result
            IShellItem? item = null;
            dialog.GetResult(out item);
            if (item == null)
            {
                return null;
            }

            string? path = null;
            item.GetDisplayName(SIGDN.FILESYSPATH, out path);
            Marshal.ReleaseComObject(item);

            return path;
        }
        catch
        {
            return null;
        }
        finally
        {
            if (dialog != null)
            {
                Marshal.ReleaseComObject(dialog);
            }
        }
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = true)]
    private static extern int SHCreateItemFromParsingName(
        [MarshalAs(UnmanagedType.LPWStr)] string pszPath,
        IntPtr pbc,
        [In, MarshalAs(UnmanagedType.LPStruct)] Guid riid,
        out IShellItem ppv);

    // COM class for FileOpenDialog
    [ComImport]
    [Guid("DC1C5A9C-E88A-4dde-A5A1-60F82A20AEF7")]
    private class FileOpenDialogClass { }

    [ComImport]
    [Guid("42f85136-db7e-439c-85f1-e4075d135fc8")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IFileOpenDialog
    {
        [PreserveSig] int Show(IntPtr parent);
        void SetFileTypes(uint cFileTypes, IntPtr rgFilterSpec);
        void SetFileTypeIndex(uint iFileType);
        void GetFileTypeIndex(out uint piFileType);
        void Advise(IntPtr pfde, out uint pdwCookie);
        void Unadvise(uint dwCookie);
        void SetOptions(FOS fos);
        void GetOptions(out FOS pfos);
        void SetDefaultFolder(IShellItem psi);
        void SetFolder(IShellItem psi);
        void GetFolder(out IShellItem ppsi);
        void GetCurrentSelection(out IShellItem ppsi);
        void SetFileName([MarshalAs(UnmanagedType.LPWStr)] string pszName);
        void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);
        void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string pszTitle);
        void SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string pszText);
        void SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string pszLabel);
        void GetResult(out IShellItem ppsi);
        void AddPlace(IShellItem psi, int fdap);
        void SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);
        void Close(int hr);
        void SetClientGuid([In, MarshalAs(UnmanagedType.LPStruct)] Guid guid);
        void ClearClientData();
        void SetFilter(IntPtr pFilter);
        void GetResults(out IntPtr ppenum);
        void GetSelectedItems(out IntPtr ppsai);
    }

    [ComImport]
    [Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellItem
    {
        void BindToHandler(IntPtr pbc, [In, MarshalAs(UnmanagedType.LPStruct)] Guid bhid, [In, MarshalAs(UnmanagedType.LPStruct)] Guid riid, out IntPtr ppv);
        void GetParent(out IShellItem ppsi);
        void GetDisplayName(SIGDN sigdnName, [MarshalAs(UnmanagedType.LPWStr)] out string ppszName);
        void GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);
        void Compare(IShellItem psi, uint hint, out int piOrder);
    }

    [Flags]
    private enum FOS : uint
    {
        OVERWRITEPROMPT = 0x00000002,
        STRICTFILETYPES = 0x00000004,
        NOCHANGEDIR = 0x00000008,
        PICKFOLDERS = 0x00000020,
        FORCEFILESYSTEM = 0x00000040,
        ALLNONSTORAGEITEMS = 0x00000080,
        NOVALIDATE = 0x00000100,
        ALLOWMULTISELECT = 0x00000200,
        PATHMUSTEXIST = 0x00000800,
        FILEMUSTEXIST = 0x00001000,
        CREATEPROMPT = 0x00002000,
        SHAREAWARE = 0x00004000,
        NOREADONLYRETURN = 0x00008000,
        NOTESTFILECREATE = 0x00010000,
        HIDEMRUPLACES = 0x00020000,
        HIDEPINNEDPLACES = 0x00040000,
        NODEREFERENCELINKS = 0x00100000,
        DONTADDTORECENT = 0x02000000,
        FORCESHOWHIDDEN = 0x10000000,
        DEFAULTNOMINIMODE = 0x20000000,
        FORCEPREVIEWPANEON = 0x40000000
    }

    private enum SIGDN : uint
    {
        NORMALDISPLAY = 0x00000000,
        PARENTRELATIVEPARSING = 0x80018001,
        DESKTOPABSOLUTEPARSING = 0x80028000,
        PARENTRELATIVEEDITING = 0x80031001,
        DESKTOPABSOLUTEEDITING = 0x8004c000,
        FILESYSPATH = 0x80058000,
        URL = 0x80068000,
        PARENTRELATIVEFORADDRESSBAR = 0x8007c001,
        PARENTRELATIVE = 0x80080001
    }
}
#endif
