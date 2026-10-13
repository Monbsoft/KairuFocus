using KairuFocus.Application.Settings.Commands.SaveThemePreference;
using KairuFocus.Application.Tests.Common;
using KairuFocus.Domain.Settings;
using Microsoft.Extensions.Logging.Abstractions;

namespace KairuFocus.Application.Tests.Settings;

public sealed class SaveThemePreferenceCommandHandlerTests
{
    private readonly FakeUserSettingsRepository _repository = new();
    private readonly SaveThemePreferenceCommandHandler _sut;

    public SaveThemePreferenceCommandHandlerTests()
    {
        _sut = new SaveThemePreferenceCommandHandler(
            _repository,
            new FakeCurrentUserService(),
            NullLogger<SaveThemePreferenceCommandHandler>.Instance);
    }

    [Theory]
    [InlineData("Light", ThemePreference.Light)]
    [InlineData("Dark", ThemePreference.Dark)]
    [InlineData("System", ThemePreference.System)]
    [InlineData("dark", ThemePreference.Dark)]
    [InlineData("LIGHT", ThemePreference.Light)]
    public async Task Should_PersistThemePreference_When_ValueIsMemberName(string value, ThemePreference expected)
    {
        var result = await _sut.Handle(new SaveThemePreferenceCommand(value));

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, _repository.Settings.ThemePreference);
        Assert.Equal(1, _repository.UpdateCallCount);
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("7")]
    public async Task Should_ReturnFailure_When_ValueIsNumericOutsideEnum(string value)
    {
        var result = await _sut.Handle(new SaveThemePreferenceCommand(value));

        Assert.False(result.IsSuccess);
        Assert.Equal(0, _repository.UpdateCallCount);
        Assert.Equal(ThemePreference.System, _repository.Settings.ThemePreference);
    }

    // Names only: numeric values are not part of the contract, even when they map to a member.
    [Theory]
    [InlineData("0")]
    [InlineData("1")]
    [InlineData("2")]
    public async Task Should_ReturnFailure_When_ValueIsNumericMatchingMember(string value)
    {
        var result = await _sut.Handle(new SaveThemePreferenceCommand(value));

        Assert.False(result.IsSuccess);
        Assert.Equal(0, _repository.UpdateCallCount);
        Assert.Equal(ThemePreference.System, _repository.Settings.ThemePreference);
    }

    [Theory]
    [InlineData("Light,Dark")]
    [InlineData(" Dark ")]
    public async Task Should_ReturnFailure_When_ValueIsNotExactlyOneMemberName(string value)
    {
        var result = await _sut.Handle(new SaveThemePreferenceCommand(value));

        Assert.False(result.IsSuccess);
        Assert.Equal(0, _repository.UpdateCallCount);
        Assert.Equal(ThemePreference.System, _repository.Settings.ThemePreference);
    }

    [Theory]
    [InlineData("Sepia")]
    [InlineData("")]
    public async Task Should_ReturnFailure_When_ValueIsUnknown(string value)
    {
        var result = await _sut.Handle(new SaveThemePreferenceCommand(value));

        Assert.False(result.IsSuccess);
        Assert.Equal(0, _repository.UpdateCallCount);
        Assert.Equal(ThemePreference.System, _repository.Settings.ThemePreference);
    }

    [Fact]
    public async Task Should_ReturnFailure_When_ValueIsNull()
    {
        var result = await _sut.Handle(new SaveThemePreferenceCommand(null!));

        Assert.False(result.IsSuccess);
        Assert.Equal(0, _repository.UpdateCallCount);
    }
}
