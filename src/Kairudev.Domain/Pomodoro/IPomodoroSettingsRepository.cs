namespace Kairudev.Domain.Pomodoro;

public interface IPomodoroSettingsRepository
{
    Task<PomodoroSettings> GetAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(PomodoroSettings settings, CancellationToken cancellationToken = default);
}
