using KairuFocus.Application.Community.Commands.JoinOpenSprint;
using KairuFocus.Application.Tests.Common;
using KairuFocus.Application.Tests.Journal;
using KairuFocus.Application.Tests.Pomodoro;
using KairuFocus.Domain.Community;
using KairuFocus.Domain.Identity;
using KairuFocus.Domain.Journal;
using KairuFocus.Domain.Pomodoro;
using Microsoft.Extensions.Logging.Abstractions;
using CommunityErrors = KairuFocus.Domain.Community.DomainErrors;
using PomodoroErrors = KairuFocus.Domain.Pomodoro.DomainErrors;

namespace KairuFocus.Application.Tests.Community;

public sealed class JoinOpenSprintCommandHandlerTests
{
    private const string TargetName = "lea_dev";
    private const string VisitorName = "tomtom";

    private static readonly DateTimeOffset Now = new(2026, 10, 30, 14, 0, 0, TimeSpan.Zero);
    private static readonly DateTime NowUtc = Now.UtcDateTime;
    private static readonly DateTime EndsAtUtc = NowUtc.AddMinutes(18).AddSeconds(24);
    private static readonly Guid RootId = new("10000000-0000-0000-0000-000000000001");
    private static readonly UserId Visitor = FakeCurrentUserService.TestUserId;

    private readonly FakeMemberProfileRepository _profiles = new();
    private readonly FakeOpenSprintReadRepository _openSprints = new();
    private readonly FakePomodoroSessionRepository _sessions = new();
    private readonly FakeJournalEntryRepository _journal = new();
    private readonly FakeMediator _mediator;

    public JoinOpenSprintCommandHandlerTests()
    {
        _mediator = new FakeMediator(_journal);
    }

    private JoinOpenSprintCommandHandler CreateSut(TimeProvider? timeProvider = null) =>
        new(
            _profiles,
            _openSprints,
            _sessions,
            _mediator,
            new FakeCurrentUserService(),
            timeProvider ?? new FixedTimeProvider(Now),
            NullLogger<JoinOpenSprintCommandHandler>.Instance);

    private void GivenVisitorIsMember() => _profiles.Seed(Visitor, VisitorName);

    private void GivenTargetIsJoinable(DateTime? endsAtUtc = null) =>
        _openSprints.SeedJoinable(TargetName, new JoinableSprint(RootId, endsAtUtc ?? EndsAtUtc));

    private static PomodoroSession JoinedSession(Guid rootId, DateTime startedAt, DateTime? sharedEndAt = null)
    {
        var joined = PomodoroSession.CreateJoined(
            Visitor,
            PomodoroSessionId.From(rootId),
            sharedEndAt ?? EndsAtUtc,
            startedAt);
        Assert.True(joined.IsSuccess);
        Assert.True(joined.Value.Start(startedAt).IsSuccess);
        return joined.Value;
    }

    private static PomodoroSession OwnSprint(DateTime startedAt, int plannedMinutes = 25)
    {
        var session = PomodoroSession.Create(PomodoroSessionType.Sprint, plannedMinutes, Visitor);
        Assert.True(session.Start(startedAt).IsSuccess);
        return session;
    }

    // --- Nominal -------------------------------------------------------------------------------

    [Fact]
    public async Task Should_CreateStartedJoinedSession_When_TargetIsJoinable()
    {
        GivenVisitorIsMember();
        GivenTargetIsJoinable();

        var result = await CreateSut().Handle(new JoinOpenSprintCommand(TargetName));

        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.Equal(new DateTimeOffset(EndsAtUtc, TimeSpan.Zero), result.SharedEndAt);

        var session = Assert.Single(_sessions.Sessions);
        Assert.Equal(Visitor, session.OwnerId);
        Assert.Equal(PomodoroSessionStatus.Active, session.Status);
        Assert.Equal(PomodoroSessionType.Sprint, session.SessionType);
        Assert.Equal(PomodoroSessionId.From(RootId), session.SharedSprintId);
        Assert.Equal(EndsAtUtc, session.SharedEndAt);
        Assert.Equal(19, session.PlannedDurationMinutes); // 18:24 rounded up.
        Assert.Equal(NowUtc, session.StartedAt);

        var entry = Assert.Single(_journal.Entries);
        Assert.Equal(JournalEventType.SprintStarted, entry.EventType);
        Assert.Equal(session.Id.Value, entry.ResourceId);
        Assert.Equal(NowUtc, entry.OccurredAt);
        Assert.Equal(Visitor, entry.OwnerId);
    }

    // The XP rule (at least 5 effective minutes) decides later: joining stays allowed.
    [Fact]
    public async Task Should_Allow_When_LessThanFiveMinutesRemain()
    {
        GivenVisitorIsMember();
        GivenTargetIsJoinable(NowUtc.AddMinutes(4));

        var result = await CreateSut().Handle(new JoinOpenSprintCommand(TargetName));

        Assert.True(result.IsSuccess);
        Assert.Equal(4, Assert.Single(_sessions.Sessions).PlannedDurationMinutes);
    }

    // ADR-023: the session is persisted, a failing journal never fails the join.
    [Fact]
    public async Task Should_Succeed_When_JournalFails()
    {
        GivenVisitorIsMember();
        GivenTargetIsJoinable();
        _mediator.ThrowOnCreateEntry = true;

        var result = await CreateSut().Handle(new JoinOpenSprintCommand(TargetName));

        Assert.True(result.IsSuccess);
        Assert.Single(_sessions.Sessions);
        Assert.Empty(_journal.Entries);
    }

    // --- Refusals ------------------------------------------------------------------------------

    [Fact]
    public async Task Should_ReturnNotMember_When_VisitorHasNoProfile()
    {
        GivenTargetIsJoinable();

        var result = await CreateSut().Handle(new JoinOpenSprintCommand(TargetName));

        Assert.False(result.IsSuccess);
        Assert.Equal(CommunityErrors.Community.NotMember, result.Error);
        Assert.Null(result.SharedEndAt);
        Assert.Empty(_sessions.Sessions);
    }

    // Unknown, not a member, closed or ended: the read returns nothing, and all of them look the same.
    [Theory]
    [InlineData("nobody")]        // Unknown or closed: the read finds nothing.
    [InlineData("ab")]            // Malformed: below the minimum length.
    [InlineData("not a name")]    // Malformed: forbidden characters.
    [InlineData("")]              // Malformed: empty.
    [InlineData("admin")]         // Reserved: nobody can hold it.
    public async Task Should_ReturnNotJoinable_When_TargetIsUnknownClosedOrMalformed(string target)
    {
        GivenVisitorIsMember();
        GivenTargetIsJoinable();

        var result = await CreateSut().Handle(new JoinOpenSprintCommand(target));

        Assert.False(result.IsSuccess);
        Assert.Equal(CommunityErrors.Community.SprintNotJoinable, result.Error);
        Assert.Empty(_sessions.Sessions);
    }

    [Theory]
    [InlineData(VisitorName)]
    [InlineData("TomTom")]
    public async Task Should_ReturnOwnSprint_When_TargetIsVisitor(string target)
    {
        GivenVisitorIsMember();
        _openSprints.SeedJoinable(VisitorName, new JoinableSprint(RootId, EndsAtUtc));

        var result = await CreateSut().Handle(new JoinOpenSprintCommand(target));

        Assert.False(result.IsSuccess);
        Assert.Equal(CommunityErrors.Community.CannotJoinOwnSprint, result.Error);
        Assert.Empty(_sessions.Sessions);
    }

    [Fact]
    public async Task Should_ReturnAlreadyActive_When_VisitorHasActiveSession()
    {
        GivenVisitorIsMember();
        GivenTargetIsJoinable();
        _sessions.Sessions.Add(OwnSprint(NowUtc.AddMinutes(-10)));

        var result = await CreateSut().Handle(new JoinOpenSprintCommand(TargetName));

        Assert.False(result.IsSuccess);
        Assert.Equal(PomodoroErrors.Pomodoro.SessionAlreadyActive, result.Error);
        Assert.Single(_sessions.Sessions);
        Assert.Empty(_journal.Entries);
    }

    // A timed session never observed past its end stays Active: the unique index still sees it.
    [Fact]
    public async Task Should_ReturnAlreadyActive_When_VisitorSessionIsExpiredButActive()
    {
        GivenVisitorIsMember();
        GivenTargetIsJoinable();
        _sessions.Sessions.Add(OwnSprint(NowUtc.AddHours(-2), plannedMinutes: 25));

        var result = await CreateSut().Handle(new JoinOpenSprintCommand(TargetName));

        Assert.False(result.IsSuccess);
        Assert.Equal(PomodoroErrors.Pomodoro.SessionAlreadyActive, result.Error);
    }

    // --- Idempotence ---------------------------------------------------------------------------

    [Fact]
    public async Task Should_ReturnExistingSession_When_SameRootWithinFiveSeconds()
    {
        GivenVisitorIsMember();
        GivenTargetIsJoinable();
        var existing = JoinedSession(RootId, NowUtc.AddSeconds(-2));
        _sessions.Sessions.Add(existing);

        var result = await CreateSut().Handle(new JoinOpenSprintCommand(TargetName));

        Assert.True(result.IsSuccess);
        Assert.Equal(new DateTimeOffset(EndsAtUtc, TimeSpan.Zero), result.SharedEndAt);
        Assert.Same(existing, Assert.Single(_sessions.Sessions));
        Assert.Empty(_journal.Entries);
    }

    [Fact]
    public async Task Should_ReturnAlreadyActive_When_SameRootAfterFiveSeconds()
    {
        GivenVisitorIsMember();
        GivenTargetIsJoinable();
        _sessions.Sessions.Add(JoinedSession(RootId, NowUtc.AddSeconds(-6)));

        var result = await CreateSut().Handle(new JoinOpenSprintCommand(TargetName));

        Assert.False(result.IsSuccess);
        Assert.Equal(PomodoroErrors.Pomodoro.SessionAlreadyActive, result.Error);
    }

    [Fact]
    public async Task Should_ReturnAlreadyActive_When_OtherRootWithinFiveSeconds()
    {
        GivenVisitorIsMember();
        GivenTargetIsJoinable();
        _sessions.Sessions.Add(JoinedSession(Guid.NewGuid(), NowUtc.AddSeconds(-2)));

        var result = await CreateSut().Handle(new JoinOpenSprintCommand(TargetName));

        Assert.False(result.IsSuccess);
        Assert.Equal(PomodoroErrors.Pomodoro.SessionAlreadyActive, result.Error);
    }

    // The visitor hosts the group of the target: the target joined the visitor's own sprint.
    // Same root, but the visitor did not join it and has no shared end to return.
    [Fact]
    public async Task Should_ReturnAlreadyActive_When_VisitorHostsTheTargetGroupWithinFiveSeconds()
    {
        GivenVisitorIsMember();
        var hosted = OwnSprint(NowUtc.AddSeconds(-2));
        _sessions.Sessions.Add(hosted);
        _openSprints.SeedJoinable(TargetName, new JoinableSprint(hosted.Id.Value, EndsAtUtc));

        var result = await CreateSut().Handle(new JoinOpenSprintCommand(TargetName));

        Assert.False(result.IsSuccess);
        Assert.Equal(PomodoroErrors.Pomodoro.SessionAlreadyActive, result.Error);
    }

    // The other request inserted its row while this one was reading: a timestamp sampled before the read
    // would give a negative age, and a double click would look like a conflict.
    [Fact]
    public async Task Should_ReturnExistingSession_When_SessionWasInsertedDuringTheRead()
    {
        GivenVisitorIsMember();
        GivenTargetIsJoinable();
        _sessions.Sessions.Add(JoinedSession(RootId, NowUtc.AddMilliseconds(500)));
        var clock = new SteppingTimeProvider(Now, TimeSpan.FromSeconds(1));

        var result = await CreateSut(clock).Handle(new JoinOpenSprintCommand(TargetName));

        Assert.True(result.IsSuccess);
        Assert.Single(_sessions.Sessions);
    }

    [Fact]
    public async Task Should_ReturnExistingSession_When_InsertRejectedForSameRoot()
    {
        GivenVisitorIsMember();
        GivenTargetIsJoinable();
        var winner = JoinedSession(RootId, NowUtc.AddSeconds(-1));
        _sessions.OnTryAdd = () => _sessions.Sessions.Add(winner);

        var result = await CreateSut().Handle(new JoinOpenSprintCommand(TargetName));

        Assert.True(result.IsSuccess);
        Assert.Equal(new DateTimeOffset(EndsAtUtc, TimeSpan.Zero), result.SharedEndAt);
        Assert.Same(winner, Assert.Single(_sessions.Sessions));
        Assert.Empty(_journal.Entries);
    }

    [Fact]
    public async Task Should_ReturnAlreadyActive_When_InsertRejectedForOtherRoot()
    {
        GivenVisitorIsMember();
        GivenTargetIsJoinable();
        var winner = JoinedSession(Guid.NewGuid(), NowUtc.AddSeconds(-1));
        _sessions.OnTryAdd = () => _sessions.Sessions.Add(winner);

        var result = await CreateSut().Handle(new JoinOpenSprintCommand(TargetName));

        Assert.False(result.IsSuccess);
        Assert.Equal(PomodoroErrors.Pomodoro.SessionAlreadyActive, result.Error);
    }

    [Fact]
    public async Task Should_ReturnConcurrentStart_When_InsertRejectedAndNothingReadable()
    {
        GivenVisitorIsMember();
        GivenTargetIsJoinable();
        var winner = OwnSprint(NowUtc);
        _sessions.OnTryAdd = () => _sessions.Sessions.Add(winner);
        _sessions.OnInsertRejected = () => Assert.True(winner.Interrupt(NowUtc).IsSuccess);

        var result = await CreateSut().Handle(new JoinOpenSprintCommand(TargetName));

        Assert.False(result.IsSuccess);
        Assert.Equal(PomodoroErrors.Pomodoro.ConcurrentSessionStart, result.Error);
        Assert.Empty(_journal.Entries);
    }

    /// <summary>Each read of the clock returns a later instant, like a real clock across slow reads.</summary>
    private sealed class SteppingTimeProvider(DateTimeOffset start, TimeSpan step) : TimeProvider
    {
        private DateTimeOffset _next = start;

        public override DateTimeOffset GetUtcNow()
        {
            var current = _next;
            _next += step;
            return current;
        }
    }
}
