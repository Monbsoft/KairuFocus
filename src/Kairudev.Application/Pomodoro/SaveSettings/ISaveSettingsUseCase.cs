namespace Kairudev.Application.Pomodoro.SaveSettings;

public interface ISaveSettingsUseCase
{
    Task Execute(SaveSettingsRequest request, CancellationToken cancellationToken = default);
}
