using Kairudev.Domain.Identity;
using Kairudev.Domain.Tasks;

namespace Kairudev.Domain.Tests.Tasks;

public sealed class DeveloperTaskTagTests
{
    private static readonly UserId OwnerId = UserId.New();

    private static DeveloperTask CreateValidTask() =>
        DeveloperTask.Create(TaskTitle.Create("Fix auth bug").Value, null, DateTime.UtcNow, OwnerId);

    private static TaskTag Tag(string value) => TaskTag.Create(value).Value;

    [Fact]
    public void Should_CreateTaskWithoutTags_When_NoTagsProvided()
    {
        var task = CreateValidTask();

        Assert.Empty(task.Tags);
    }

    [Fact]
    public void Should_CreateTaskWithTags_When_TagsProvided()
    {
        var tags = new[] { Tag("bug"), Tag("urgent") };

        var task = DeveloperTask.Create(
            TaskTitle.Create("Fix login").Value, null, DateTime.UtcNow, OwnerId, tags);

        Assert.Equal(2, task.Tags.Count);
        Assert.Contains(task.Tags, t => t.Value == "bug");
        Assert.Contains(task.Tags, t => t.Value == "urgent");
    }

    [Fact]
    public void Should_SetTags_When_ValidTags()
    {
        var task = CreateValidTask();
        var tags = new List<TaskTag> { Tag("backend"), Tag("fix") };

        var result = task.SetTags(tags);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, task.Tags.Count);
    }

    [Fact]
    public void Should_ReplacePreviousTags_When_SetTagsCalled()
    {
        var task = CreateValidTask();
        task.SetTags([Tag("old")]);

        var result = task.SetTags([Tag("new1"), Tag("new2")]);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, task.Tags.Count);
        Assert.DoesNotContain(task.Tags, t => t.Value == "old");
    }

    [Fact]
    public void Should_ClearTags_When_SetTagsCalledWithEmptyList()
    {
        var task = CreateValidTask();
        task.SetTags([Tag("existing")]);

        var result = task.SetTags([]);

        Assert.True(result.IsSuccess);
        Assert.Empty(task.Tags);
    }

    [Fact]
    public void Should_FailSetTags_When_TooManyTags()
    {
        var task = CreateValidTask();
        var tags = Enumerable.Range(1, DeveloperTask.MaxTags + 1)
            .Select(i => Tag($"tag{i}"))
            .ToList();

        var result = task.SetTags(tags);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Tasks.TooManyTags, result.Error);
    }

    [Fact]
    public void Should_FailSetTags_When_DuplicateTag()
    {
        var task = CreateValidTask();
        var tags = new List<TaskTag> { Tag("bug"), Tag("Bug") };

        var result = task.SetTags(tags);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Tasks.DuplicateTag, result.Error);
    }
}
