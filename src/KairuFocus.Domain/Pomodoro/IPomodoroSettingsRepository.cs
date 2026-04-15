using KairuFocus.Domain.Identity;

namespace KairuFocus.Domain.Pomodoro;

public interface IPomodoroSettingsRepository
{
    Task<PomodoroSettings> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken = default);
    Task SaveAsync(PomodoroSettings settings, UserId userId, CancellationToken cancellationToken = default);
}
