using Kairudev.Application.Common;
using Kairudev.Application.Sprint.Common;
using Kairudev.Domain.Sprint;
using Microsoft.Extensions.Logging;
using Monbsoft.BrilliantMediator.Abstractions.Queries;

namespace Kairudev.Application.Sprint.Queries.GetTodaySprints;

public sealed class GetTodaySprintsQueryHandler : IQueryHandler<GetTodaySprintsQuery, GetTodaySprintsResult>
{
    private readonly ISprintSessionRepository _sprintRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetTodaySprintsQueryHandler> _logger;

    public GetTodaySprintsQueryHandler(
        ISprintSessionRepository sprintRepository,
        ICurrentUserService currentUserService,
        ILogger<GetTodaySprintsQueryHandler> logger)
    {
        _sprintRepository = sprintRepository;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<GetTodaySprintsResult> Handle(
        GetTodaySprintsQuery query,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.CurrentUserId;
        var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.Date);

        _logger.LogDebug("Fetching today sprints for user {UserId}", userId);

        var sessions = await _sprintRepository.GetByDateAsync(today, userId, cancellationToken);

        var viewModels = sessions
            .OrderBy(s => s.StartedAt)
            .Select(SprintSessionViewModel.From)
            .ToList();

        _logger.LogDebug("Found {Count} sprints today for user {UserId}", viewModels.Count, userId);

        return new GetTodaySprintsResult(viewModels);
    }
}
