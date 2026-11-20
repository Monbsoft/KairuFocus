using KairuFocus.Application.Common;
using KairuFocus.Application.Community.Common;
using KairuFocus.Application.Journal.Commands.CreateEntry;
using KairuFocus.Domain.Community;
using KairuFocus.Domain.Journal;
using KairuFocus.Domain.Pomodoro;
using Microsoft.Extensions.Logging;
using Monbsoft.BrilliantMediator.Abstractions;
using Monbsoft.BrilliantMediator.Abstractions.Commands;
using CommunityErrors = KairuFocus.Domain.Community.DomainErrors;
using PomodoroErrors = KairuFocus.Domain.Pomodoro.DomainErrors;

namespace KairuFocus.Application.Community.Commands.LaunchGroupStep;

/// <summary>
/// The host launches the next step of the group, at the host's own configured duration. The group is written
/// before the host's session (decision 3): its Version token settles a double click or a second tab. Only the
/// host's session is created here; the participants create theirs when they follow (UC-CO-14).
/// </summary>
public sealed class LaunchGroupStepCommandHandler : ICommandHandler<LaunchGroupStepCommand, LaunchGroupStepResult>
{
    private readonly IMemberProfileRepository _profileRepository;
    private readonly IGroupSessionRepository _groupRepository;
    private readonly IPomodoroSessionRepository _sessionRepository;
    private readonly IPomodoroSettingsRepository _settingsRepository;
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<LaunchGroupStepCommandHandler> _logger;

    public LaunchGroupStepCommandHandler(
        IMemberProfileRepository profileRepository,
        IGroupSessionRepository groupRepository,
        IPomodoroSessionRepository sessionRepository,
        IPomodoroSettingsRepository settingsRepository,
        IMediator mediator,
        ICurrentUserService currentUserService,
        TimeProvider timeProvider,
        ILogger<LaunchGroupStepCommandHandler> logger)
    {
        _profileRepository = profileRepository;
        _groupRepository = groupRepository;
        _sessionRepository = sessionRepository;
        _settingsRepository = settingsRepository;
        _mediator = mediator;
        _currentUserService = currentUserService;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<LaunchGroupStepResult> Handle(
        LaunchGroupStepCommand command,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.CurrentUserId;

        var profile = await _profileRepository.GetByUserIdAsync(userId, cancellationToken);
        if (profile is null)
            return LaunchGroupStepResult.Failure(CommunityErrors.Community.NotMember);

        var group = await _groupRepository.FindLatestForUserAsync(userId, cancellationToken);
        if (group is null || !group.IsHostedBy(userId))
            return LaunchGroupStepResult.Failure(CommunityErrors.Community.NotSprintHost);

        var now = UtcNow;

        // A host absence only computed so far is recorded first; losing that race changes nothing, the
        // group is over either way.
        if (group.EndIfHostAbsent(now))
        {
            await _groupRepository.TryUpdateAsync(group, cancellationToken);
            return LaunchGroupStepResult.Failure(CommunityErrors.Community.GroupEnded);
        }

        var state = group.StateAt(now);
        if (state.Phase == GroupPhase.Ended)
            return LaunchGroupStepResult.Failure(CommunityErrors.Community.GroupEnded);

        if (!TryParseStepType(command.StepType, out var stepType))
            return LaunchGroupStepResult.Failure(CommunityErrors.Community.InvalidGroupStep);

        if (state.Phase == GroupPhase.Running)
            return LaunchGroupStepResult.Failure(CommunityErrors.Community.GroupStepRunning);

        if (group.ActiveMemberCount == 0)
            return await EndAsHostAloneAsync(group, now, cancellationToken);

        var settings = await _settingsRepository.GetByUserIdAsync(userId, cancellationToken);
        var endsAt = now.AddMinutes(DurationMinutes(settings, stepType));

        var launched = group.LaunchStep(stepType, now, endsAt, now);
        if (launched.IsFailure)
            return LaunchGroupStepResult.Failure(launched.Error);

        // A Version conflict is a second launch (double click, second tab) or a stop in between: no session.
        if (!await _groupRepository.TryUpdateAsync(group, cancellationToken))
        {
            _logger.LogInformation(
                "Step launch of group session {GroupId} by user {UserId} rejected by a concurrent write",
                group.Id, userId);
            return LaunchGroupStepResult.Failure(CommunityErrors.Community.GroupStepRunning);
        }

        var sessionType = stepType.ToSessionType();
        var created = PomodoroSession.CreateJoinedStep(
            sessionType, userId, PomodoroSessionId.From(group.Id), endsAt, now);
        if (created.IsFailure || created.Value.Start(now).IsFailure)
            return LaunchGroupStepResult.Failure(PomodoroErrors.Pomodoro.SessionAlreadyActive);

        var session = created.Value;

        // Decision 3: the step stays launched for the participants even when the host already runs another
        // session; the "one active session per owner" index rejects this one.
        if (!await _sessionRepository.TryAddAsync(session, cancellationToken))
        {
            _logger.LogWarning(
                "Step {StepNumber} of group session {GroupId} launched but the session of host {UserId} was rejected",
                group.CurrentStep.Number, group.Id, userId);
            return LaunchGroupStepResult.Failure(PomodoroErrors.Pomodoro.SessionAlreadyActive);
        }

        _logger.LogInformation(
            "User {UserId} launched step {StepNumber} ({StepType}) of group session {GroupId}",
            userId, group.CurrentStep.Number, stepType, group.Id);

        // Same entry as a start on the Pomodoro page (ADR-023). The session is persisted: a failing journal
        // must never fail the launch, and a client disconnect must not abort the entry.
        var eventType = sessionType == PomodoroSessionType.Sprint
            ? JournalEventType.SprintStarted
            : JournalEventType.BreakStarted;
        try
        {
            await _mediator.DispatchAsync<CreateEntryCommand, CreateEntryResult>(
                new CreateEntryCommand(eventType, session.Id.Value, now, userId),
                CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Journal entry failed for session {SessionId}", session.Id.Value);
        }

        return LaunchGroupStepResult.Success(AsUtcOffset(endsAt));
    }

    private DateTime UtcNow => _timeProvider.GetUtcNow().UtcDateTime;

    /// <summary>Every member left during the step: the group ends, the host goes on alone (UC-CO-10 A1).</summary>
    private async Task<LaunchGroupStepResult> EndAsHostAloneAsync(
        GroupSession group,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var ended = group.EndAsHostAlone(now);
        if (ended.IsFailure)
            return LaunchGroupStepResult.Failure(ended.Error);

        // A conflict means the group changed in between (another tab launched or stopped it): the page reads
        // its state again rather than starting a solo session.
        if (!await _groupRepository.TryUpdateAsync(group, cancellationToken))
        {
            _logger.LogInformation("End of group session {GroupId} with its host alone rejected by a concurrent write", group.Id);
            return LaunchGroupStepResult.Failure(CommunityErrors.Community.GroupStepRunning);
        }

        _logger.LogInformation("Group session {GroupId} ended: its host is alone", group.Id);
        return LaunchGroupStepResult.EndedHostAlone();
    }

    /// <summary>Exact names only: <see cref="Enum.TryParse{TEnum}(string?, out TEnum)"/> would also accept
    /// numbers and undefined values.</summary>
    private static bool TryParseStepType(string? value, out GroupStepType stepType)
    {
        foreach (var candidate in Enum.GetValues<GroupStepType>())
        {
            if (string.Equals(candidate.ToString(), value, StringComparison.Ordinal))
            {
                stepType = candidate;
                return true;
            }
        }

        stepType = default;
        return false;
    }

    /// <exception cref="ArgumentOutOfRangeException">An unknown value: a programmer error.</exception>
    private static int DurationMinutes(PomodoroSettings settings, GroupStepType stepType) =>
        stepType switch
        {
            GroupStepType.Sprint => settings.SprintDurationMinutes,
            GroupStepType.ShortBreak => settings.ShortBreakDurationMinutes,
            GroupStepType.LongBreak => settings.LongBreakDurationMinutes,
            _ => throw new ArgumentOutOfRangeException(nameof(stepType), stepType, "Unknown group step type."),
        };

    // A datetime2 column comes back with an unspecified kind: all session instants are UTC.
    private static DateTimeOffset AsUtcOffset(DateTime utc) => new(DateTime.SpecifyKind(utc, DateTimeKind.Utc));
}
