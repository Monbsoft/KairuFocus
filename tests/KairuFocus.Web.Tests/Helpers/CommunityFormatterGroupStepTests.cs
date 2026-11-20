using KairuFocus.Web.Helpers;
using KairuFocus.Web.Pages;
using Microsoft.Extensions.Localization;

namespace KairuFocus.Web.Tests.Helpers;

/// <summary>
/// <see cref="CommunityFormatter.GroupStepLabel"/> names the step shown in the group band and announced on the
/// Pomodoro page: a sprint carries its number, a break does not.
/// </summary>
public sealed class CommunityFormatterGroupStepTests
{
    private static readonly CommunityFormatter Formatter = new(
        new FakeLocalizer(
            ("Group.Step.Sprint", "Sprint {0}"),
            ("Group.Step.ShortBreak", "Pause courte"),
            ("Group.Step.LongBreak", "Pause longue")),
        TimeProvider.System);

    [Fact]
    public void Should_NumberSprint_When_StepIsSprint()
    {
        Assert.Equal("Sprint 3", Formatter.GroupStepLabel("Sprint", 3));
    }

    [Fact]
    public void Should_NameBreakWithoutNumber_When_StepIsShortBreak()
    {
        Assert.Equal("Pause courte", Formatter.GroupStepLabel("ShortBreak", 2));
    }

    [Fact]
    public void Should_NameBreakWithoutNumber_When_StepIsLongBreak()
    {
        Assert.Equal("Pause longue", Formatter.GroupStepLabel("LongBreak", 4));
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
