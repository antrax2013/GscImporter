using GscImporter.Domain;

namespace GscImporter.Tests.Domain;

public sealed class SiteUrlTests
{
    [Fact]
    public void Constructor_RemovesPathQueryAndTrailingSlash()
    {
        var site = new SiteUrl("https://cyril.cophignon.net/somewhere?x=1");
        Assert.Equal("https://cyril.cophignon.net", site.Value);
    }

    [Fact]
    public void Contains_ReturnsFalseForAnotherHost()
    {
        var site = new SiteUrl("https://cyril.cophignon.net");
        Assert.False(site.Contains(new Uri("https://massage-reiki.fr/geobiologie")));
    }
}
