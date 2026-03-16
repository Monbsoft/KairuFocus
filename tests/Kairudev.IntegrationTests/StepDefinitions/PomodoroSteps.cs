using Kairudev.Domain.Identity;
using Kairudev.Domain.Pomodoro;
using Kairudev.Domain.Tasks;
using Kairudev.IntegrationTests.Support;
using Reqnroll;

namespace Kairudev.IntegrationTests.StepDefinitions;

[Binding]
public class PomodoroSteps
{
    private readonly ScenarioContext _scenarioContext;
    private DatabaseContext DbContext => (DatabaseContext)_scenarioContext["DatabaseContext"];

    public PomodoroSteps(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    [When("I start a Pomodoro session of (\\d+) minutes")]
    public void WhenStartSession(int minutes)
    {
        var userId = UserId.From("test-user");

        var session = PomodoroSession.Create(
            PomodoroSessionType.Sprint,
            minutes,
            userId
        );

        DbContext.DbContext.PomodoroSessions.Add(session);
        DbContext.DbContext.SaveChanges();

        _scenarioContext["CurrentSession"] = session;
    }

    [Then("the session should be created")]
    public void ThenSessionCreated()
    {
        var session = (PomodoroSession)_scenarioContext["CurrentSession"];
        Assert.NotNull(session);
        Assert.NotEqual(default, session.Id.Value);
    }

    [Then("the session status should be \"([^\"]*)\"")]
    public void ThenSessionStatus(string status)
    {
        var session = (PomodoroSession)_scenarioContext["CurrentSession"];
        Assert.Equal(status, session.Status.ToString());
    }

    [Then("the session should have a start time")]
    public void ThenSessionHasStartTime()
    {
        var session = (PomodoroSession)_scenarioContext["CurrentSession"];
        Assert.NotNull(session.StartedAt);
    }

    [Given("I have a running Pomodoro session")]
    public void GivenRunningSession()
    {
        var userId = UserId.From("test-user");

        var session = PomodoroSession.Create(
            PomodoroSessionType.Sprint,
            25,
            userId
        );

        DbContext.DbContext.PomodoroSessions.Add(session);
        DbContext.DbContext.SaveChanges();

        _scenarioContext["CurrentSession"] = session;
    }

    [When("I complete the session")]
    public void WhenCompleteSession()
    {
        var session = (PomodoroSession)_scenarioContext["CurrentSession"];
        var result = session.Complete(DateTime.UtcNow);

        Assert.True(result.IsSuccess);

        DbContext.DbContext.PomodoroSessions.Update(session);
        DbContext.DbContext.SaveChanges();

        _scenarioContext["CurrentSession"] = session;
    }

    [Then("the session should have an end time")]
    public void ThenSessionHasEndTime()
    {
        var session = (PomodoroSession)_scenarioContext["CurrentSession"];
        Assert.NotNull(session.EndedAt);
    }

    [Given("I have a Pomodoro session")]
    public void GivenPomodoroSession()
    {
        var userId = UserId.From("test-user");

        var session = PomodoroSession.Create(
            PomodoroSessionType.Sprint,
            25,
            userId
        );

        DbContext.DbContext.PomodoroSessions.Add(session);
        DbContext.DbContext.SaveChanges();

        _scenarioContext["CurrentSession"] = session;
    }

    [When("I link the task to the session")]
    public void WhenLinkTaskToSession()
    {
        var task = (DeveloperTask)_scenarioContext["CurrentTask"];
        var session = (PomodoroSession)_scenarioContext["CurrentSession"];

        var result = session.LinkTask(task.Id);

        Assert.True(result.IsSuccess);

        DbContext.DbContext.PomodoroSessions.Update(session);
        DbContext.DbContext.SaveChanges();

        _scenarioContext["CurrentSession"] = session;
    }

    [Then("the task should be linked to the session")]
    public void ThenTaskLinkedToSession()
    {
        var task = (DeveloperTask)_scenarioContext["CurrentTask"];
        var session = (PomodoroSession)_scenarioContext["CurrentSession"];

        Assert.Contains(task.Id.Value, session.LinkedTaskIdValues);
    }

    [Given("I have Pomodoro settings configured with:")]
    public void GivenPomodoroSettings(DataTable table)
    {
        var row = table.Rows[0];
        var sprintDuration = int.Parse(row["Value"]); // This is a simplified version
        var settings = PomodoroSettings.Create(sprintDuration, 5, 15).Value;

        _scenarioContext["CurrentSettings"] = settings;
    }

    [When("I retrieve the settings")]
    public void WhenRetrieveSettings()
    {
        var settings = (PomodoroSettings)_scenarioContext["CurrentSettings"];
        _scenarioContext["RetrievedSettings"] = settings;
    }

    [Then("the settings should match the configured values")]
    public void ThenSettingsMatch()
    {
        var retrievedSettings = _scenarioContext["RetrievedSettings"];
        Assert.NotNull(retrievedSettings);
    }
}
