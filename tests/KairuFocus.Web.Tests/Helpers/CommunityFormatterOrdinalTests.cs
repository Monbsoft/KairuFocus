using KairuFocus.Web.Helpers;
using KairuFocus.Web.Pages;
using Microsoft.Extensions.Localization;

namespace KairuFocus.Web.Tests.Helpers;

/// <summary>
/// <see cref="CommunityFormatter.Ordinal"/> cuts the ordinal resource around its <c>{0}</c> with
/// <see cref="CommunityFormatter.SplitAtPlaceholder"/>, and falls back to the bare number when the resource is missing.
/// Value 1 uses the <c>One</c> category in every language, so the tests do not depend on the culture of the machine.
/// </summary>
public sealed class CommunityFormatterOrdinalTests
{
    [Fact]
    public void Should_ReturnBareNumber_When_OrdinalResourceIsMissing()
    {
        var formatter = new CommunityFormatter(new FakeLocalizer(), TimeProvider.System);

        Assert.Equal(new OrdinalParts("", "1", ""), formatter.Ordinal(1));
    }

    [Fact]
    public void Should_KeepMarkerAroundNumber_When_OrdinalResourceHasPlaceholder()
    {
        var formatter = new CommunityFormatter(
            new FakeLocalizer(("Community.Ordinal.One", "第{0}名")), TimeProvider.System);

        Assert.Equal(new OrdinalParts("第", "1", "名"), formatter.Ordinal(1));
    }

    /// <summary>Like the resource localizer: a missing key comes back as the key itself.</summary>
    private sealed class FakeLocalizer(params (string Key, string Value)[] resources) : IStringLocalizer<Community>
    {
        private readonly Dictionary<string, string> _resources = resources.ToDictionary(r => r.Key, r => r.Value);

        public LocalizedString this[string name] =>
            _resources.TryGetValue(name, out var value)
                ? new LocalizedString(name, value)
                : new LocalizedString(name, name, resourceNotFound: true);

        public LocalizedString this[string name, params object[] arguments] =>
            new(name, string.Format(this[name].Value, arguments), this[name].ResourceNotFound);

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
            _resources.Select(r => new LocalizedString(r.Key, r.Value));
    }
}
