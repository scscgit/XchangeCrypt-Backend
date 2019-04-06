using System;

namespace XchangeCrypt.Backend.ConvergenceService.Extensions
{
    public static class DateTimeExtensions
    {
        public static long GetUnixEpochMillis(this DateTime dateTime)
        {
            var unixTime = dateTime.ToUniversalTime() -
                           new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return (long) unixTime.TotalMilliseconds;
        }

        public static DateTime GetDateTimeFromUnixEpochMillis(this long unixEpochMillis)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(unixEpochMillis);
        }
    }
}
