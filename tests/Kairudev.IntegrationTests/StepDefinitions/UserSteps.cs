using Kairudev.Domain.Identity;
using Kairudev.Domain.Settings;
using Kairudev.IntegrationTests.Support;
using Reqnroll;

namespace Kairudev.IntegrationTests.StepDefinitions;

[Binding]
public class UserSteps
{
    private readonly ScenarioContext _scenarioContext;
    private DatabaseContext DbContext => (DatabaseContext)_scenarioContext["DatabaseContext"];

    public UserSteps(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    [When("I create a user with GitHub ID \"([^\"]*)\" and login \"([^\"]*)\"")]
    public void WhenCreateUser(string githubId, string login)
    {
        var user = User.Create(
            githubId,
            login,
            login,
            null
        );

        DbContext.DbContext.Users.Add(user);
        DbContext.DbContext.SaveChanges();

        _scenarioContext["CreatedUser"] = user;
    }

    [Then("the user should be created successfully")]
    public void ThenUserCreated()
    {
        var user = (User)_scenarioContext["CreatedUser"];
        Assert.NotNull(user);
        Assert.NotEmpty(user.Id.Value);
    }

    [Then("the user should have display name \"([^\"]*)\"")]
    public void ThenUserDisplayName(string displayName)
    {
        var user = (User)_scenarioContext["CreatedUser"];
        Assert.Equal(displayName, user.DisplayName);
    }

    [Given("I have a user")]
    public void GivenUser()
    {
        var user = User.Create(
            "github-123",
            "test-user",
            "Test User",
            null
        );

        DbContext.DbContext.Users.Add(user);
        DbContext.DbContext.SaveChanges();

        _scenarioContext["CurrentUser"] = user;
    }

    [When("I update user settings with theme \"([^\"]*)\" and ringtone \"([^\"]*)\"")]
    public void WhenUpdateUserSettings(string theme, string ringtone)
    {
        var user = (User)_scenarioContext["CurrentUser"];
        var userId = user.Id;

        var settings = UserSettings.CreateDefault(userId);
        settings.UpdateThemePreference(theme == "dark" ? ThemePreference.Dark : ThemePreference.Light);
        settings.UpdateRingtonePreference(ringtone == "chime" ? RingtonePreference.Bird : RingtonePreference.AlarmClock);

        DbContext.DbContext.UserSettings.Add(settings);
        DbContext.DbContext.SaveChanges();

        _scenarioContext["CurrentSettings"] = settings;
    }

    [Then("the settings should be saved")]
    public void ThenSettingsSaved()
    {
        var settings = (UserSettings)_scenarioContext["CurrentSettings"];
        Assert.NotNull(settings);
        Assert.NotEmpty(settings.Id.Value);
    }

    [Then("the theme should be \"([^\"]*)\"")]
    public void ThenThemeIs(string theme)
    {
        var settings = (UserSettings)_scenarioContext["CurrentSettings"];
        var expectedTheme = theme == "dark" ? ThemePreference.Dark : ThemePreference.Light;
        Assert.Equal(expectedTheme, settings.ThemePreference);
    }

    [Then("the ringtone should be \"([^\"]*)\"")]
    public void ThenRingtoneIs(string ringtone)
    {
        var settings = (UserSettings)_scenarioContext["CurrentSettings"];
        var expectedRingtone = ringtone == "chime" ? RingtonePreference.Bird : RingtonePreference.AlarmClock;
        Assert.Equal(expectedRingtone, settings.RingtonePreference);
    }

    [When("I configure Jira settings with URL and API token")]
    public void WhenConfigureJira()
    {
        var settings = (UserSettings)_scenarioContext["CurrentSettings"];

        settings.UpdateJiraSettings(
            "https://jira.example.com",
            "user@example.com",
            "api-token-123"
        );

        DbContext.DbContext.UserSettings.Update(settings);
        DbContext.DbContext.SaveChanges();

        _scenarioContext["CurrentSettings"] = settings;
    }

    [Then("the Jira settings should be saved")]
    public void ThenJiraSettingsSaved()
    {
        var settings = (UserSettings)_scenarioContext["CurrentSettings"];
        Assert.NotNull(settings.JiraBaseUrl);
        Assert.NotNull(settings.JiraEmail);
        Assert.NotNull(settings.JiraApiToken);
    }

    [Then("the user should be able to access Jira integration")]
    public void ThenCanAccessJira()
    {
        var settings = (UserSettings)_scenarioContext["CurrentSettings"];
        Assert.True(!string.IsNullOrEmpty(settings.JiraBaseUrl));
    }
}
