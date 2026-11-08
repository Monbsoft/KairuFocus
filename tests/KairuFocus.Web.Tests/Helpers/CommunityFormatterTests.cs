using KairuFocus.Web.Helpers;

namespace KairuFocus.Web.Tests.Helpers;

/// <summary>
/// The static helpers of <see cref="CommunityFormatter"/> that build raw markup from member names (chosen by the members)
/// and translated templates: everything must come out HTML-encoded, in a single pass.
/// </summary>
public sealed class CommunityFormatterTests
{
    // --- WithLinks ------------------------------------------------------------------------------

    [Fact]
    public void Should_LinkName_When_ArgumentHasAddress()
    {
        var html = CommunityFormatter.WithLinks("Avec {0}", ("tomtom", "/membres/tomtom")).Value;

        Assert.Equal("Avec <a href=\"/membres/tomtom\">tomtom</a>", html);
    }

    [Fact]
    public void Should_WritePlainText_When_ArgumentHasNoAddress()
    {
        var html = CommunityFormatter.WithLinks("{0} et {1} autres", ("tomtom", "/membres/tomtom"), ("2", null)).Value;

        Assert.Equal("<a href=\"/membres/tomtom\">tomtom</a> et 2 autres", html);
    }

    [Fact]
    public void Should_EncodeName_When_NameContainsMarkup()
    {
        const string name = "<img src=x onerror=alert(1)>";

        var html = CommunityFormatter.WithLinks("Avec {0}", (name, CommunityFormatter.ProfileHref(name))).Value;

        Assert.DoesNotContain("<img", html, StringComparison.Ordinal);
        Assert.Contains(">&lt;img src=x onerror=alert(1)&gt;</a>", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Should_EncodeTemplate_When_TemplateContainsMarkup()
    {
        var html = CommunityFormatter.WithLinks("<b>{0}</b> & co", ("tomtom", null)).Value;

        Assert.Equal("&lt;b&gt;tomtom&lt;/b&gt; &amp; co", html);
    }

    [Fact]
    public void Should_EncodeAddress_When_AddressContainsQuote()
    {
        var html = CommunityFormatter.WithLinks("{0}", ("tomtom", "/x\" onclick=\"alert(1)")).Value;

        Assert.Equal("<a href=\"/x&quot; onclick=&quot;alert(1)\">tomtom</a>", html);
    }

    [Fact]
    public void Should_KeepNameLiteral_When_NameLooksLikePlaceholder()
    {
        var html = CommunityFormatter.WithLinks("{0} et {1}", ("{1}", null), ("tomtom", null)).Value;

        Assert.Equal("{1} et tomtom", html);
    }

    [Fact]
    public void Should_KeepPlaceholder_When_IndexHasNoArgument()
    {
        var html = CommunityFormatter.WithLinks("{0} et {3}", ("tomtom", null)).Value;

        Assert.Equal("tomtom et {3}", html);
    }

    // --- WithNewTabLinks ------------------------------------------------------------------------

    [Fact]
    public void Should_OpenInNewTabAndSayIt_When_NewTabLinks()
    {
        var html = CommunityFormatter.WithNewTabLinks("Avec {0}", "(nouvel onglet)", ("tomtom", "/membres/tomtom")).Value;

        Assert.Equal(
            // A no-break space keeps the arrow on the line of the name.
            "Avec <a href=\"/membres/tomtom\" target=\"_blank\" rel=\"noopener\">tomtom<span aria-hidden=\"true\"> ↗</span>"
            + "<span class=\"visually-hidden\"> (nouvel onglet)</span></a>",
            html);
    }

    [Fact]
    public void Should_EncodeNewTabText_When_TextContainsMarkup()
    {
        var html = CommunityFormatter.WithNewTabLinks("{0}", "<script>", ("tomtom", "/membres/tomtom")).Value;

        Assert.Contains("<span class=\"visually-hidden\"> &lt;script&gt;</span>", html, StringComparison.Ordinal);
        Assert.DoesNotContain("<script>", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Should_WritePlainText_When_NewTabArgumentHasNoAddress()
    {
        var html = CommunityFormatter.WithNewTabLinks("{0}", "(nouvel onglet)", ("2", null)).Value;

        Assert.Equal("2", html);
    }

    // --- ProfileHref ----------------------------------------------------------------------------

    [Theory]
    [InlineData("tomtom", "/membres/tomtom")]
    [InlineData("a/b c", "/membres/a%2Fb%20c")]
    [InlineData("lea?x=1#y", "/membres/lea%3Fx%3D1%23y")]
    public void Should_EscapeName_When_BuildingProfileAddress(string name, string expected)
    {
        Assert.Equal(expected, CommunityFormatter.ProfileHref(name));
    }

    // --- SplitAtPlaceholder ---------------------------------------------------------------------

    [Fact]
    public void Should_CutAroundFirstPlaceholder_When_TemplateHasOne()
    {
        Assert.Equal(("encore ", " min"), CommunityFormatter.SplitAtPlaceholder("encore {0} min"));
    }

    [Fact]
    public void Should_CutAroundFirstOnly_When_TemplateHasTwo()
    {
        Assert.Equal(("", " et {0}"), CommunityFormatter.SplitAtPlaceholder("{0} et {0}"));
    }

    [Fact]
    public void Should_ReturnNull_When_TemplateHasNoPlaceholder()
    {
        Assert.Null(CommunityFormatter.SplitAtPlaceholder("OpenSprints.Remaining"));
    }
}
