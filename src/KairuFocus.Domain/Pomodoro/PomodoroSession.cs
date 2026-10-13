using KairuFocus.Domain.Common;
using KairuFocus.Domain.Identity;
using KairuFocus.Domain.Tasks;

namespace KairuFocus.Domain.Pomodoro;

public sealed class PomodoroSession : AggregateRoot<PomodoroSessionId>
{
    private readonly List<Guid> _linkedTaskIdValues = [];

    private PomodoroSession(
        PomodoroSessionId id,
        UserId ownerId,
        PomodoroSessionType sessionType,
        int plannedDurationMinutes,
        string? journalComment)
        : base(id)
    {
        OwnerId = ownerId;
        SessionType = sessionType;
        PlannedDurationMinutes = plannedDurationMinutes;
        Status = PomodoroSessionStatus.Planned;
        JournalComment = journalComment;
    }

    public UserId OwnerId { get; private set; } = default!;
    public PomodoroSessionType SessionType { get; }
    public PomodoroSessionStatus Status { get; private set; }
    public int PlannedDurationMinutes { get; }
    public string? JournalComment { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? EndedAt { get; private set; }

    public IReadOnlyList<TaskId> LinkedTaskIds =>
        _linkedTaskIdValues.ConvertAll(TaskId.From).AsReadOnly();

    // Public for EF Core materialization — domain consumers use LinkedTaskIds
    public List<Guid> LinkedTaskIdValues
    {
        get => _linkedTaskIdValues;
        init => _linkedTaskIdValues = value;
    }

    public static PomodoroSession Create(
        PomodoroSessionType sessionType,
        int plannedDurationMinutes,
        UserId ownerId,
        string? journalComment = null) =>
        new(PomodoroSessionId.New(), ownerId, sessionType, plannedDurationMinutes, journalComment);

    public Result Start(DateTime now)
    {
        if (Status != PomodoroSessionStatus.Planned)
            return Result.Failure(DomainErrors.Pomodoro.InvalidTransition);

        Status = PomodoroSessionStatus.Active;
        StartedAt = now;
        return Result.Success();
    }

    public Result Complete(DateTime now)
    {
        if (Status != PomodoroSessionStatus.Active)
            return Result.Failure(DomainErrors.Pomodoro.SessionNotActive);

        Status = PomodoroSessionStatus.Completed;
        EndedAt = ResolveEndedAt(now);
        return Result.Success();
    }

    public Result Interrupt(DateTime now)
    {
        if (Status != PomodoroSessionStatus.Active)
            return Result.Failure(DomainErrors.Pomodoro.SessionNotActive);

        Status = PomodoroSessionStatus.Interrupted;
        EndedAt = ResolveEndedAt(now);
        return Result.Success();
    }

    /// <summary>Scheduled end of a timed session. Null for a free sprint
    /// (PlannedDurationMinutes == 0) or a session that has not started.</summary>
    private DateTime? ScheduledEndAt =>
        PlannedDurationMinutes > 0 && StartedAt.HasValue
            ? StartedAt.Value.AddMinutes(PlannedDurationMinutes)
            : null;

    /// <summary>End time retained for a completion or interruption observed at <paramref name="now"/>:
    /// never before the start, never after the scheduled end of a timed session.</summary>
    private DateTime ResolveEndedAt(DateTime now)
    {
        if (StartedAt.HasValue && now < StartedAt.Value)
            return StartedAt.Value;

        return ScheduledEndAt is { } scheduledEnd && now > scheduledEnd
            ? scheduledEnd
            : now;
    }

    public Result LinkTask(TaskId taskId)
    {
        if (Status != PomodoroSessionStatus.Active)
            return Result.Failure(DomainErrors.Pomodoro.SessionNotActive);

        if (SessionType != PomodoroSessionType.Sprint)
            return Result.Failure(DomainErrors.Pomodoro.TaskLinkingNotAllowedForBreak);

        if (_linkedTaskIdValues.Contains(taskId.Value))
            return Result.Failure(DomainErrors.Pomodoro.TaskAlreadyLinked);

        _linkedTaskIdValues.Add(taskId.Value);
        return Result.Success();
    }

    public Result UnlinkTask(TaskId taskId)
    {
        if (Status != PomodoroSessionStatus.Active)
            return Result.Failure(DomainErrors.Pomodoro.SessionNotActive);

        if (!_linkedTaskIdValues.Remove(taskId.Value))
            return Result.Failure(DomainErrors.Pomodoro.TaskNotLinked);

        return Result.Success();
    }
}
