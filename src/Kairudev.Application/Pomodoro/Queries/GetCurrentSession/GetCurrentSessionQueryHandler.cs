using Kairudev.Application.Pomodoro.Common;
using Kairudev.Domain.Pomodoro;

namespace Kairudev.Application.Pomodoro.Queries.GetCurrentSession;

public sealed class GetCurrentSessionQueryHandler
{
    private readonly IPomodoroSessionRepository _repository;

    public GetCurrentSessionQueryHandler(IPomodoroSessionRepository repository)
    {
        _repository = repository;
    }

    public async Task<GetCurrentSessionResult> HandleAsync(
        GetCurrentSessionQuery query,
        CancellationToken cancellationToken = default)
    {
        var session = await _repository.GetActiveAsync(cancellationToken);
        
        return session is not null
            ? GetCurrentSessionResult.WithSession(PomodoroSessionViewModel.From(session))
            : GetCurrentSessionResult.NoSession();
    }
}
