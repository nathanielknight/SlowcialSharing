using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SlowcialSharing.Data;

namespace SlowcialSharing.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context, ILogger<IndexModel> logger)
    {
        _logger = logger;
        _context = context;
    }

    public IEnumerable<Site> Sites
    {
        get => _context.Sites.OrderBy(i => i.Name);
    }
}
