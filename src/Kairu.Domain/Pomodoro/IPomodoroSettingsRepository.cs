using Kairu.Domain.Identity;

namespace Kairu.Domain.Pomodoro;

public interface IPomodoroSettingsRepository
{
    Task<PomodoroSettings> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken = default);
    Task SaveAsync(PomodoroSettings settings, UserId userId, CancellationToken cancellationToken = default);
}
