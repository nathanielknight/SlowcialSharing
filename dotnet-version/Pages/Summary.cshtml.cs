using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;

using SlowcialSharing.Data;

namespace SlowcialSharing.Pages;
public class SummaryModel : PageModel
{
    private ApplicationDbContext _context;
    private ILogger<SummaryModel> _logger;
    private Summary.SummaryService _summarize;

    public string siteName { get; set; }
    public Site site { get; set; }
    public Summary.Summary summary { get; set; }
    public DateOnly? selectedDate { get; set; }
    public DateNavigationResult navigationDates { get; set; }

    public SummaryModel(ApplicationDbContext context, ILogger<SummaryModel> logger, Summary.SummaryService summarize)
    {
        _context = context;
        _logger = logger;
        _summarize = summarize;
    }
    public IActionResult OnGet(string siteName, string? date = null)
    {
        this.siteName = siteName;
        site = _context.Sites.Where(site => site.Name == siteName).First();
        if (site is null)
        {
            return NotFound();
        }

        DateOnly? parsedDate = null;
        if (!string.IsNullOrEmpty(date) && DateOnly.TryParse(date, out var validDate))
        {
            parsedDate = validDate;
        }
        selectedDate = parsedDate;

        summary = _summarize.SummaryFor(site, parsedDate);
        navigationDates = GetNavigationDates(site, parsedDate);

        return Page();
    }

    public DateNavigationResult GetNavigationDates(Site site, DateOnly? currentDate)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var targetDate = currentDate ?? today;
        
        var previousDate = targetDate.AddDays(-1);
        var nextDate = targetDate.AddDays(1);
        
        var previousDateString = previousDate.ToString("yyyy-MM-dd");
        var nextDateString = nextDate.ToString("yyyy-MM-dd");
        
        var hasPreviousData = _context.Items
            .Where(item => item.SiteId == site.SiteId)
            .Any(item => item.PubDate.ToString().StartsWith(previousDateString));
        
        var hasNextData = _context.Items
            .Where(item => item.SiteId == site.SiteId)
            .Any(item => item.PubDate.ToString().StartsWith(nextDateString));
        
        var nextDateIsNotFuture = nextDate <= today;
        
        return new DateNavigationResult
        {
            PreviousDate = hasPreviousData ? previousDate : null,
            NextDate = (hasNextData && nextDateIsNotFuture) ? nextDate : null
        };
    }
}

public class DateNavigationResult
{
    public DateOnly? PreviousDate { get; set; }
    public DateOnly? NextDate { get; set; }
}
