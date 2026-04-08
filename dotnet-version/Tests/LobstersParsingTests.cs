using Xunit;
using HtmlAgilityPack;
using SlowcialSharing.Clients;

namespace SlowcialSharing.Tests;

public class LobstersParsingTests
{
    [Fact]
    public void GetScoreAndComments_ShouldExtractScoreCorrectly()
    {
        // Arrange
        var html = @"
            <div class=""story_liner h-entry"">
                <div class=""voters"">
                    <a class=""upvoter"" href=""/login"">49</a>
                </div>
            </div>";
        
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        
        // Act
        var result = LobstersClient.GetScoreAndComments(doc.DocumentNode);
        
        // Assert
        Assert.Equal(49, result.Score);
        Assert.Equal(0, result.Comments);
    }
    
    [Fact]
    public void GetScoreAndComments_ShouldReturnZeroWhenScoreNodeMissing()
    {
        // Arrange
        var html = @"<div class=""story_liner""><div>no score here</div></div>";
        
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        
        // Act
        var result = LobstersClient.GetScoreAndComments(doc.DocumentNode);
        
        // Assert
        Assert.Equal(0, result.Score);
    }
}