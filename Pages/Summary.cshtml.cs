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
    public Site site {get; set;}

    public SummaryModel(ApplicationDbContext context, ILogger<SummaryModel> logger, Summary.SummaryService summarize)
    {
        _context = context;
        _logger = logger;
        _summarize = summarize;
    }
    public IActionResult OnGet(string siteName)
    {
        this.siteName = siteName;
        var site = _context.Sites.Where(site => site.Name == siteName).First();
        if (site is null)
        {
            return NotFound();
        }
        return Page();
    }

    public IEnumerable<Item> Items
    {
        get => _summarize.SummaryFor(site);
    }
}
