namespace Kairudev.Application.Pomodoro.LinkTask;

public interface ILinkTaskPresenter
{
    void PresentSuccess();
    void PresentNotFound();
    void PresentFailure(string reason);
}
