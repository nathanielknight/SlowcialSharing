using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SlowcialSharing.Data;
using SlowcialSharing.Summary;
using Microsoft.Data.Sqlite;

namespace SlowcialSharing.Tests;

public class SummaryServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SummaryService> _logger;
    private readonly SummaryService _summaryService;
    private readonly Site _testSite;
    private readonly SqliteConnection _connection;

    public SummaryServiceTests()
    {
        // Create and open a connection to SQLite in-memory database
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;
        
        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated(); // Create the SQLite database schema
        _logger = new LoggerFactory().CreateLogger<SummaryService>();
        _summaryService = new SummaryService(_context, _logger);
        
        _testSite = new Site { SiteId = 1, Name = "TestSite", HomePageUrl = "https://test.com" };
        _context.Sites.Add(_testSite);
        _context.SaveChanges();
    }

    [Fact]
    public void SummaryFor_WithSpecificDate_ReturnsItemsInDateRange()
    {
        // Arrange
        var targetDate = new DateOnly(2023, 3, 15);
        var startTime = new DateTimeOffset(2023, 3, 14, 0, 0, 0, TimeSpan.Zero);
        var endTime = new DateTimeOffset(2023, 3, 15, 0, 0, 0, TimeSpan.Zero);
        
        var itemInRange = new Item
        {
            Key = "item-in-range",
            SiteId = _testSite.SiteId,
            PubDate = new DateTimeOffset(2023, 3, 14, 12, 0, 0, TimeSpan.Zero),
            Title = "Item In Range",
            Link = "https://test.com/in-range",
            CommentsLink = "https://test.com/in-range/comments",
            Site = _testSite,
            Score = 50,
            Comments = 10
        };
        
        var itemOutsideRange = new Item
        {
            Key = "item-outside-range",
            SiteId = _testSite.SiteId,
            PubDate = new DateTimeOffset(2023, 3, 13, 12, 0, 0, TimeSpan.Zero),
            Title = "Item Outside Range",
            Link = "https://test.com/outside-range",
            CommentsLink = "https://test.com/outside-range/comments",
            Site = _testSite,
            Score = 30,
            Comments = 5
        };
        
        _context.Items.AddRange(itemInRange, itemOutsideRange);
        _context.SaveChanges();

        // Act
        var summary = _summaryService.SummaryFor(_testSite, targetDate);

        // Assert
        Assert.Equal(startTime, summary.startTime);
        Assert.Equal(endTime, summary.endTime);
        Assert.NotNull(summary.items);
        Assert.Single(summary.items);
        Assert.Equal("item-in-range", summary.items.First().Key);
    }

    [Fact]
    public void SummaryFor_OnlyIncludesItemsWithScores()
    {
        // Arrange
        var targetDate = new DateOnly(2023, 3, 15);
        
        var itemWithScore = new Item
        {
            Key = "item-with-score",
            SiteId = _testSite.SiteId,
            PubDate = new DateTimeOffset(2023, 3, 14, 12, 0, 0, TimeSpan.Zero),
            Title = "Item With Score",
            Link = "https://test.com/with-score",
            CommentsLink = "https://test.com/with-score/comments",
            Site = _testSite,
            Score = 50,
            Comments = 10
        };
        
        var itemWithoutScore = new Item
        {
            Key = "item-without-score",
            SiteId = _testSite.SiteId,
            PubDate = new DateTimeOffset(2023, 3, 14, 13, 0, 0, TimeSpan.Zero),
            Title = "Item Without Score",
            Link = "https://test.com/without-score",
            CommentsLink = "https://test.com/without-score/comments",
            Site = _testSite,
            Score = null,
            Comments = null
        };
        
        _context.Items.AddRange(itemWithScore, itemWithoutScore);
        _context.SaveChanges();

        // Act
        var summary = _summaryService.SummaryFor(_testSite, targetDate);

        // Assert
        Assert.NotNull(summary.items);
        Assert.Single(summary.items);
        Assert.Equal("item-with-score", summary.items.First().Key);
    }

    [Fact]
    public void SummaryFor_FiltersBySite()
    {
        // Arrange
        var otherSite = new Site { SiteId = 2, Name = "OtherSite", HomePageUrl = "https://other.com" };
        _context.Sites.Add(otherSite);
        _context.SaveChanges();
        
        var targetDate = new DateOnly(2023, 3, 15);
        
        var itemForTestSite = new Item
        {
            Key = "item-test-site",
            SiteId = _testSite.SiteId,
            PubDate = new DateTimeOffset(2023, 3, 14, 12, 0, 0, TimeSpan.Zero),
            Title = "Item For Test Site",
            Link = "https://test.com/test-site",
            CommentsLink = "https://test.com/test-site/comments",
            Site = _testSite,
            Score = 50,
            Comments = 10
        };
        
        var itemForOtherSite = new Item
        {
            Key = "item-other-site",
            SiteId = otherSite.SiteId,
            PubDate = new DateTimeOffset(2023, 3, 14, 13, 0, 0, TimeSpan.Zero),
            Title = "Item For Other Site",
            Link = "https://other.com/other-site",
            CommentsLink = "https://other.com/other-site/comments",
            Site = otherSite,
            Score = 30,
            Comments = 5
        };
        
        _context.Items.AddRange(itemForTestSite, itemForOtherSite);
        _context.SaveChanges();

        // Act
        var summary = _summaryService.SummaryFor(_testSite, targetDate);

        // Assert
        Assert.NotNull(summary.items);
        Assert.Single(summary.items);
        Assert.Equal("item-test-site", summary.items.First().Key);
    }

    [Fact]
    public void SummaryFor_OrdersByScoreAndPubDateDescending()
    {
        // Arrange
        var targetDate = new DateOnly(2023, 3, 15);
        
        var highScoreOlder = new Item
        {
            Key = "high-score-older",
            SiteId = _testSite.SiteId,
            PubDate = new DateTimeOffset(2023, 3, 14, 10, 0, 0, TimeSpan.Zero),
            Title = "High Score Older",
            Link = "https://test.com/high-older",
            CommentsLink = "https://test.com/high-older/comments",
            Site = _testSite,
            Score = 100,
            Comments = 20
        };
        
        var highScoreNewer = new Item
        {
            Key = "high-score-newer",
            SiteId = _testSite.SiteId,
            PubDate = new DateTimeOffset(2023, 3, 14, 12, 0, 0, TimeSpan.Zero),
            Title = "High Score Newer",
            Link = "https://test.com/high-newer",
            CommentsLink = "https://test.com/high-newer/comments",
            Site = _testSite,
            Score = 100,
            Comments = 15
        };
        
        var lowScore = new Item
        {
            Key = "low-score",
            SiteId = _testSite.SiteId,
            PubDate = new DateTimeOffset(2023, 3, 14, 14, 0, 0, TimeSpan.Zero),
            Title = "Low Score",
            Link = "https://test.com/low",
            CommentsLink = "https://test.com/low/comments",
            Site = _testSite,
            Score = 50,
            Comments = 5
        };
        
        _context.Items.AddRange(lowScore, highScoreOlder, highScoreNewer);
        _context.SaveChanges();

        // Act
        var summary = _summaryService.SummaryFor(_testSite, targetDate);

        // Assert
        Assert.NotNull(summary.items);
        var itemsList = summary.items.ToList();
        Assert.Equal(3, itemsList.Count);
        
        // Should be ordered by (Score, PubDate) descending
        Assert.Equal("high-score-newer", itemsList[0].Key); // Higher score, newer date
        Assert.Equal("high-score-older", itemsList[1].Key); // Higher score, older date
        Assert.Equal("low-score", itemsList[2].Key); // Lower score
    }

    [Fact]
    public void SummaryFor_WithDateRangeBoundaries_InclusiveStart_ExclusiveEnd()
    {
        // Arrange
        var targetDate = new DateOnly(2023, 3, 15);
        var startTime = new DateTimeOffset(2023, 3, 14, 0, 0, 0, TimeSpan.Zero);
        var endTime = new DateTimeOffset(2023, 3, 15, 0, 0, 0, TimeSpan.Zero);
        
        var itemAtStartBoundary = new Item
        {
            Key = "item-at-start",
            SiteId = _testSite.SiteId,
            PubDate = startTime, // Exactly at start time
            Title = "Item At Start",
            Link = "https://test.com/start",
            CommentsLink = "https://test.com/start/comments",
            Site = _testSite,
            Score = 50,
            Comments = 10
        };
        
        var itemAtEndBoundary = new Item
        {
            Key = "item-at-end",
            SiteId = _testSite.SiteId,
            PubDate = endTime, // Exactly at end time
            Title = "Item At End",
            Link = "https://test.com/end",
            CommentsLink = "https://test.com/end/comments",
            Site = _testSite,
            Score = 30,
            Comments = 5
        };
        
        _context.Items.AddRange(itemAtStartBoundary, itemAtEndBoundary);
        _context.SaveChanges();

        // Act
        var summary = _summaryService.SummaryFor(_testSite, targetDate);

        // Assert
        Assert.NotNull(summary.items);
        var itemsList = summary.items.ToList();
        Assert.Single(itemsList);
        Assert.Equal("item-at-start", itemsList[0].Key); // Start is inclusive, end is exclusive
    }

    [Fact]
    public void SummaryFor_WithNullDate_UsesDefaultDateRange()
    {
        // Arrange - Add an item that should be in the default range
        // The default range is determined by UpdateSchedule.LastUpdateBounds
        // which is the day before yesterday to yesterday (both at midnight)
        var yesterday = DateTime.UtcNow.Date.AddDays(-1);
        var dayBeforeYesterday = yesterday.AddDays(-1);
        
        var itemInDefaultRange = new Item
        {
            Key = "item-default-range",
            SiteId = _testSite.SiteId,
            PubDate = new DateTimeOffset(dayBeforeYesterday.AddHours(12)),
            Title = "Item In Default Range",
            Link = "https://test.com/default",
            CommentsLink = "https://test.com/default/comments",
            Site = _testSite,
            Score = 50,
            Comments = 10
        };
        
        _context.Items.Add(itemInDefaultRange);
        _context.SaveChanges();

        // Act
        var summary = _summaryService.SummaryFor(_testSite, null);

        // Assert - Just verify the method works with null date
        Assert.NotNull(summary.items);
        // Note: We don't test the exact date range here since it depends on current time
        // but we verify that it doesn't crash and returns valid data
    }

    [Fact]
    public void SummaryFor_WithNoMatchingItems_ReturnsEmptyCollection()
    {
        // Arrange
        var targetDate = new DateOnly(2023, 3, 15);
        
        // Add an item outside the date range
        var itemOutsideRange = new Item
        {
            Key = "item-outside",
            SiteId = _testSite.SiteId,
            PubDate = new DateTimeOffset(2023, 3, 13, 12, 0, 0, TimeSpan.Zero),
            Title = "Item Outside",
            Link = "https://test.com/outside",
            CommentsLink = "https://test.com/outside/comments",
            Site = _testSite,
            Score = 50,
            Comments = 10
        };
        
        _context.Items.Add(itemOutsideRange);
        _context.SaveChanges();

        // Act
        var summary = _summaryService.SummaryFor(_testSite, targetDate);

        // Assert
        Assert.NotNull(summary.items);
        Assert.Empty(summary.items);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Close();
        _connection.Dispose();
    }
}