namespace Kairudev.Application.Pomodoro.CreateTaskDuringSession;

public interface ICreateTaskDuringSessionUseCase
{
    Task Execute(CreateTaskDuringSessionRequest request, CancellationToken cancellationToken = default);
}
