using System.Globalization;

namespace OnlineCourse.Extensions;

public static class DateTimeExtensions
{
    public static string ToPersianDateTime(this DateTime utcDateTime)
    {
        // Define the Iran Standard Time zone
        TimeZoneInfo iranTimeZone;
        try
        {
            // For Windows systems
            iranTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Iran Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            try
            {
                // For Unix/Linux systems
                iranTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Tehran");
            }
            catch (TimeZoneNotFoundException)
            {
                throw new TimeZoneNotFoundException("Iran Standard Time zone not found on this system.");
            }
        }

        // Convert UTC time to Iran Standard Time
        DateTime iranDateTime = TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, iranTimeZone);

        // Use the PersianCalendar to format the date
        PersianCalendar pc = new PersianCalendar();
        return $"{pc.GetYear(iranDateTime)}/{pc.GetMonth(iranDateTime):00}/{pc.GetDayOfMonth(iranDateTime):00} " +
               $"{iranDateTime.Hour:00}:{iranDateTime.Minute:00}";
    }
}
