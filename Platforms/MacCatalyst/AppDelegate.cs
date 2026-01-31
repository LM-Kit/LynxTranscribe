using Foundation;
using UIKit;
using ObjCRuntime;

namespace LynxTranscribe;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
    {
        var result = base.FinishedLaunching(application, launchOptions);

#if MACCATALYST
        ConfigureForMac();
#endif

        return result;
    }

#if MACCATALYST
    private void ConfigureForMac()
    {
        NSNotificationCenter.DefaultCenter.AddObserver(
            UIScene.DidActivateNotification,
            notification =>
            {
                if (notification.Object is UIWindowScene windowScene)
                {
                    ConfigureWindowScene(windowScene);
                }
            });

        foreach (var scene in UIApplication.SharedApplication.ConnectedScenes)
        {
            if (scene is UIWindowScene windowScene)
            {
                ConfigureWindowScene(windowScene);
            }
        }
    }

    private void ConfigureWindowScene(UIWindowScene windowScene)
    {
        try
        {
            var titlebar = windowScene.Titlebar;
            if (titlebar != null)
            {
                titlebar.TitleVisibility = UITitlebarTitleVisibility.Hidden;
                titlebar.Toolbar = null;
                titlebar.SeparatorStyle = UITitlebarSeparatorStyle.None;
            }

            if (windowScene.SizeRestrictions != null)
            {
                windowScene.SizeRestrictions.MinimumSize = new CoreGraphics.CGSize(1024, 700);
                windowScene.SizeRestrictions.MaximumSize = new CoreGraphics.CGSize(3840, 2160);
            }

            var darkColor = UIColor.FromRGB(10, 10, 11);
            foreach (var window in windowScene.Windows)
            {
                window.BackgroundColor = darkColor;
                window.OverrideUserInterfaceStyle = UIUserInterfaceStyle.Dark;

                if (window.RootViewController?.View != null)
                {
                    window.RootViewController.View.BackgroundColor = darkColor;
                }

                ConfigureNSWindow();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ConfigureWindowScene error: {ex.Message}");
        }
    }

    private void ConfigureNSWindow()
    {
        try
        {
            var nsAppClass = new Class("NSApplication");
            var sharedAppSel = new Selector("sharedApplication");
            var nsApp = Messaging.IntPtr_objc_msgSend(nsAppClass.Handle, sharedAppSel.Handle);
            
            if (nsApp == IntPtr.Zero) return;

            var windowsSel = new Selector("windows");
            var windowsPtr = Messaging.IntPtr_objc_msgSend(nsApp, windowsSel.Handle);
            var windows = Runtime.GetNSObject<NSArray>(windowsPtr);
            
            if (windows == null || windows.Count == 0) return;

            var nsWindow = windows.GetItem<NSObject>(0);
            if (nsWindow == null) return;

            var handle = nsWindow.Handle;

            // Make titlebar fully transparent
            var setTitlebarAppearsTransparent = new Selector("setTitlebarAppearsTransparent:");
            Messaging.void_objc_msgSend_bool(handle, setTitlebarAppearsTransparent.Handle, true);

            // Hide window title (NSWindowTitleHidden = 1)
            var setTitleVisibility = new Selector("setTitleVisibility:");
            Messaging.void_objc_msgSend_nint(handle, setTitleVisibility.Handle, 1);

            // Add full size content view style
            var styleMaskSel = new Selector("styleMask");
            var currentMask = (nuint)Messaging.nuint_objc_msgSend(handle, styleMaskSel.Handle);
            
            // NSWindowStyleMaskFullSizeContentView = 1 << 15 = 32768
            var newMask = currentMask | 32768;
            var setStyleMaskSel = new Selector("setStyleMask:");
            Messaging.void_objc_msgSend_nuint(handle, setStyleMaskSel.Handle, newMask);

            // Set window background color
            var nsColorClass = new Class("NSColor");
            var colorSel = new Selector("colorWithRed:green:blue:alpha:");
            var darkColor = Messaging.IntPtr_objc_msgSend_nfloat_nfloat_nfloat_nfloat(
                nsColorClass.Handle, colorSel.Handle,
                10.0 / 255.0, 10.0 / 255.0, 11.0 / 255.0, 1.0);

            var setBgColorSel = new Selector("setBackgroundColor:");
            Messaging.void_objc_msgSend_IntPtr(handle, setBgColorSel.Handle, darkColor);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ConfigureNSWindow error: {ex.Message}");
        }
    }
#endif
}

#if MACCATALYST
internal static class Messaging
{
    private const string LIBOBJC = "/usr/lib/libobjc.dylib";

    [System.Runtime.InteropServices.DllImport(LIBOBJC, EntryPoint = "objc_msgSend")]
    public static extern IntPtr IntPtr_objc_msgSend(IntPtr receiver, IntPtr selector);

    [System.Runtime.InteropServices.DllImport(LIBOBJC, EntryPoint = "objc_msgSend")]
    public static extern void void_objc_msgSend_bool(IntPtr receiver, IntPtr selector, bool arg1);

    [System.Runtime.InteropServices.DllImport(LIBOBJC, EntryPoint = "objc_msgSend")]
    public static extern void void_objc_msgSend_IntPtr(IntPtr receiver, IntPtr selector, IntPtr arg1);

    [System.Runtime.InteropServices.DllImport(LIBOBJC, EntryPoint = "objc_msgSend")]
    public static extern void void_objc_msgSend_nint(IntPtr receiver, IntPtr selector, nint arg1);

    [System.Runtime.InteropServices.DllImport(LIBOBJC, EntryPoint = "objc_msgSend")]
    public static extern nuint nuint_objc_msgSend(IntPtr receiver, IntPtr selector);

    [System.Runtime.InteropServices.DllImport(LIBOBJC, EntryPoint = "objc_msgSend")]
    public static extern void void_objc_msgSend_nuint(IntPtr receiver, IntPtr selector, nuint arg1);

    [System.Runtime.InteropServices.DllImport(LIBOBJC, EntryPoint = "objc_msgSend")]
    public static extern IntPtr IntPtr_objc_msgSend_nfloat_nfloat_nfloat_nfloat(
        IntPtr receiver, IntPtr selector, double arg1, double arg2, double arg3, double arg4);
}
#endif
