namespace SnpDepotManagement.Domain.Common;

// Asia/Ho_Chi_Minh uses UTC+07:00 with no daylight-saving changes.
public static class DepotCalendar
{
    public static readonly TimeSpan UtcOffset = TimeSpan.FromHours(7);
    public static DateOnly GetBusinessDate(DateTime utc) => DateOnly.FromDateTime(utc.Add(UtcOffset));
}
