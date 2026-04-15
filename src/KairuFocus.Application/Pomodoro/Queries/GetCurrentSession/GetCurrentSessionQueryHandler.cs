using KairuFocus.Application.Common;
using KairuFocus.Application.Pomodoro.Common;
using KairuFocus.Domain.Pomodoro;
using Microsoft.Extensions.Logging;
using Monbsoft.BrilliantMediator.Abstractions.Queries;

namespace KairuFocus.Application.Pomodoro.Queries.GetCurrentSession;

public sealed class GetCurrentSessionQueryHandler : IQueryHandler<GetCurrentSessionQuery, GetCurrentSessionResult>
{
    private readonly IPomodoroSessionRepository _repository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetCurrentSessionQueryHandler> _logger;

    public GetCurrentSessionQueryHandler(
        IPomodoroSessionRepository repository,
        ICurrentUserService currentUserService,
        ILogger<GetCurrentSessionQueryHandler> logger)
    {
        _repository = repository;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<GetCurrentSessionResult> Handle(
        GetCurrentSessionQuery query,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.CurrentUserId;
        _logger.LogDebug("Fetching active session for user {UserId}", userId);
        var session = await _repository.GetActiveAsync(userId, cancellationToken);

        return session is not null
            ? GetCurrentSessionResult.WithSession(PomodoroSessionViewModel.From(session))
            : GetCurrentSessionResult.NoSession();
    }
}
