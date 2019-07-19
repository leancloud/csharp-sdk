using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeanCloud.Realtime.Internal
{
    internal enum UnixTimeStampUnit
    {
        Second = 1,
        Milisecond = 1000,
    }
    internal static class DateTimeEngine
    {
        public static long ToUnixTimeStamp(this DateTime date, UnixTimeStampUnit unit = UnixTimeStampUnit.Milisecond)
        {
            long unixTimestamp = (long)(date.ToUniversalTime().Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            return (unixTimestamp * (int)unit);
        }

        public static DateTime ToDateTime(this long timestamp, UnixTimeStampUnit unit = UnixTimeStampUnit.Milisecond)
        {
            var timespan = timestamp * 1000 / (int)(unit);
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddMilliseconds(timespan).ToLocalTime();
            return dtDateTime;
        }
    }
}
