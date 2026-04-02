using Kairu.Domain.Identity;
using Kairu.Domain.Pomodoro;

namespace Kairu.Application.Tests.Pomodoro;

internal sealed class FakePomodoroSettingsRepository : IPomodoroSettingsRepository
{
    public PomodoroSettings Settings { get; set; } = PomodoroSettings.Default;

    public Task<PomodoroSettings> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Settings);

    public Task SaveAsync(PomodoroSettings settings, UserId userId, CancellationToken cancellationToken = default)
    {
        Settings = settings;
        return Task.CompletedTask;
    }
}
