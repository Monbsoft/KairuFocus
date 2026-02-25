namespace Kairudev.Application.Pomodoro.SaveSettings;

public interface ISaveSettingsPresenter
{
    void PresentSuccess();
    void PresentValidationError(string error);
}
