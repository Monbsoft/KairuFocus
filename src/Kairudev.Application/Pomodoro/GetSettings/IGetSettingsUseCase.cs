namespace Kairudev.Application.Pomodoro.GetSettings;

public interface IGetSettingsUseCase
{
    Task Execute(CancellationToken cancellationToken = default);
}
