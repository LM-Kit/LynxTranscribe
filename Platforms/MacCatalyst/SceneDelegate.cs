using Foundation;
using UIKit;

namespace LynxTranscribe;

[Register("SceneDelegate")]
public class SceneDelegate : MauiUISceneDelegate
{
    public override void WillConnect(UIScene scene, UISceneSession session, UISceneConnectionOptions connectionOptions)
    {
        base.WillConnect(scene, session, connectionOptions);

        if (scene is UIWindowScene windowScene)
        {
            ConfigureWindowScene(windowScene);
        }
    }

    private void ConfigureWindowScene(UIWindowScene windowScene)
    {
#if MACCATALYST
        if (windowScene.Titlebar != null)
        {
            windowScene.Titlebar.TitleVisibility = UITitlebarTitleVisibility.Hidden;
            windowScene.Titlebar.Toolbar = null;
            windowScene.Titlebar.ToolbarStyle = UITitlebarToolbarStyle.Unified;
        }

        var geometryPreferences = new UIWindowSceneGeometryPreferencesMac
        {
            SystemFrame = new CoreGraphics.CGRect(100, 100, 1400, 900)
        };

        windowScene.RequestGeometryUpdate(geometryPreferences, error =>
        {
            if (error != null)
            {
                System.Diagnostics.Debug.WriteLine($"Window geometry update error: {error}");
            }
        });

        windowScene.SizeRestrictions.MinimumSize = new CoreGraphics.CGSize(1024, 700);
        windowScene.SizeRestrictions.MaximumSize = new CoreGraphics.CGSize(2560, 1600);
#endif
    }
}
