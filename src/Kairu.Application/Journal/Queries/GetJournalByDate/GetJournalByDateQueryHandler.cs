using Kairu.Application.Common;
using Kairu.Application.Journal.Common;
using Kairu.Domain.Journal;
using Kairu.Domain.Pomodoro;
using Kairu.Domain.Tasks;
using Microsoft.Extensions.Logging;
using Monbsoft.BrilliantMediator.Abstractions.Queries;

namespace Kairu.Application.Journal.Queries.GetJournalByDate;

public sealed class GetJournalByDateQueryHandler : IQueryHandler<GetJournalByDateQuery, GetJournalByDateResult>
{
    private readonly IJournalEntryRepository _repository;
    private readonly IPomodoroSessionRepository _sessionRepository;
    private readonly ITaskRepository _taskRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetJournalByDateQueryHandler> _logger;

    public GetJournalByDateQueryHandler(
        IJournalEntryRepository repository,
        IPomodoroSessionRepository sessionRepository,
        ITaskRepository taskRepository,
        ICurrentUserService currentUserService,
        ILogger<GetJournalByDateQueryHandler> logger)
    {
        _repository = repository;
        _sessionRepository = sessionRepository;
        _taskRepository = taskRepository;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<GetJournalByDateResult> Handle(
        GetJournalByDateQuery query,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.CurrentUserId;
        _logger.LogDebug("Fetching journal for date {Date} for user {UserId}", query.Date, userId);
        var entries = await _repository.GetEntriesByDateAsync(query.Date, userId, cancellationToken);
        var viewModels = await JournalEntryMapper.MapToViewModelsAsync(
            entries, _sessionRepository, _taskRepository, userId, cancellationToken);

        _logger.LogDebug("Found {Count} journal entries for date {Date} for user {UserId}", viewModels.Count, query.Date, userId);
        return new GetJournalByDateResult(viewModels);
    }
}
