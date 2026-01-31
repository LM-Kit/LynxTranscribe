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
                    var accentColor = UIColor.FromRGB(245, 158, 11);
                    var trackColor = UIColor.FromRGB(63, 63, 70);
                    
                    slider.MinimumTrackTintColor = accentColor;
                    slider.MaximumTrackTintColor = trackColor;
                    
                    var thumbSize = new CGSize(14, 14);
                    UIGraphics.BeginImageContextWithOptions(thumbSize, false, 0);
                    var context = UIGraphics.GetCurrentContext();
                    if (context != null)
                    {
                        context.SetFillColor(UIColor.White.CGColor);
                        context.SetShadow(new CGSize(0, 1), 2, UIColor.FromRGBA(0, 0, 0, 80).CGColor);
                        var rect = new CGRect(1, 1, 12, 12);
                        context.FillEllipseInRect(rect);
                    }
                    var thumbImage = UIGraphics.GetImageFromCurrentImageContext();
                    UIGraphics.EndImageContext();
                    
                    if (thumbImage != null)
                    {
                        slider.SetThumbImage(thumbImage, UIControlState.Normal);
                        slider.SetThumbImage(thumbImage, UIControlState.Highlighted);
                    }
                }
            });

            Microsoft.Maui.Handlers.SwitchHandler.Mapper.AppendToMapping("MacCatalystSwitch", (handler, view) =>
            {
                if (handler.PlatformView is UISwitch uiSwitch)
                {
                    uiSwitch.Transform = CGAffineTransform.MakeScale(0.6f, 0.6f);
                    uiSwitch.OnTintColor = UIColor.FromRGB(245, 158, 11);
                    uiSwitch.ThumbTintColor = UIColor.White;
                    uiSwitch.BackgroundColor = UIColor.Clear;
                }
            });

            Microsoft.Maui.Handlers.PickerHandler.Mapper.AppendToMapping("MacCatalystPicker", (handler, view) =>
            {
                if (handler.PlatformView is UITextField textField)
                {
                    textField.BackgroundColor = UIColor.FromRGB(24, 24, 27);
                    textField.TextColor = UIColor.White;
                    textField.Font = UIFont.SystemFontOfSize(14);
                    textField.TintColor = UIColor.FromRGB(245, 158, 11);
                    textField.Layer.CornerRadius = 8;
                    textField.Layer.BorderWidth = 1;
                    textField.Layer.BorderColor = UIColor.FromRGB(63, 63, 70).CGColor;
                }
            });

            Microsoft.Maui.Handlers.LabelHandler.Mapper.AppendToMapping("MacCatalystLabel", (handler, view) =>
            {
                if (handler.PlatformView is UILabel label)
                {
                    if (label.Font.PointSize < 12)
                    {
                        label.Font = UIFont.SystemFontOfSize(label.Font.PointSize * 1.15f);
                    }
                }
            });
        }
#endif
    }
}