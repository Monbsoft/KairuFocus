using KairuFocus.Application.Common;
using KairuFocus.Application.Journal.Common;
using KairuFocus.Domain.Journal;
using KairuFocus.Domain.Pomodoro;
using KairuFocus.Domain.Tasks;
using Microsoft.Extensions.Logging;
using Monbsoft.BrilliantMediator.Abstractions.Queries;

namespace KairuFocus.Application.Journal.Queries.GetTodayJournal;

public sealed class GetTodayJournalQueryHandler : IQueryHandler<GetTodayJournalQuery, GetTodayJournalResult>
{
    private readonly IJournalEntryRepository _repository;
    private readonly IPomodoroSessionRepository _sessionRepository;
    private readonly ITaskRepository _taskRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetTodayJournalQueryHandler> _logger;

    public GetTodayJournalQueryHandler(
        IJournalEntryRepository repository,
        IPomodoroSessionRepository sessionRepository,
        ITaskRepository taskRepository,
        ICurrentUserService currentUserService,
        ILogger<GetTodayJournalQueryHandler> logger)
    {
        _repository = repository;
        _sessionRepository = sessionRepository;
        _taskRepository = taskRepository;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<GetTodayJournalResult> Handle(
        GetTodayJournalQuery query,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.CurrentUserId;
        _logger.LogDebug("Fetching today journal for user {UserId}", userId);
        var entries = await _repository.GetEntriesByDateAsync(DateOnly.FromDateTime(DateTime.UtcNow), userId, cancellationToken);
        var viewModels = await JournalEntryMapper.MapToViewModelsAsync(
            entries, _sessionRepository, _taskRepository, userId, cancellationToken);

        _logger.LogDebug("Found {Count} journal entries for user {UserId}", viewModels.Count, userId);
        return new GetTodayJournalResult(viewModels);
    }
}
