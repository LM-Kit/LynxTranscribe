using LynxTranscribe.Services;

namespace LynxTranscribe.Helpers
{
    internal class SystemEx
    {
        public static int PhysicalCoreCount;

        public static void Initialize(AppSettingsService settingsService)
        {
            // Initialize LM-Kit runtime with configured model storage directory
            LMKit.Global.Configuration.ModelStorageDirectory = settingsService.ModelStorageDirectory;
            LMKit.Global.Runtime.EnableCuda = true;
            LMKit.Global.Runtime.EnableVulkan = true;
            LMKit.Global.Runtime.Initialize();

            PhysicalCoreCount = LMKit.Global.Configuration.ThreadCount;

            // Apply resource usage setting
            ApplyResourceUsage(settingsService);
        }

        public static void ApplyResourceUsage(AppSettingsService settingsService)
        {
            double factor = settingsService.GetResourceFactor();
            int maxThreads = Math.Max(1, (int)(PhysicalCoreCount * factor));
            LMKit.Global.Configuration.ThreadCount = maxThreads;
        }

        public static int GetCurrentThreadCount(AppSettingsService settingsService)
        {
            double factor = settingsService.GetResourceFactor();
            return Math.Max(1, (int)(PhysicalCoreCount * factor));
        }
    }
}
