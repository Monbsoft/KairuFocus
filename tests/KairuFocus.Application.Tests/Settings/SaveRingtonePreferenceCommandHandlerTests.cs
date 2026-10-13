using KairuFocus.Application.Settings.Commands.SaveRingtonePreference;
using KairuFocus.Application.Tests.Common;
using KairuFocus.Domain.Settings;
using Microsoft.Extensions.Logging.Abstractions;

namespace KairuFocus.Application.Tests.Settings;

public sealed class SaveRingtonePreferenceCommandHandlerTests
{
    private readonly FakeUserSettingsRepository _repository = new();
    private readonly SaveRingtonePreferenceCommandHandler _sut;

    public SaveRingtonePreferenceCommandHandlerTests()
    {
        _sut = new SaveRingtonePreferenceCommandHandler(
            _repository,
            new FakeCurrentUserService(),
            NullLogger<SaveRingtonePreferenceCommandHandler>.Instance);
    }

    [Theory]
    [InlineData("None", RingtonePreference.None)]
    [InlineData("AlarmClock", RingtonePreference.AlarmClock)]
    [InlineData("Bird", RingtonePreference.Bird)]
    [InlineData("bird", RingtonePreference.Bird)]
    [InlineData("ALARMCLOCK", RingtonePreference.AlarmClock)]
    public async Task Should_PersistRingtonePreference_When_ValueIsMemberName(string value, RingtonePreference expected)
    {
        var result = await _sut.Handle(new SaveRingtonePreferenceCommand(value));

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, _repository.Settings.RingtonePreference);
        Assert.Equal(1, _repository.UpdateCallCount);
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("7")]
    public async Task Should_ReturnFailure_When_ValueIsNumericOutsideEnum(string value)
    {
        var result = await _sut.Handle(new SaveRingtonePreferenceCommand(value));

        Assert.False(result.IsSuccess);
        Assert.Equal(0, _repository.UpdateCallCount);
        Assert.Equal(RingtonePreference.AlarmClock, _repository.Settings.RingtonePreference);
    }

    // Names only: numeric values are not part of the contract, even when they map to a member.
    [Theory]
    [InlineData("0")]
    [InlineData("1")]
    [InlineData("2")]
    public async Task Should_ReturnFailure_When_ValueIsNumericMatchingMember(string value)
    {
        var result = await _sut.Handle(new SaveRingtonePreferenceCommand(value));

        Assert.False(result.IsSuccess);
        Assert.Equal(0, _repository.UpdateCallCount);
        Assert.Equal(RingtonePreference.AlarmClock, _repository.Settings.RingtonePreference);
    }

    [Theory]
    [InlineData("None,Bird")]
    [InlineData(" Bird ")]
    public async Task Should_ReturnFailure_When_ValueIsNotExactlyOneMemberName(string value)
    {
        var result = await _sut.Handle(new SaveRingtonePreferenceCommand(value));

        Assert.False(result.IsSuccess);
        Assert.Equal(0, _repository.UpdateCallCount);
        Assert.Equal(RingtonePreference.AlarmClock, _repository.Settings.RingtonePreference);
    }

    [Theory]
    [InlineData("Trumpet")]
    [InlineData("")]
    public async Task Should_ReturnFailure_When_ValueIsUnknown(string value)
    {
        var result = await _sut.Handle(new SaveRingtonePreferenceCommand(value));

        Assert.False(result.IsSuccess);
        Assert.Equal(0, _repository.UpdateCallCount);
        Assert.Equal(RingtonePreference.AlarmClock, _repository.Settings.RingtonePreference);
    }

    [Fact]
    public async Task Should_ReturnFailure_When_ValueIsNull()
    {
        var result = await _sut.Handle(new SaveRingtonePreferenceCommand(null!));

        Assert.False(result.IsSuccess);
        Assert.Equal(0, _repository.UpdateCallCount);
    }
}
