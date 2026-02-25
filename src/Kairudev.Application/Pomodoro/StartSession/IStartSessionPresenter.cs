using Kairudev.Application.Pomodoro.Common;

namespace Kairudev.Application.Pomodoro.StartSession;

public interface IStartSessionPresenter
{
    void PresentSuccess(PomodoroSessionViewModel session);
    void PresentFailure(string reason);
}
