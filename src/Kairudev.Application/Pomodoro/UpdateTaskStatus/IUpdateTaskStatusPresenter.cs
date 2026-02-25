using Kairudev.Application.Tasks.Common;

namespace Kairudev.Application.Pomodoro.UpdateTaskStatus;

public interface IUpdateTaskStatusPresenter
{
    void PresentSuccess(TaskViewModel task);
    void PresentNotFound();
    void PresentFailure(string reason);
}
