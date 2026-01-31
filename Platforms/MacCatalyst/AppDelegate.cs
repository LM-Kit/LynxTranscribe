using Foundation;
using UIKit;

namespace LynxTranscribe;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
    {
        var result = base.FinishedLaunching(application, launchOptions);

#if MACCATALYST
        NSNotificationCenter.DefaultCenter.AddObserver(
            UIScene.DidActivateNotification,
            notification =>
            {
                if (notification.Object is UIWindowScene windowScene)
                {
                    ConfigureWindowScene(windowScene);
                }
            });

        ConfigureAllWindowScenes();
#endif

        return result;
    }

#if MACCATALYST
    private void ConfigureAllWindowScenes()
    {
        try
        {
            foreach (var scene in UIApplication.SharedApplication.ConnectedScenes)
            {
                if (scene is UIWindowScene windowScene)
                {
                    ConfigureWindowScene(windowScene);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ConfigureAllWindowScenes error: {ex.Message}");
        }
    }

    private void ConfigureWindowScene(UIWindowScene windowScene)
    {
        try
        {
            if (windowScene.Titlebar != null)
            {
                windowScene.Titlebar.TitleVisibility = UITitlebarTitleVisibility.Hidden;
                windowScene.Titlebar.Toolbar = null;
                windowScene.Titlebar.SeparatorStyle = UITitlebarSeparatorStyle.None;
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
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ConfigureWindowScene error: {ex.Message}");
        }
    }
#endif
}
