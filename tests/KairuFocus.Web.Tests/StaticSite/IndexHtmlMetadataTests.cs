using System.Net;
using System.Text.RegularExpressions;

namespace KairuFocus.Web.Tests.StaticSite;

/// <summary>
/// The search and share metadata of <c>index.html</c>. The API serves this file for every route, so these tags are
/// what crawlers and link previews read when they do not run the Blazor app.
/// </summary>
public sealed class IndexHtmlMetadataTests
{
    private const string ExpectedTitle = "KairuFocus — Your developer workspace";

    private static readonly string Html = StaticSiteFiles.ReadText("index.html");

    [Fact]
    public void Should_DeclareEnglish_When_ServingIndexHtml()
    {
        Assert.Matches(@"<html\s+lang=""en""", Html);
    }

    [Fact]
    public void Should_UseProductTitle_When_ServingIndexHtml()
    {
        var titles = Regex.Matches(Html, @"<title>(.*?)</title>", RegexOptions.Singleline)
            .Select(m => WebUtility.HtmlDecode(m.Groups[1].Value.Trim()));

        Assert.Equal(ExpectedTitle, Assert.Single(titles));
    }

    [Fact]
    public void Should_KeepDescriptionShort_When_ServingIndexHtml()
    {
        var description = SingleMeta("description");

        Assert.InRange(description.Length, 50, 160);
    }

    [Fact]
    public void Should_AllowIndexing_When_ServingIndexHtml()
    {
        Assert.Equal("index, follow", SingleMeta("robots"));
    }

    [Fact]
    public void Should_PointToApexDomain_When_DeclaringCanonicalUrl()
    {
        var canonical = Assert.Single(Tags("link"), a => a.GetValueOrDefault("rel") == "canonical");

        Assert.Equal(StaticSiteFiles.CanonicalUrl, canonical.GetValueOrDefault("href"));
        Assert.Equal(StaticSiteFiles.CanonicalUrl, SingleMeta("og:url"));
    }

    [Fact]
    public void Should_DescribeOpenGraphCard_When_ServingIndexHtml()
    {
        Assert.Equal("website", SingleMeta("og:type"));
        Assert.Equal("KairuFocus", SingleMeta("og:site_name"));
        Assert.Equal("en_US", SingleMeta("og:locale"));
        Assert.Equal(ExpectedTitle, SingleMeta("og:title"));
        Assert.Equal(SingleMeta("description"), SingleMeta("og:description"));
        Assert.Equal(StaticSiteFiles.ShareImageUrl, SingleMeta("og:image"));
        Assert.Equal("1200", SingleMeta("og:image:width"));
        Assert.Equal("630", SingleMeta("og:image:height"));
        Assert.False(string.IsNullOrWhiteSpace(SingleMeta("og:image:alt")));
    }

    [Fact]
    public void Should_DescribeTwitterCard_When_ServingIndexHtml()
    {
        Assert.Equal("summary_large_image", SingleMeta("twitter:card"));
        Assert.Equal(ExpectedTitle, SingleMeta("twitter:title"));
        Assert.Equal(SingleMeta("description"), SingleMeta("twitter:description"));
        Assert.Equal(StaticSiteFiles.ShareImageUrl, SingleMeta("twitter:image"));
        Assert.Equal(SingleMeta("og:image:alt"), SingleMeta("twitter:image:alt"));
    }

    /// <summary>Content of the one <c>&lt;meta&gt;</c> whose <c>name</c> or <c>property</c> is <paramref name="key"/>.</summary>
    private static string SingleMeta(string key)
    {
        var meta = Assert.Single(
            Tags("meta"),
            a => (a.GetValueOrDefault("name") ?? a.GetValueOrDefault("property")) == key);

        return meta.GetValueOrDefault("content") ?? string.Empty;
    }

    private static IEnumerable<Dictionary<string, string>> Tags(string tagName) =>
        Regex.Matches(Html, $@"<{tagName}\b[^>]*>", RegexOptions.IgnoreCase)
            .Select(tag => Regex.Matches(tag.Value, @"([\w:-]+)\s*=\s*""([^""]*)""")
                .ToDictionary(
                    m => m.Groups[1].Value.ToLowerInvariant(),
                    m => WebUtility.HtmlDecode(m.Groups[2].Value)));
}
