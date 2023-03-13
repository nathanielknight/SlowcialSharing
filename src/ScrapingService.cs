using SlowcialSharing.Data;

namespace SlowcialSharing;

class ScrapingWorker : IHostedService, IDisposable
{
    private readonly ILogger<ScrapingWorker> _logger;
    private readonly IServiceProvider _services;
    private Timer? _timer;

    public ScrapingWorker(ILogger<ScrapingWorker> logger, IServiceProvider services)
    {
        _logger = logger;
        _services = services;
    }

    public Task StartAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation($"Starting {nameof(ScrapingWorker)}");
        _timer = new Timer(Scrape, null, TimeSpan.Zero, TimeSpan.FromHours(1));
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken stopingToken)
    {
        _logger.LogInformation($"Stopping {nameof(ScrapingWorker)}");
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    private async void Scrape(object? state)
    {
        _logger.LogInformation("Starting scraper task");
        using (var scope = _services.CreateScope())
        using (var dbcontext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>())
        {
            var scraper = new Scraper(dbcontext, _logger);
            await scraper.DoAllUpdates();
        }
    }


    public void Dispose()
    {
        _timer?.Dispose();
    }
}

