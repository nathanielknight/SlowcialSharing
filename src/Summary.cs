using SlowcialSharing.Data;

namespace SlowcialSharing.Summary;

class SummaryService
{
    private readonly ApplicationDbContext _context;
    public SummaryService(ApplicationDbContext context)
    {
        _context = context;
    }

    public IEnumerable<Item>? SummaryFor(Site site)
    {
        (var startTime, var endTime) = CutOffs();
        var items = _context.Items
            .Where(i =>
                i.Site == site
                && i.Score != null
                && i.PubDate >= startTime
                && i.PubDate < endTime
        );
        if (items.Count() > 0)
        {
            return items;
        }
        else
        {
            return null;
        }
    }

    private (DateTimeOffset, DateTimeOffset) CutOffs()
    {
        var prevMidnight = DateTimeOffset.UtcNow.Date;
        var pprevMidnight = prevMidnight - TimeSpan.FromDays(1);
        var ppprevMidnight = pprevMidnight - TimeSpan.FromDays(1);
        return (ppprevMidnight, pprevMidnight);
    }

}