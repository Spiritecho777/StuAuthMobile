using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
using System.Diagnostics;
using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;

#if ANDROID
using Android.Hardware;
using Android.Content.PM;
#elif IOS || MACCATALYST
using AVFoundation;
using Foundation;
#endif

namespace StuAuthMobile
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var hasCamera = CheckIfCameraIsAvailable();
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                })
                .ConfigureLifecycleEvents(events =>
                {
#if ANDROID
                    events.AddAndroid(android => android.OnStop(activity =>
                    {
                        App.CleanupBeforeExit();
                    }));

#elif IOS || MACCATALYST
                    events.AddiOS(ios => ios.WillTerminate(application =>
                    {
                        App.CleanupBeforeExit();
                    }));
#endif
                });
            if (hasCamera) { builder.UseBarcodeReader(); }
            return builder.Build();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
        
        static bool CheckIfCameraIsAvailable()
        {
#if ANDROID
            var packageManager = Android.App.Application.Context.PackageManager;
            return packageManager.HasSystemFeature(PackageManager.FeatureCamera) || packageManager.HasSystemFeature(PackageManager.FeatureCameraFront);

#elif IOS || MACCATALYST
            return AVCaptureDevice.GetDefaultDevice(AVCaptureDeviceType.BuiltInWideAngleCamera, "AVMediaType.Video", AVCaptureDevicePosition.Unspecified) != null;
#else
            return false;
#endif
        }
    }
}