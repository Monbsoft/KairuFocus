using KairuFocus.Application.Community.Commands.LeaveOrStopGroupSession;
using KairuFocus.Application.Pomodoro.Commands.InterruptSession;
using KairuFocus.Application.Tests.Common;
using KairuFocus.Application.Tests.Journal;
using KairuFocus.Application.Tests.Pomodoro;
using KairuFocus.Domain.Community;
using KairuFocus.Domain.Identity;
using KairuFocus.Domain.Journal;
using KairuFocus.Domain.Pomodoro;
using Microsoft.Extensions.Logging.Abstractions;
using CommunityErrors = KairuFocus.Domain.Community.DomainErrors;

namespace KairuFocus.Application.Tests.Community;

public sealed class LeaveOrStopGroupSessionCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 30, 14, 0, 0, TimeSpan.Zero);
    private static readonly DateTime NowUtc = Now.UtcDateTime;
    private static readonly UserId Host = UserId.From(new Guid("20000000-0000-0000-0000-000000000002"));
    private static readonly UserId Participant = UserId.From(new Guid("30000000-0000-0000-0000-000000000003"));
    private static readonly UserId Other = UserId.From(new Guid("40000000-0000-0000-0000-000000000004"));

    private readonly FakeMemberProfileRepository _profiles = new();
    private readonly FakePomodoroSessionRepository _sessions = new();
    private readonly FakeGroupSessionRepository _groups = new();
    private readonly FakeJournalEntryRepository _journal = new();

    private LeaveOrStopGroupSessionCommandHandler CreateSut(UserId me, IGroupSessionRepository? groups = null)
    {
        var currentUser = new MembershipCurrentUser(me);
        var interrupt = new InterruptSessionCommandHandler(
            _sessions,
            new FakeMediator(_journal),
            currentUser,
            NullLogger<InterruptSessionCommandHandler>.Instance);

        return new LeaveOrStopGroupSessionCommandHandler(
            _profiles,
            groups ?? _groups,
            _sessions,
            new FakeMediator(_journal, interruptSession: interrupt),
            currentUser,
            new FixedTimeProvider(Now),
            NullLogger<LeaveOrStopGroupSessionCommandHandler>.Instance);
    }

    /// <summary>The group of <see cref="Host"/>, joined by <see cref="Participant"/>. The step ends at
    /// <paramref name="stepEndsAtUtc"/> (default: still running at <see cref="NowUtc"/>).</summary>
    private static GroupSession NewGroup(Guid? id = null, DateTime? stepEndsAtUtc = null)
    {
        var end = stepEndsAtUtc ?? NowUtc.AddMinutes(15);
        var group = GroupSession.Start(
            id ?? new Guid("10000000-0000-0000-0000-000000000001"), Host, end.AddMinutes(-25), end, Participant, end.AddMinutes(-20));
        Assert.True(group.IsSuccess);
        return group.Value;
    }

    private GroupSession GivenGroup(DateTime? stepEndsAtUtc = null, Guid? id = null)
    {
        var group = NewGroup(id, stepEndsAtUtc);
        _groups.Seed(group);
        return group;
    }

    private PomodoroSession GivenHostSprint()
    {
        var root = PomodoroSession.Create(PomodoroSessionType.Sprint, 25, Host);
        Assert.True(root.Start(NowUtc.AddMinutes(-10)).IsSuccess);
        _sessions.Sessions.Add(root);
        return root;
    }

    private PomodoroSession GivenParticipantSprint(GroupSession group)
    {
        var joined = PomodoroSession.CreateJoinedStep(
            PomodoroSessionType.Sprint, Participant, PomodoroSessionId.From(group.Id),
            group.CurrentStep.EndsAtUtc, group.CurrentStep.StartedAtUtc);
        Assert.True(joined.IsSuccess);
        Assert.True(joined.Value.Start(group.CurrentStep.StartedAtUtc).IsSuccess);
        _sessions.Sessions.Add(joined.Value);
        return joined.Value;
    }

    [Fact]
    public async Task Should_StopForEveryone_When_HostStopsDuringSprint()
    {
        _profiles.Seed(Host, "lea_dev");
        var hostSession = GivenHostSprint();
        var group = GivenGroup(id: hostSession.Id.Value);

        var result = await CreateSut(Host).Handle(new LeaveOrStopGroupSessionCommand());

        Assert.True(result.IsSuccess);
        Assert.True(result.Stopped);
        Assert.NotNull(group.EndedAtUtc);
        Assert.Equal(GroupSessionEndReason.Stopped, group.EndReason);
        Assert.Equal(PomodoroSessionStatus.Interrupted, hostSession.Status);
        var entry = Assert.Single(_journal.Entries);
        Assert.Equal(JournalEventType.SprintInterrupted, entry.EventType);
        Assert.Equal(hostSession.Id.Value, entry.ResourceId);
        Assert.Equal(1, _groups.UpdateCallCount);
    }

    [Fact]
    public async Task Should_Stop_When_HostWaitsWithoutSession()
    {
        _profiles.Seed(Host, "lea_dev");
        var group = GivenGroup(stepEndsAtUtc: NowUtc.AddMinutes(-2));

        var result = await CreateSut(Host).Handle(new LeaveOrStopGroupSessionCommand());

        Assert.True(result.IsSuccess);
        Assert.True(result.Stopped);
        Assert.Equal(GroupSessionEndReason.Stopped, group.EndReason);
        Assert.Empty(_journal.Entries);
    }

    [Fact]
    public async Task Should_RecordHostLeftCommunity_When_HostStopsBecauseLeavingCommunity()
    {
        _profiles.Seed(Host, "lea_dev");
        var root = GivenHostSprint();
        var group = GivenGroup(id: root.Id.Value);

        var result = await CreateSut(Host).Handle(new LeaveOrStopGroupSessionCommand(BecauseLeavingCommunity: true));

        Assert.True(result.IsSuccess);
        Assert.Equal(GroupSessionEndReason.HostLeftCommunity, group.EndReason);
    }

    [Fact]
    public async Task Should_LeaveAlone_When_ParticipantLeaves()
    {
        _profiles.Seed(Participant, "tomtom");
        var hostSession = GivenHostSprint();
        var group = GivenGroup(id: hostSession.Id.Value);
        var mySession = GivenParticipantSprint(group);

        var result = await CreateSut(Participant).Handle(new LeaveOrStopGroupSessionCommand());

        Assert.True(result.IsSuccess);
        Assert.False(result.Stopped);
        Assert.False(group.IsActiveMember(Participant));
        Assert.Null(group.EndedAtUtc);
        Assert.Equal(PomodoroSessionStatus.Interrupted, mySession.Status);
        Assert.Equal(PomodoroSessionStatus.Active, hostSession.Status);
        var entry = Assert.Single(_journal.Entries);
        Assert.Equal(JournalEventType.SprintInterrupted, entry.EventType);
        Assert.Equal(mySession.Id.Value, entry.ResourceId);
    }

    [Fact]
    public async Task Should_Leave_When_ParticipantWaits()
    {
        _profiles.Seed(Participant, "tomtom");
        var group = GivenGroup(stepEndsAtUtc: NowUtc.AddMinutes(-2));

        var result = await CreateSut(Participant).Handle(new LeaveOrStopGroupSessionCommand());

        Assert.True(result.IsSuccess);
        Assert.False(result.Stopped);
        Assert.False(group.IsActiveMember(Participant));
        Assert.Null(group.EndedAtUtc);
        Assert.Empty(_journal.Entries);
    }

    [Fact]
    public async Task Should_NotInterruptSession_When_ItIsOutsideTheGroup()
    {
        _profiles.Seed(Participant, "tomtom");
        GivenGroup();
        var own = PomodoroSession.Create(PomodoroSessionType.Sprint, 25, Participant);
        Assert.True(own.Start(NowUtc.AddMinutes(-5)).IsSuccess);
        _sessions.Sessions.Add(own);

        var result = await CreateSut(Participant).Handle(new LeaveOrStopGroupSessionCommand());

        Assert.True(result.IsSuccess);
        Assert.Equal(PomodoroSessionStatus.Active, own.Status);
        Assert.Empty(_journal.Entries);
    }

    [Fact]
    public async Task Should_ReturnNotInGroup_When_NoGroup()
    {
        _profiles.Seed(Other, "lea_dev");
        GivenGroup();

        var result = await CreateSut(Other).Handle(new LeaveOrStopGroupSessionCommand());

        Assert.False(result.IsSuccess);
        Assert.Equal(CommunityErrors.Community.NotInGroup, result.Error);
        Assert.Equal(0, _groups.UpdateCallCount);
    }

    [Fact]
    public async Task Should_ReturnNotInGroup_When_HostIsAbsentAndGroupEnds()
    {
        _profiles.Seed(Participant, "tomtom");
        var group = GivenGroup(stepEndsAtUtc: NowUtc.AddMinutes(-16));

        var result = await CreateSut(Participant).Handle(new LeaveOrStopGroupSessionCommand());

        Assert.False(result.IsSuccess);
        Assert.Equal(CommunityErrors.Community.NotInGroup, result.Error);
        Assert.Equal(GroupSessionEndReason.HostAbsent, group.EndReason);
    }

    [Fact]
    public async Task Should_ReturnNotMember_When_NoProfile()
    {
        GivenGroup();

        var result = await CreateSut(Participant).Handle(new LeaveOrStopGroupSessionCommand());

        Assert.False(result.IsSuccess);
        Assert.Equal(CommunityErrors.Community.NotMember, result.Error);
    }

    [Fact]
    public async Task Should_RetryOnce_When_VersionConflict()
    {
        _profiles.Seed(Host, "lea_dev");
        var root = GivenHostSprint();
        var group = GivenGroup(id: root.Id.Value);
        var scripted = new ConflictingGroupRepository(_groups, group.Id, results: [false, true]);

        var result = await CreateSut(Host, scripted).Handle(new LeaveOrStopGroupSessionCommand());

        Assert.True(result.IsSuccess);
        Assert.True(result.Stopped);
        Assert.Equal(2, scripted.UpdateCalls);
        Assert.Equal(2, scripted.LoadCalls);
        var reloaded = Assert.Single(_groups.Groups);
        Assert.NotSame(group, reloaded);
        Assert.Equal(GroupSessionEndReason.Stopped, reloaded.EndReason);
        Assert.Single(_journal.Entries);
    }

    [Fact]
    public async Task Should_ReturnGroupStepRunning_When_VersionConflictsTwice()
    {
        _profiles.Seed(Host, "lea_dev");
        var group = GivenGroup();
        var scripted = new ConflictingGroupRepository(_groups, group.Id, results: [false, false]);

        var result = await CreateSut(Host, scripted).Handle(new LeaveOrStopGroupSessionCommand());

        Assert.False(result.IsSuccess);
        Assert.Equal(CommunityErrors.Community.GroupStepRunning, result.Error);
        Assert.Equal(2, scripted.UpdateCalls);
    }

    /// <summary>Answers <see cref="IGroupSessionRepository.TryUpdateAsync"/> from a script. A rejected update
    /// detaches the aggregate like the real repository: the next load returns a fresh instance of the stored
    /// group (here, a new one with the same id and state).</summary>
    private sealed class ConflictingGroupRepository : IGroupSessionRepository
    {
        private readonly FakeGroupSessionRepository _inner;
        private readonly Guid _groupId;
        private readonly Queue<bool> _results;

        public ConflictingGroupRepository(FakeGroupSessionRepository inner, Guid groupId, IEnumerable<bool> results)
        {
            _inner = inner;
            _groupId = groupId;
            _results = new Queue<bool>(results);
        }

        public int UpdateCalls { get; private set; }
        public int LoadCalls { get; private set; }

        public Task<GroupSession?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            _inner.GetByIdAsync(id, ct);

        public Task<GroupSession?> FindLatestForUserAsync(UserId userId, CancellationToken ct = default)
        {
            LoadCalls++;
            return _inner.FindLatestForUserAsync(userId, ct);
        }

        public Task<bool> TryAddAsync(GroupSession group, CancellationToken ct = default) =>
            _inner.TryAddAsync(group, ct);

        public Task<bool> TryUpdateAsync(GroupSession group, CancellationToken ct = default)
        {
            UpdateCalls++;
            var accepted = _results.Dequeue();
            if (!accepted)
            {
                _inner.Groups.Clear();
                _inner.Groups.Add(NewGroup(_groupId));
            }

            return Task.FromResult(accepted);
        }

        public Task<bool> TryJoinAsync(GroupSession group, CancellationToken ct = default) =>
            _inner.TryJoinAsync(group, ct);
    }
}
