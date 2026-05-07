using KairuFocus.Api.Mcp;
using KairuFocus.Api.Tests.Auth;
using KairuFocus.Application.Tasks.Commands.AddTask;
using KairuFocus.Application.Tasks.Common;

namespace KairuFocus.Api.Tests.Mcp;

public sealed class KairuFocusMcpToolsTests
{
    private static TaskViewModel SampleTask(List<string>? tags = null) =>
        new(
            Guid.NewGuid(),
            "Title",
            "Description",
            "Pending",
            DateTime.UtcNow,
            CompletedAt: null,
            JiraTicketKey: null,
            Tags: tags ?? new List<string>());

    [Fact]
    public async Task Should_pass_tags_to_AddTaskCommand_When_create_task_called_with_tags()
    {
        AddTaskCommand? captured = null;
        var mediator = new TestMediator
        {
            DispatchResult = cmd =>
            {
                captured = (AddTaskCommand)cmd;
                return AddTaskResult.Success(SampleTask(captured.Tags));
            }
        };
        var tools = new KairuFocusMcpTools(mediator);

        var json = await tools.create_task(
            title: "Fix bug",
            description: "Critical",
            tags: new[] { "PROJ-123", "backend" });

        Assert.NotNull(captured);
        Assert.Equal("Fix bug", captured!.Title);
        Assert.Equal("Critical", captured.Description);
        Assert.NotNull(captured.Tags);
        Assert.Equal(new[] { "PROJ-123", "backend" }, captured.Tags!);
        Assert.Contains("PROJ-123", json);
    }
}
