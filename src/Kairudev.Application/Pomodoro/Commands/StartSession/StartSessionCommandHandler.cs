using Kairudev.Application.Common;
using Kairudev.Application.Journal.Commands.CreateEntry;
using Kairudev.Application.Pomodoro.Common;
using Kairudev.Domain.Journal;
using Kairudev.Domain.Pomodoro;
using Microsoft.Extensions.Logging;
using Monbsoft.BrilliantMediator.Abstractions;
using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace Kairudev.Application.Pomodoro.Commands.StartSession;

public sealed class StartSessionCommandHandler : ICommandHandler<StartSessionCommand, StartSessionResult>
{
    private readonly IPomodoroSessionRepository _sessionRepository;
    private readonly IPomodoroSettingsRepository _settingsRepository;
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<StartSessionCommandHandler> _logger;

    public StartSessionCommandHandler(
        IPomodoroSessionRepository sessionRepository,
        IPomodoroSettingsRepository settingsRepository,
        IMediator mediator,
        ICurrentUserService currentUserService,
        ILogger<StartSessionCommandHandler> logger)
    {
        _sessionRepository = sessionRepository;
        _settingsRepository = settingsRepository;
        _mediator = mediator;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<StartSessionResult> Handle(
        StartSessionCommand command,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.CurrentUserId;

        _logger.LogDebug("Starting session of type {SessionType} for user {UserId}", command.SessionType, userId);

        var existingSession = await _sessionRepository.GetActiveAsync(userId, cancellationToken);
        if (existingSession is not null)
        {
            _logger.LogWarning("Session already active for user {UserId}", userId);
            return StartSessionResult.Failure("A session is already active");
        }

        var settings = await _settingsRepository.GetByUserIdAsync(userId, cancellationToken);

        // Parse session type
        var sessionType = string.IsNullOrWhiteSpace(command.SessionType) ||
                          !Enum.TryParse<PomodoroSessionType>(command.SessionType, true, out var parsedType)
            ? PomodoroSessionType.Sprint
            : parsedType;

        // Determine duration based on type (in minutes)
        var durationMinutes = sessionType switch
        {
            PomodoroSessionType.Sprint => settings.SprintDurationMinutes,
            PomodoroSessionType.ShortBreak => settings.ShortBreakDurationMinutes,
            PomodoroSessionType.LongBreak => settings.LongBreakDurationMinutes,
            _ => settings.SprintDurationMinutes
        };

        var session = PomodoroSession.Create(sessionType, durationMinutes, userId);
        var startResult = session.Start(DateTime.UtcNow);
        if (startResult.IsFailure)
            return StartSessionResult.Failure(startResult.Error);

        await _sessionRepository.AddAsync(session, cancellationToken);
        _logger.LogInformation("Session {SessionId} of type {SessionType} started for user {UserId}", session.Id.Value, sessionType, userId);

        var eventType = sessionType == PomodoroSessionType.Sprint
            ? JournalEventType.SprintStarted
            : JournalEventType.BreakStarted;

        // Generate journal entry
        await _mediator.DispatchAsync<CreateEntryCommand, CreateEntryResult>(
            new CreateEntryCommand(
                eventType,
                session.Id.Value,
                DateTime.UtcNow,
                userId),
            cancellationToken);

        return StartSessionResult.Success(PomodoroSessionViewModel.From(session));
    }
}
