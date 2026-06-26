using KairuFocus.Application.Pomodoro.Commands.SaveSettings;
using KairuFocus.Application.Tests.Common;
using KairuFocus.Domain.Pomodoro;
using Microsoft.Extensions.Logging.Abstractions;

namespace KairuFocus.Application.Tests.Pomodoro;

public sealed class SaveSettingsCommandHandlerTests
{
    private readonly FakePomodoroSettingsRepository _settingsRepository = new();
    private readonly SaveSettingsCommandHandler _sut;

    public SaveSettingsCommandHandlerTests()
    {
        _sut = new SaveSettingsCommandHandler(
            _settingsRepository,
            new FakeCurrentUserService(),
            NullLogger<SaveSettingsCommandHandler>.Instance);
    }

    [Fact]
    public async Task Should_PersistDailySprintGoal_When_SavingValidSettings()
    {
        var command = new SaveSettingsCommand(25, 5, 15, 8);

        var result = await _sut.Handle(command);

        Assert.True(result.IsSuccess);
        Assert.Equal(8, _settingsRepository.Settings.DailySprintGoal);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(17)]
    public async Task Should_ReturnValidationError_When_DailySprintGoalOutOfBounds(int goal)
    {
        var command = new SaveSettingsCommand(25, 5, 15, goal);

        var result = await _sut.Handle(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrors.Pomodoro.InvalidDailySprintGoal, result.ValidationError);
    }
}
