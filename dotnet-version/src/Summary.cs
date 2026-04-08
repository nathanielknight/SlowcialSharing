using Microsoft.EntityFrameworkCore;
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
        
        // Convert DateTimeOffset to SQLite string format for lexicographic comparison
        var startTimeString = startTime.ToString("yyyy-MM-dd HH:mm:sszzz");
        var endTimeString = endTime.ToString("yyyy-MM-dd HH:mm:sszzz");
        
        // Use raw SQL with string comparison for efficient date filtering in SQLite
        var items = _context.Items
            .FromSqlRaw(@"
                SELECT * FROM Items 
                WHERE SiteId = {0} 
                  AND Score IS NOT NULL 
                  AND PubDate >= {1} 
                  AND PubDate < {2}
                ORDER BY Score DESC, PubDate DESC", 
                site.SiteId, startTimeString, endTimeString);
                
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