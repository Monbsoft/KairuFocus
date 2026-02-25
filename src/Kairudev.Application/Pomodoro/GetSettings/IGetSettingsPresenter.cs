using Kairudev.Application.Pomodoro.Common;

namespace Kairudev.Application.Pomodoro.GetSettings;

public interface IGetSettingsPresenter
{
    void PresentSuccess(PomodoroSettingsViewModel settings);
}
