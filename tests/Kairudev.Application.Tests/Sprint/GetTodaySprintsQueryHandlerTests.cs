using Kairudev.Application.Sprint.Queries.GetTodaySprints;
using Kairudev.Application.Tests.Common;
using Kairudev.Domain.Identity;
using Kairudev.Domain.Sprint;
using Kairudev.Domain.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Kairudev.Application.Tests.Sprint;

public sealed class GetTodaySprintsQueryHandlerTests
{
    private static readonly UserId DefaultOwnerId = UserId.From(new Guid("00000000-0000-0000-0000-000000000001"));
    private static readonly UserId OtherOwnerId   = UserId.From(new Guid("00000000-0000-0000-0000-000000000002"));

    private static GetTodaySprintsQueryHandler BuildHandler(FakeSprintSessionRepository repo) =>
        new(repo, new FakeCurrentUserService(), NullLogger<GetTodaySprintsQueryHandler>.Instance);

    private static SprintSession BuildSession(
        UserId ownerId,
        DateTimeOffset startedAt,
        DateTimeOffset endedAt,
        SprintOutcome outcome = SprintOutcome.Completed)
    {
        var name = SprintName.Create("Focus", 1).Value;
        return SprintSession.Record(name, ownerId, startedAt, endedAt, outcome, null).Value;
    }

    [Fact]
    public async Task Should_ReturnEmptyList_When_NoSprintsToday()
    {
        var repo = new FakeSprintSessionRepository();
        var handler = BuildHandler(repo);

        var result = await handler.Handle(new GetTodaySprintsQuery());

        Assert.Empty(result.Sessions);
    }

    [Fact]
    public async Task Should_ReturnTodaySprints_When_SessionsExist()
    {
        var repo = new FakeSprintSessionRepository();
        var now = DateTimeOffset.UtcNow;
        var session = BuildSession(DefaultOwnerId, now.AddMinutes(-30), now);
        repo.Sessions.Add(session);

        var handler = BuildHandler(repo);
        var result = await handler.Handle(new GetTodaySprintsQuery());

        Assert.Single(result.Sessions);
    }

    [Fact]
    public async Task Should_NotReturnOtherUserSprints_When_MultipleUsersExist()
    {
        var repo = new FakeSprintSessionRepository();
        var now = DateTimeOffset.UtcNow;
        repo.Sessions.Add(BuildSession(DefaultOwnerId, now.AddMinutes(-30), now));
        repo.Sessions.Add(BuildSession(OtherOwnerId, now.AddMinutes(-60), now.AddMinutes(-30)));

        var handler = BuildHandler(repo);
        var result = await handler.Handle(new GetTodaySprintsQuery());

        Assert.Single(result.Sessions);
    }

    [Fact]
    public async Task Should_OrderSessionsChronologically_When_MultipleSessions()
    {
        var repo = new FakeSprintSessionRepository();
        var now = DateTimeOffset.UtcNow;
        var laterSession  = BuildSession(DefaultOwnerId, now.AddMinutes(-30), now);
        var earlierSession = BuildSession(DefaultOwnerId, now.AddMinutes(-90), now.AddMinutes(-60));
        repo.Sessions.Add(laterSession);
        repo.Sessions.Add(earlierSession);

        var handler = BuildHandler(repo);
        var result = await handler.Handle(new GetTodaySprintsQuery());

        Assert.Equal(2, result.Sessions.Count);
        Assert.True(result.Sessions[0].StartedAt < result.Sessions[1].StartedAt);
    }

    [Fact]
    public async Task Should_MapViewModelCorrectly_When_SessionHasLinkedTask()
    {
        var repo = new FakeSprintSessionRepository();
        var now = DateTimeOffset.UtcNow;
        var taskId = Guid.NewGuid();
        var name = SprintName.Create("Work session", 1).Value;
        var session = SprintSession.Record(name, DefaultOwnerId, now.AddMinutes(-25), now, SprintOutcome.Interrupted, TaskId.From(taskId)).Value;
        repo.Sessions.Add(session);

        var handler = BuildHandler(repo);
        var result = await handler.Handle(new GetTodaySprintsQuery());

        var vm = result.Sessions[0];
        Assert.Equal("Work session", vm.Name);
        Assert.Equal("Interrupted", vm.Outcome);
        Assert.Equal(taskId, vm.LinkedTaskId);
        Assert.Equal(25 * 60, vm.DurationSeconds, precision: 1);
    }
}
