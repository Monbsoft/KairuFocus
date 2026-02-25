namespace Kairudev.Application.Pomodoro.UpdateTaskStatus;

public interface IUpdateTaskStatusUseCase
{
    Task Execute(UpdateTaskStatusRequest request, CancellationToken cancellationToken = default);
}
