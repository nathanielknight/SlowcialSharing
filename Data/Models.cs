using System.ComponentModel.DataAnnotations;

namespace SlowcialSharing.Data;

public class Item
{
    public required string Title { get; set; }
    public required string Link { get; set; }
    [Key]
    public required string guid { get; set; }
    public required DateTimeOffset PubDate { get; set; }
    public required string Author { get; set; }
    public int? Score { get; set; }
    public int? Comments { get; set; }

    public int SiteId { get; set; }
    public required Site Site { get; set; }
}


public class Site
{
    public required int SiteId { get; set; }
    public required string Name { get; set; }
    public required string Url { get; set; }
    public DateTimeOffset? lastChecked { get; set; }
}
