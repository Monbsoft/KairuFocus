using Kairudev.Domain.Identity;
using Kairudev.Domain.Tasks;
using Kairudev.IntegrationTests.Support;
using Reqnroll;

namespace Kairudev.IntegrationTests.StepDefinitions;

[Binding]
public class TaskSteps
{
    private readonly ScenarioContext _scenarioContext;
    private DatabaseContext DbContext => (DatabaseContext)_scenarioContext["DatabaseContext"];

    public TaskSteps(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    [Given("I have a fresh database")]
    public void GivenFreshDatabase()
    {
        DbContext.Reset();
    }

    [When("I create a task with title \"([^\"]*)\" and description \"([^\"]*)\"")]
    public void WhenCreateTask(string title, string description)
    {
        var userId = UserId.From("test-user");

        var task = DeveloperTask.Create(
            TaskTitle.Create(title).Value,
            TaskDescription.Create(description).Value,
            DateTime.UtcNow,
            userId
        );

        DbContext.DbContext.Tasks.Add(task);
        DbContext.DbContext.SaveChanges();

        _scenarioContext["CreatedTask"] = task;
    }

    [Then("the task should be created successfully")]
    public void ThenTaskCreated()
    {
        var task = (DeveloperTask)_scenarioContext["CreatedTask"];
        Assert.NotNull(task);
        Assert.NotEqual(default, task.Id.Value);
    }

    [Then("the task status should be \"([^\"]*)\"")]
    public void ThenTaskStatus(string status)
    {
        var task = (DeveloperTask)_scenarioContext["CreatedTask"];
        Assert.Equal(status, task.Status.ToString());
    }

    [Then("the task should have a creation date")]
    public void ThenTaskHasCreationDate()
    {
        var task = (DeveloperTask)_scenarioContext["CreatedTask"];
        Assert.NotEqual(default, task.CreatedAt);
    }

    [Given("I have a task \"([^\"]*)\" with status \"([^\"]*)\"")]
    public void GivenTaskWithStatus(string title, string status)
    {
        var userId = UserId.From("test-user");

        var task = DeveloperTask.Create(
            TaskTitle.Create(title).Value,
            null,
            DateTime.UtcNow,
            userId
        );

        DbContext.DbContext.Tasks.Add(task);
        DbContext.DbContext.SaveChanges();

        _scenarioContext["CurrentTask"] = task;
    }

    [When("I complete the task")]
    public void WhenCompleteTask()
    {
        var task = (DeveloperTask)_scenarioContext["CurrentTask"];
        var result = task.Complete();

        Assert.True(result.IsSuccess);

        DbContext.DbContext.Tasks.Update(task);
        DbContext.DbContext.SaveChanges();

        _scenarioContext["CurrentTask"] = task;
    }

    [Then("the task status should be \"([^\"]*)\"")]
    public void ThenCurrentTaskStatus(string status)
    {
        var task = (DeveloperTask)_scenarioContext["CurrentTask"];
        Assert.Equal(status, task.Status.ToString());
    }

    [Then("the task should have a completion date")]
    public void ThenTaskHasCompletionDate()
    {
        var task = (DeveloperTask)_scenarioContext["CurrentTask"];
        Assert.NotNull(task.CompletedAt);
    }

    [When("I link it to Jira ticket \"([^\"]*)\"")]
    public void WhenLinkToJira(string jiraKey)
    {
        var task = (DeveloperTask)_scenarioContext["CurrentTask"];
        task.LinkJiraTicket(JiraTicketKey.Create(jiraKey).Value);

        DbContext.DbContext.Tasks.Update(task);
        DbContext.DbContext.SaveChanges();

        _scenarioContext["CurrentTask"] = task;
    }

    [Then("the task should be linked to Jira ticket \"([^\"]*)\"")]
    public void ThenTaskLinkedToJira(string jiraKey)
    {
        var task = (DeveloperTask)_scenarioContext["CurrentTask"];
        Assert.NotNull(task.JiraTicketKey);
        Assert.Equal(jiraKey, task.JiraTicketKey.Value);
    }

    [Given("I have a task \"([^\"]*)\"")]
    public void GivenTask(string title)
    {
        var userId = UserId.From("test-user");

        var task = DeveloperTask.Create(
            TaskTitle.Create(title).Value,
            null,
            DateTime.UtcNow,
            userId
        );

        DbContext.DbContext.Tasks.Add(task);
        DbContext.DbContext.SaveChanges();

        _scenarioContext["CurrentTask"] = task;
    }

    [Given("I have the following tasks:")]
    public void GivenTasks(DataTable table)
    {
        var userId = UserId.From("test-user");
        var tasks = new List<DeveloperTask>();

        foreach (var row in table.Rows)
        {
            var title = row["Title"];
            var status = row["Status"];

            var task = DeveloperTask.Create(
                TaskTitle.Create(title).Value,
                null,
                DateTime.UtcNow,
                userId
            );

            tasks.Add(task);
        }

        DbContext.DbContext.Tasks.AddRange(tasks);
        DbContext.DbContext.SaveChanges();

        _scenarioContext["AllTasks"] = tasks;
    }

    [When("I retrieve all tasks")]
    public void WhenRetrieveAllTasks()
    {
        var tasks = DbContext.DbContext.Tasks.ToList();
        _scenarioContext["RetrievedTasks"] = tasks;
    }

    [Then("I should have (\\d+) tasks")]
    public void ThenTaskCount(int count)
    {
        var tasks = (List<DeveloperTask>)_scenarioContext["RetrievedTasks"];
        Assert.Equal(count, tasks.Count);
    }

    [Then("the tasks should be in the correct order")]
    public void ThenTasksInCorrectOrder()
    {
        var tasks = (List<DeveloperTask>)_scenarioContext["RetrievedTasks"];
        // Tasks should be ordered by creation date
        for (int i = 1; i < tasks.Count; i++)
        {
            Assert.True(tasks[i - 1].CreatedAt <= tasks[i].CreatedAt);
        }
    }
}
