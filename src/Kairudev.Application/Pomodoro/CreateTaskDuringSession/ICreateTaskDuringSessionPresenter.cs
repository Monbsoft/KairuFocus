using Kairudev.Application.Tasks.Common;

namespace Kairudev.Application.Pomodoro.CreateTaskDuringSession;

public interface ICreateTaskDuringSessionPresenter
{
    void PresentSuccess(TaskViewModel task);
    void PresentValidationError(string error);
    void PresentFailure(string reason);
}
