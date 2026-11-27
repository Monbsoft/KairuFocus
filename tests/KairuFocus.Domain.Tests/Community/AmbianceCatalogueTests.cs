using KairuFocus.Domain.Community;

namespace KairuFocus.Domain.Tests.Community;

public sealed class AmbianceCatalogueTests
{
    private const string ValidUrl = AmbianceCatalogue.YouTubeVideoPrefix + "abc123";

    private static Ambiance Entry(string code, string url = ValidUrl) =>
        new(code, "Label " + code, AmbianceCategory.Lofi, url);

    [Theory]
    [InlineData("https://vimeo.com/1")]
    [InlineData("http://www.youtube.com/watch?v=x")]
    [InlineData("https://www.youtube.com/@LofiGirl")]
    public void Should_Throw_When_UrlIsNotAYouTubeVideo(string url)
    {
        Assert.Throws<ArgumentException>(() => new AmbianceCatalogue([Entry("lofi", url)]));
    }

    [Fact]
    public void Should_Throw_When_CodesAreDuplicated()
    {
        Assert.Throws<ArgumentException>(() => new AmbianceCatalogue([Entry("lofi"), Entry("lofi")]));
    }

    [Fact]
    public void Should_Throw_When_CodeIsEmptyOrTooLong()
    {
        Assert.Throws<ArgumentException>(() => new AmbianceCatalogue([Entry(" ")]));
        Assert.Throws<ArgumentException>(() => new AmbianceCatalogue([Entry(new string('a', AmbianceCatalogue.MaxCodeLength + 1))]));
    }

    [Fact]
    public void Should_FindNothing_When_CodeIsUnknownOrNull()
    {
        var catalogue = new AmbianceCatalogue([Entry("lofi")]);

        Assert.Null(catalogue.Find(null));
        Assert.Null(catalogue.Find(""));
        Assert.Null(catalogue.Find("jazz"));
        Assert.Null(catalogue.Find("LOFI"));
    }

    [Fact]
    public void Should_FindEntry_When_CodeIsKnown()
    {
        var lofi = Entry("lofi");
        var catalogue = new AmbianceCatalogue([Entry("jazz"), lofi]);

        Assert.Equal(lofi, catalogue.Find("lofi"));
        Assert.Equal(["jazz", "lofi"], catalogue.All.Select(a => a.Code));
    }

    [Fact]
    public void Should_BuildDefaultCatalogue_When_Loaded()
    {
        var catalogue = AmbianceCatalogue.Default;

        Assert.NotNull(catalogue);
        Assert.Equal(catalogue.All.Count, catalogue.All.Select(a => a.Code).Distinct(StringComparer.Ordinal).Count());
    }
}
