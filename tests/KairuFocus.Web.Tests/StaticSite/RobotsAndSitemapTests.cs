using System.Xml.Linq;

namespace KairuFocus.Web.Tests.StaticSite;

/// <summary>
/// <c>robots.txt</c> keeps crawlers off the API and the private pages without hiding what Google needs to render
/// the public home page; <c>sitemap.xml</c> lists that page under its canonical address.
/// </summary>
public sealed class RobotsAndSitemapTests
{
    private static readonly string[] RobotsLines = StaticSiteFiles.ReadText("robots.txt")
        .Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

    private static IEnumerable<string> Disallowed =>
        RobotsLines
            .Where(line => line.StartsWith("Disallow:", StringComparison.OrdinalIgnoreCase))
            .Select(line => line["Disallow:".Length..].Trim())
            .Where(path => path.Length > 0);

    [Fact]
    public void Should_PointToSitemap_When_ServingRobotsTxt()
    {
        Assert.Contains("Sitemap: https://kairufocus.com/sitemap.xml", RobotsLines);
    }

    [Theory]
    [InlineData("/api/")]
    [InlineData("/mcp")]
    [InlineData("/dashboard")]
    public void Should_BlockPrivateArea_When_ServingRobotsTxt(string path)
    {
        Assert.Contains(path, Disallowed);
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/login")]
    [InlineData("/_framework/blazor.webassembly.js")]
    [InlineData("/css/app.css")]
    [InlineData("/lib/bootstrap/dist/css/bootstrap.min.css")]
    [InlineData("/og-image.png")]
    [InlineData("/sitemap.xml")]
    public void Should_LetCrawlersFetch_When_PathIsPublicOrNeededToRender(string path)
    {
        Assert.DoesNotContain(Disallowed, prefix => path.StartsWith(prefix, StringComparison.Ordinal));
    }

    [Fact]
    public void Should_ListOnlyCanonicalHome_When_ServingSitemap()
    {
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        var sitemap = XDocument.Parse(StaticSiteFiles.ReadText("sitemap.xml"));

        var locations = sitemap.Root!.Elements(ns + "url").Select(url => url.Element(ns + "loc")?.Value.Trim());

        Assert.Equal(StaticSiteFiles.CanonicalUrl, Assert.Single(locations));
    }
}
