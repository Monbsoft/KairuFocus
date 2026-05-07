using KairuFocus.Api.Mcp;
using KairuFocus.Api.Tests.Auth;
using KairuFocus.Application.Tasks.Commands.AddTask;
using KairuFocus.Application.Tasks.Commands.UpdateTask;
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

    [Fact]
    public async Task Should_return_error_When_update_task_called_with_invalid_guid()
    {
        var mediator = new TestMediator(); // pas de DispatchResult — ne devrait pas être appelé
        var tools = new KairuFocusMcpTools(mediator);

        var result = await tools.update_task(
            taskId: "not-a-guid",
            title: "Whatever");

        Assert.StartsWith("Error", result);
        Assert.Contains("GUID", result);
    }

    [Fact]
    public async Task Should_return_NotFound_When_update_task_called_with_unknown_id()
    {
        var mediator = new TestMediator
        {
            DispatchResult = _ => UpdateTaskResult.NotFound()
        };
        var tools = new KairuFocusMcpTools(mediator);

        var taskId = Guid.NewGuid().ToString();
        var result = await tools.update_task(
            taskId: taskId,
            title: "Whatever");

        Assert.Contains("not found", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(taskId, result);
    }

    [Fact]
    public async Task Should_pass_tags_to_UpdateTaskCommand_When_update_task_called_with_tags()
    {
        UpdateTaskCommand? captured = null;
        var mediator = new TestMediator
        {
            DispatchResult = cmd =>
            {
                captured = (UpdateTaskCommand)cmd;
                return UpdateTaskResult.Success(SampleTask(captured.Tags));
            }
        };
        var tools = new KairuFocusMcpTools(mediator);

        var taskId = Guid.NewGuid();
        var json = await tools.update_task(
            taskId: taskId.ToString(),
            title: "Renamed",
            description: "New desc",
            tags: new[] { "PROJ-9", "frontend" });

        Assert.NotNull(captured);
        Assert.Equal(taskId, captured!.TaskId);
        Assert.Equal("Renamed", captured.Title);
        Assert.Equal("New desc", captured.Description);
        Assert.NotNull(captured.Tags);
        Assert.Equal(new[] { "PROJ-9", "frontend" }, captured.Tags!);
        Assert.Contains("PROJ-9", json);
    }

    [Fact]
    public async Task Should_pass_null_tags_When_update_task_called_without_tags()
    {
        UpdateTaskCommand? captured = null;
        var mediator = new TestMediator
        {
            DispatchResult = cmd =>
            {
                captured = (UpdateTaskCommand)cmd;
                return UpdateTaskResult.Success(SampleTask());
            }
        };
        var tools = new KairuFocusMcpTools(mediator);

        await tools.update_task(
            taskId: Guid.NewGuid().ToString(),
            title: "Renamed");

        Assert.NotNull(captured);
        Assert.Null(captured!.Tags);
    }
}
