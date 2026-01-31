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
        ConfigureMacWindow();
#endif

        return result;
    }

#if MACCATALYST
    private void ConfigureMacWindow()
    {
        try
        {
            var scenes = UIApplication.SharedApplication.ConnectedScenes;
            foreach (var scene in scenes)
            {
                if (scene is UIWindowScene windowScene)
                {
                    ConfigureWindowScene(windowScene);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ConfigureMacWindow error: {ex.Message}");
        }
    }

    private void ConfigureWindowScene(UIWindowScene windowScene)
    {
        try
        {
            if (windowScene.Titlebar != null)
            {
                windowScene.Titlebar.TitleVisibility = UITitlebarTitleVisibility.Hidden;
            }

            if (windowScene.SizeRestrictions != null)
            {
                windowScene.SizeRestrictions.MinimumSize = new CoreGraphics.CGSize(1024, 700);
                windowScene.SizeRestrictions.MaximumSize = new CoreGraphics.CGSize(3840, 2160);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ConfigureWindowScene error: {ex.Message}");
        }
    }
#endif
}
