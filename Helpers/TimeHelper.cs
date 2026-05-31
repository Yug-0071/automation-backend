using System;

namespace AutomationBackend.Helpers
{
    public static class TimeHelper
    {
        public static DateTime GetIST()
        {
            try
            {
                TimeZoneInfo istZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, istZone);
            }
            catch (TimeZoneNotFoundException)
            {
                // Fallback for Linux environments if "India Standard Time" is not found
                TimeZoneInfo istZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, istZone);
            }
        }
    }
}
