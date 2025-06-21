using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Handlers;
using SkiaSharp.Views.Maui.Controls.Hosting;

namespace Trevoria
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .UseSkiaSharp()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif
            ApplyStyleCustomization();

            return builder.Build();
        }

        private static void ApplyStyleCustomization()
        {
            EntryHandler.Mapper.AppendToMapping("NoUnderline", (handler, _) =>
            {
#if __ANDROID__
                // Remove the underline from the EditText
                handler.PlatformView.SetBackgroundColor(Android.Graphics.Color.Transparent);
#endif
            });

            EntryHandler.Mapper.AppendToMapping("SetUpEntry", (handler, view) =>
            { });
        }
    }
}