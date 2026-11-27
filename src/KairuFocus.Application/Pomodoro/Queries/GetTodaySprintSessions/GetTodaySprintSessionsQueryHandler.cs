using KairuFocus.Application.Common;
using KairuFocus.Application.Pomodoro.Common;
using KairuFocus.Domain.Pomodoro;
using Microsoft.Extensions.Logging;
using Monbsoft.BrilliantMediator.Abstractions.Queries;

namespace KairuFocus.Application.Pomodoro.Queries.GetTodaySprintSessions;

public sealed class GetTodaySprintSessionsQueryHandler
    : IQueryHandler<GetTodaySprintSessionsQuery, GetTodaySprintSessionsResult>
{
    private readonly IPomodoroSessionRepository _sessionRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetTodaySprintSessionsQueryHandler> _logger;
    private readonly TimeProvider _timeProvider;

    public GetTodaySprintSessionsQueryHandler(
        IPomodoroSessionRepository sessionRepository,
        ICurrentUserService currentUserService,
        ILogger<GetTodaySprintSessionsQueryHandler> logger,
        TimeProvider timeProvider)
    {
        _sessionRepository = sessionRepository;
        _currentUserService = currentUserService;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task<GetTodaySprintSessionsResult> Handle(
        GetTodaySprintSessionsQuery query,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.CurrentUserId;

        _logger.LogDebug("Fetching today sprint sessions for user {UserId} (offsetMinutes={Offset})", userId, query.OffsetMinutes);

        // Local "today" window expressed as a UTC range (offset clamped inside LocalDayWindow.From).
        var window = LocalDayWindow.From(_timeProvider.GetUtcNow().UtcDateTime, query.OffsetMinutes);

        var sessions = await _sessionRepository.GetTodaySprintSessionsAsync(
            userId, window.StartUtc, window.EndUtc, cancellationToken);

        var viewModels = sessions
            .OrderBy(s => s.StartedAt)
            .Select(PomodoroSessionViewModel.From)
            .ToList();

        _logger.LogDebug("Found {Count} sprint sessions today for user {UserId}", viewModels.Count, userId);

        return new GetTodaySprintSessionsResult(viewModels);
    }
}
