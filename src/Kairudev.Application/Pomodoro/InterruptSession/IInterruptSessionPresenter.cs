namespace Kairudev.Application.Pomodoro.InterruptSession;

public interface IInterruptSessionPresenter
{
    void PresentSuccess();
    void PresentFailure(string reason);
}
