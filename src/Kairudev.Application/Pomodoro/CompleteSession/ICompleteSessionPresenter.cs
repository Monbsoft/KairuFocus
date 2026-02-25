namespace Kairudev.Application.Pomodoro.CompleteSession;

public interface ICompleteSessionPresenter
{
    void PresentSuccess(CompleteSessionResult result);
    void PresentFailure(string reason);
}
