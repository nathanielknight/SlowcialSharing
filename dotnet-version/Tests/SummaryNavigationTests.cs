using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SlowcialSharing.Data;
using SlowcialSharing.Pages;
using SlowcialSharing.Summary;

namespace SlowcialSharing.Tests;

public class SummaryNavigationTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SummaryModel> _logger;
    private readonly ILogger<SummaryService> _summaryLogger;
    private readonly SummaryService _summaryService;
    private readonly SummaryModel _summaryModel;
    private readonly Site _testSite;

    public SummaryNavigationTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        _context = new ApplicationDbContext(options);
        _logger = new LoggerFactory().CreateLogger<SummaryModel>();
        _summaryLogger = new LoggerFactory().CreateLogger<SummaryService>();
        _summaryService = new SummaryService(_context, _summaryLogger);
        _summaryModel = new SummaryModel(_context, _logger, _summaryService);
        
        _testSite = new Site { SiteId = 1, Name = "TestSite", HomePageUrl = "https://test.com" };
        _context.Sites.Add(_testSite);
        _context.SaveChanges();
    }

    [Fact]
    public void GetNavigationDates_WithNoData_ReturnsNullForBothDates()
    {
        // Arrange
        var currentDate = new DateOnly(2023, 12, 15);

        // Act
        var result = _summaryModel.GetNavigationDates(_testSite, currentDate);

        // Assert
        Assert.Null(result.PreviousDate);
        Assert.Null(result.NextDate);
    }

    [Fact]
    public void GetNavigationDates_WithPreviousDayData_ReturnsPreviousDate()
    {
        // Arrange
        var currentDate = new DateOnly(2023, 12, 15);
        var previousDate = new DateOnly(2023, 12, 14);
        
        _context.Items.Add(new Item
        {
            Key = "test-item-1",
            SiteId = _testSite.SiteId,
            PubDate = previousDate.ToDateTime(TimeOnly.MinValue),
            Title = "Test Item",
            Link = "https://test.com/item",
            CommentsLink = "https://test.com/item/comments",
            Site = _testSite
        });
        _context.SaveChanges();

        // Act
        var result = _summaryModel.GetNavigationDates(_testSite, currentDate);

        // Assert
        Assert.Equal(previousDate, result.PreviousDate);
        Assert.Null(result.NextDate);
    }

    [Fact]
    public void GetNavigationDates_WithNextDayData_ReturnsNextDate()
    {
        // Arrange
        var currentDate = new DateOnly(2023, 12, 15);
        var nextDate = new DateOnly(2023, 12, 16);
        
        _context.Items.Add(new Item
        {
            Key = "test-item-2",
            SiteId = _testSite.SiteId,
            PubDate = nextDate.ToDateTime(TimeOnly.MinValue),
            Title = "Test Item",
            Link = "https://test.com/item",
            CommentsLink = "https://test.com/item/comments",
            Site = _testSite
        });
        _context.SaveChanges();

        // Act
        var result = _summaryModel.GetNavigationDates(_testSite, currentDate);

        // Assert
        Assert.Null(result.PreviousDate);
        Assert.Equal(nextDate, result.NextDate);
    }

    [Fact]
    public void GetNavigationDates_WithFutureNextDay_ReturnsNullForNext()
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.Today);
        var futureDate = today.AddDays(2);
        
        _context.Items.Add(new Item
        {
            Key = "test-item-3",
            SiteId = _testSite.SiteId,
            PubDate = futureDate.ToDateTime(TimeOnly.MinValue),
            Title = "Test Item",
            Link = "https://test.com/item",
            CommentsLink = "https://test.com/item/comments",
            Site = _testSite
        });
        _context.SaveChanges();

        // Act
        var result = _summaryModel.GetNavigationDates(_testSite, today);

        // Assert
        Assert.Null(result.NextDate);
    }

    [Fact]
    public void GetNavigationDates_WithNullCurrentDate_UsesToday()
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.Today);
        var yesterday = today.AddDays(-1);
        
        _context.Items.Add(new Item
        {
            Key = "test-item-4",
            SiteId = _testSite.SiteId,
            PubDate = yesterday.ToDateTime(TimeOnly.MinValue),
            Title = "Test Item",
            Link = "https://test.com/item",
            CommentsLink = "https://test.com/item/comments",
            Site = _testSite
        });
        _context.SaveChanges();

        // Act
        var result = _summaryModel.GetNavigationDates(_testSite, null);

        // Assert
        Assert.Equal(yesterday, result.PreviousDate);
    }

    [Fact]
    public void GetNavigationDates_WithBothPreviousAndNext_ReturnsBothDates()
    {
        // Arrange
        var currentDate = new DateOnly(2023, 12, 15);
        var previousDate = new DateOnly(2023, 12, 14);
        var nextDate = new DateOnly(2023, 12, 16);
        
        _context.Items.AddRange(
            new Item
            {
                Key = "test-item-5",
                SiteId = _testSite.SiteId,
                PubDate = previousDate.ToDateTime(TimeOnly.MinValue),
                Title = "Previous Item",
                Link = "https://test.com/prev",
                CommentsLink = "https://test.com/prev/comments",
                Site = _testSite
            },
            new Item
            {
                Key = "test-item-6",
                SiteId = _testSite.SiteId,
                PubDate = nextDate.ToDateTime(TimeOnly.MinValue),
                Title = "Next Item",
                Link = "https://test.com/next",
                CommentsLink = "https://test.com/next/comments",
                Site = _testSite
            }
        );
        _context.SaveChanges();

        // Act
        var result = _summaryModel.GetNavigationDates(_testSite, currentDate);

        // Assert
        Assert.Equal(previousDate, result.PreviousDate);
        Assert.Equal(nextDate, result.NextDate);
    }

    [Fact]
    public void GetNavigationDates_WithDifferentSite_ReturnsNullForBothDates()
    {
        // Arrange
        var otherSite = new Site { SiteId = 2, Name = "OtherSite", HomePageUrl = "https://other.com" };
        _context.Sites.Add(otherSite);
        
        var currentDate = new DateOnly(2023, 12, 15);
        var previousDate = new DateOnly(2023, 12, 14);
        
        _context.Items.Add(new Item
        {
            Key = "test-item-7",
            SiteId = otherSite.SiteId,
            PubDate = previousDate.ToDateTime(TimeOnly.MinValue),
            Title = "Other Site Item",
            Link = "https://other.com/item",
            CommentsLink = "https://other.com/item/comments",
            Site = otherSite
        });
        _context.SaveChanges();

        // Act
        var result = _summaryModel.GetNavigationDates(_testSite, currentDate);

        // Assert
        Assert.Null(result.PreviousDate);
        Assert.Null(result.NextDate);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}