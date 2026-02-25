namespace Kairudev.Application.Pomodoro.LinkTask;

public interface ILinkTaskUseCase
{
    Task Execute(LinkTaskRequest request, CancellationToken cancellationToken = default);
}
