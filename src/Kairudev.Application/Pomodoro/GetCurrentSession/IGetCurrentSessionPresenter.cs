using Kairudev.Application.Pomodoro.Common;

namespace Kairudev.Application.Pomodoro.GetCurrentSession;

public interface IGetCurrentSessionPresenter
{
    void PresentSession(PomodoroSessionViewModel session);
    void PresentNoSession();
}
