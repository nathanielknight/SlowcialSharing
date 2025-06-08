using SlowcialSharing.Data;
using SlowcialSharing.Schedule;

namespace SlowcialSharing.Summary;


public class SummaryService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SummaryService> _logger;
    public SummaryService(ApplicationDbContext context, ILogger<SummaryService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public Summary SummaryFor(Site site, DateOnly? date = null)
    {
        (DateTimeOffset startTime, DateTimeOffset endTime) = CutOffs(date);
        _logger.LogInformation($"Finding {site.Name} items between {startTime} and {endTime}");
        // TODO: replace this with a SQL query to avoid loading the whole table.
        var items = _context.Items
            .Where(i =>
                i.Site == site
                && i.Score != null
            ).AsEnumerable()
            .Where(i =>
                i.PubDate >= startTime
                && i.PubDate < endTime
            )
            .OrderByDescending(i => (i.Score, i.PubDate));
        if (items.Count() > 0)
        {
            _logger.LogInformation($"Found {items.Count()} {site.Name} items where {startTime} < pubdate < {endTime}");
        }
        else
        {
            _logger.LogInformation($"No items for {site.Name} where {startTime} < pubdate < {endTime}");
        }
        return new Summary(startTime: startTime, endTime: endTime, items: items);
    }

    private (DateTimeOffset, DateTimeOffset) CutOffs(DateOnly? date = null)
    {
        if (date.HasValue)
        {
            var targetDate = date.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var endTime = new DateTimeOffset(targetDate, TimeSpan.Zero);
            var startTime = endTime.AddDays(-1);
            return (startTime, endTime);
        }
        return UpdateSchedule.LastUpdateBounds;
    }
}

public record Summary(DateTimeOffset startTime, DateTimeOffset endTime, IEnumerable<Item>? items);