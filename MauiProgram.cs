using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
#if MACCATALYST
using Microsoft.Maui.Platform;
using UIKit;
using CoreGraphics;
#endif

namespace LynxTranscribe
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if MACCATALYST
            ConfigureMacHandlers(builder);
#endif

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }

#if MACCATALYST
        private static void ConfigureMacHandlers(MauiAppBuilder builder)
        {
            Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("MacCatalystEntry", (handler, view) =>
            {
                if (handler.PlatformView is UITextField textField)
                {
                    textField.BackgroundColor = UIColor.Clear;
                    textField.BorderStyle = UITextBorderStyle.None;
                }
            });

            Microsoft.Maui.Handlers.EditorHandler.Mapper.AppendToMapping("MacCatalystEditor", (handler, view) =>
            {
                if (handler.PlatformView is UITextView textView)
                {
                    textView.BackgroundColor = UIColor.Clear;
                }
            });

            Microsoft.Maui.Handlers.SearchBarHandler.Mapper.AppendToMapping("MacCatalystSearchBar", (handler, view) =>
            {
                if (handler.PlatformView is UISearchBar searchBar)
                {
                    searchBar.BackgroundColor = UIColor.Clear;
                    searchBar.BarTintColor = UIColor.Clear;
                    searchBar.SearchBarStyle = UISearchBarStyle.Minimal;
                }
            });

            Microsoft.Maui.Handlers.SliderHandler.Mapper.AppendToMapping("MacCatalystSlider", (handler, view) =>
            {
                if (handler.PlatformView is UISlider slider)
                {
                    slider.Transform = CGAffineTransform.MakeScale(0.8f, 0.8f);
                    
                    var accentColor = UIColor.FromRGB(245, 158, 11);
                    var trackColor = UIColor.FromRGB(63, 63, 70);
                    
                    slider.MinimumTrackTintColor = accentColor;
                    slider.MaximumTrackTintColor = trackColor;
                    slider.ThumbTintColor = UIColor.White;
                }
            });

            Microsoft.Maui.Handlers.SwitchHandler.Mapper.AppendToMapping("MacCatalystSwitch", (handler, view) =>
            {
                if (handler.PlatformView is UISwitch uiSwitch)
                {
                    uiSwitch.Transform = CGAffineTransform.MakeScale(0.7f, 0.7f);
                    uiSwitch.OnTintColor = UIColor.FromRGB(245, 158, 11);
                    uiSwitch.ThumbTintColor = UIColor.White;
                }
            });
        }
#endif
    }
}