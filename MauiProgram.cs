using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
#if MACCATALYST
using Microsoft.Maui.Platform;
using UIKit;
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
            builder.ConfigureMauiHandlers(handlers =>
            {
                handlers.AddHandler<Entry, Microsoft.Maui.Handlers.EntryHandler>();
                handlers.AddHandler<Editor, Microsoft.Maui.Handlers.EditorHandler>();
                handlers.AddHandler<SearchBar, Microsoft.Maui.Handlers.SearchBarHandler>();
            });

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
        }
#endif
    }
}