namespace SnpDepotManagement.Application.Common.Time;

public static class DepotTime
{
    public static readonly TimeSpan UtcOffset = SnpDepotManagement.Domain.Common.DepotCalendar.UtcOffset;
    public static DateOnly GetBusinessDate(DateTime utc) => SnpDepotManagement.Domain.Common.DepotCalendar.GetBusinessDate(utc);
    public static DateTime StartOfBusinessDateUtc(DateOnly date) =>
        DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue).Subtract(UtcOffset), DateTimeKind.Utc);
}
