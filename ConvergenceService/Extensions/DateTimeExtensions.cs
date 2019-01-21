using System;

namespace XchangeCrypt.Backend.ConvergenceService.Extensions
{
    public static class DateTimeExtensions
    {
        public static decimal GetUnixEpochMillis(this DateTime dateTime)
        {
            var unixTime = dateTime.ToUniversalTime() -
                           new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return (decimal) unixTime.TotalMilliseconds;
        }
    }
}
