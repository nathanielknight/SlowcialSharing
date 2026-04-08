using SlowcialSharing.Data;

namespace SlowcialSharing;

class Scraper
{
    const int EventId = 1;
    private ApplicationDbContext _context;
    private ILogger _logger;
    public Scraper(ApplicationDbContext context, ILogger logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task DoAllUpdates()
    {
        try
        {
            await FetchNewItems();
            await FetchItemDetails();
        }
        catch (Exception ex)
        {
            _logger.LogError(EventId, ex, "Error during scraping");
        }
    }

    private async Task FetchNewItems()
    {
        _logger.LogInformation("Fetching new items from lobste.rs");

        var lobstersClient = new Clients.LobstersClient(_context, _logger);
        SaveItems(await lobstersClient.FetchNewItems());

        var hackerNewsClient = new Clients.HackerNewsClient(_context, _logger);
        SaveItems(await hackerNewsClient.FetchNewItems());
    }

    private void SaveItems(IEnumerable<Item> items)
    {
        foreach (var item in items)
        {
            if (_context.Items.Any(i => i.Key == item.Key))
            {
                continue;
            }
            _context.Items.Add(item);
        }
        _context.SaveChanges();

    }

    private async Task FetchItemDetails()
    {
        var items = ItemsReadyForScore();
        _logger.LogInformation($"Retrieving item details for {items.Count()} ready items");
        foreach (var item in items)
        {
            Clients.IScraper? client = item.Site.Name switch
            {
                "lobste.rs" => new Clients.LobstersClient(_context, _logger),
                "news.ycombinator.com" => new Clients.HackerNewsClient(_context, _logger),
                _ => null,
            };
            if (client is null)
            {
                _logger.LogError($"No client for site: {item.Site.Name}");
                continue;
            }
            (int Score, int Comments) = await client.FetchItemDetails(item);
            item.Score = Score;
            item.Comments = Comments;
        }
        _context.SaveChanges();
    }

    private TimeSpan OneDay = TimeSpan.FromDays(1);
    private IEnumerable<Item> ItemsReadyForScore() =>
        _context.Items
            .Where(i => i.Score == null)
            .AsEnumerable()  // execute the rest client side so we can use dates
            .Where(item => item.PubDate < DateTimeOffset.UtcNow - OneDay);

}