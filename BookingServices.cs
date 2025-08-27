using APES.Data;
using NodaTime;
using NodaTime.Text;
using System.Globalization;

namespace APES
{
    public class BookingServices
    {
        public static Dictionary<string, string> commonTimezones = new Dictionary<string, string>
        {
            { "UTC", "Coordinated Universal Time" },            // UTC+0
            { "America/New_York", "Eastern Time (US)" },        // UTC-5
            { "America/Chicago", "Central Time (US)" },        // UTC-6
            { "America/Denver", "Mountain Time (US)" },        // UTC-7
            { "America/Los_Angeles", "Pacific Time (US)" },    // UTC-8
            { "America/Halifax", "Atlantic Time (Canada)" },   // UTC-4
            { "America/St_Johns", "Newfoundland Time" },       // UTC-3:30
            { "America/Argentina/Buenos_Aires", "Argentina Time" }, // UTC-3
            { "Europe/London", "Greenwich Mean Time" },        // UTC+0
            { "Europe/Paris", "Central European Time" },       // UTC+1
            { "Europe/Athens", "Eastern European Time" },     // UTC+2
            { "Europe/Moscow", "Moscow Time" },               // UTC+3
            { "Asia/Dubai", "Gulf Standard Time" },           // UTC+4
            { "Asia/Karachi", "Pakistan Standard Time" },     // UTC+5
            { "Asia/Kolkata", "India Standard Time" },        // UTC+5:30
            { "Asia/Dhaka", "Bangladesh Standard Time" },     // UTC+6
            { "Asia/Bangkok", "Indochina Time" },             // UTC+7
            { "Asia/Shanghai", "China Standard Time" },       // UTC+8
            { "Asia/Tokyo", "Japan Standard Time" },          // UTC+9
            { "Australia/Adelaide", "Australian Central Time" }, // UTC+9:30
            { "Australia/Sydney", "Australian Eastern Time" },   // UTC+10
            { "Pacific/Noumea", "New Caledonia Time" },       // UTC+11
            { "Pacific/Auckland", "New Zealand Standard Time" }, // UTC+12
            { "Pacific/Chatham", "Chatham Islands Time" },   // UTC+12:45
            { "Pacific/Tongatapu", "Tonga Time" }            // UTC+13
        };
    
        public enum SessionLevel
        {
            None = 0,
            Basic = 1,
            Intermediate = 2,
            Advanced = 3,
        }

        public static Data.SessionRequest CreateRequest(ulong userId)
        {
            SessionRequest request = new SessionRequest();
            request.Guid = Guid.NewGuid().ToString();
            request.UserId = userId;
            request.ExpireDateTime = DateTime.UtcNow.AddMinutes(15).ToString("yyyy-MM-dd HH:mm:ss");
            request.TimeSlots = new List<TimeSlot>();
            Program.requestsInSetup.TryAdd(request.Guid, request);

            return request;
        }

        public static string GetLocalTime(string timeZoneId, string utcTime, string date)
        {
            // Parse the UTC date and time
            var datePattern = LocalDatePattern.CreateWithInvariantCulture("yyyy-MM-dd");
            var timePattern = LocalTimePattern.CreateWithInvariantCulture("HH:mm");

            var parsedDate = datePattern.Parse(date).Value;
            var parsedTime = timePattern.Parse(utcTime).Value;

            // Combine into a LocalDateTime (in UTC)
            var utcDateTime = parsedDate + parsedTime;
            var instant = utcDateTime.InZoneStrictly(DateTimeZone.Utc).ToInstant();

            // Convert to the target time zone
            var zone = DateTimeZoneProviders.Tzdb[timeZoneId];
            var zonedTime = instant.InZone(zone);

            // Return the local time as HH:mm
            return zonedTime.TimeOfDay.ToString("HH:mm", null);
        }

        public static string GetLocalDayAndTime(string timeZoneId, string utcTime, string date)
        {
            // Parse the UTC date and time
            var datePattern = LocalDatePattern.CreateWithInvariantCulture("yyyy-MM-dd");
            var timePattern = LocalTimePattern.CreateWithInvariantCulture("HH:mm");

            var parsedDate = datePattern.Parse(date).Value;
            var parsedTime = timePattern.Parse(utcTime).Value;

            // Combine into a LocalDateTime (in UTC)
            var utcDateTime = parsedDate + parsedTime;
            var instant = utcDateTime.InZoneStrictly(DateTimeZone.Utc).ToInstant();

            // Convert to the target time zone
            var zone = DateTimeZoneProviders.Tzdb[timeZoneId];
            var zonedTime = instant.InZone(zone);

            // Format: "Sunday, July 06, 21:30"
            return zonedTime.ToString("dddd, MMMM dd, HH:mm", CultureInfo.InvariantCulture);
        }

        public static string FormatDuration(string hhmm)
        {
            if (!TimeSpan.TryParseExact(hhmm, @"hh\:mm", null, out var timeSpan))
                return "Invalid duration";

            if (timeSpan.Hours > 0 && timeSpan.Minutes > 0)
                return $"{timeSpan.Hours}h {timeSpan.Minutes}m";
            if (timeSpan.Hours > 0)
                return $"{timeSpan.Hours}h";
            return $"{timeSpan.Minutes} minutes";
        }
    }
}
