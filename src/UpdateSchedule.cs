
namespace SlowcialSharing.Schedule;

public static class UpdateSchedule
{
    public static DateTimeOffset LastUpdate =>
        LastMidnight;
    public static DateTimeOffset NextUpdate =>
        LastUpdate + OneDay;

    public static (DateTimeOffset startTime, DateTimeOffset endTime) LastUpdateBounds
    {
        get
        {
            var endTime = LastUpdate;
            var startTime = endTime - OneDay;
            return (startTime, endTime);
        }
    }

    private static DateTimeOffset LastMidnight
    {
        get
        {
            DateTime todayUtc = DateTime.UtcNow.Date;
            DateTimeOffset prevMidnight = new DateTimeOffset(todayUtc.Year, todayUtc.Month, todayUtc.Day, 0, 0, 0, TimeSpan.Zero);
            return prevMidnight;
        }
    }

    private static TimeSpan OneDay = TimeSpan.FromDays(1);
}