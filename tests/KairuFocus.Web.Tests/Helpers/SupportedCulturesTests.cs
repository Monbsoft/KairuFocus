using KairuFocus.Web.Helpers;

namespace KairuFocus.Web.Tests.Helpers;

public sealed class SupportedCulturesTests
{
    [Fact]
    public void Should_ListTenCultures_When_FrenchFirst()
    {
        Assert.Equal(10, SupportedCultures.All.Count);
        Assert.Equal("fr", SupportedCultures.All[0].Code);
        Assert.Equal(10, SupportedCultures.All.Select(c => c.Code).Distinct().Count());
    }

    [Fact]
    public void Should_ReturnLabel_When_CodeKnown() => Assert.Equal("🇬🇧 English", SupportedCultures.LabelFor("en"));

    [Fact]
    public void Should_FallBackToFrench_When_CodeUnknown() =>
        Assert.Equal(SupportedCultures.All[0].Label, SupportedCultures.LabelFor("xx"));
}
