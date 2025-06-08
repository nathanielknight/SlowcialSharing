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

        return Page();
    }
}
