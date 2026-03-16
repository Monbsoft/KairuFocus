using Kairudev.Domain.Identity;
using Kairudev.Domain.Journal;
using Kairudev.IntegrationTests.Support;
using Reqnroll;

namespace Kairudev.IntegrationTests.StepDefinitions;

[Binding]
public class JournalSteps
{
    private readonly ScenarioContext _scenarioContext;
    private DatabaseContext DbContext => (DatabaseContext)_scenarioContext["DatabaseContext"];

    public JournalSteps(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    [When("I create a journal entry with event type \"([^\"]*)\" for resource \"([^\"]*)\"")]
    public void WhenCreateJournalEntry(string eventType, string resourceId)
    {
        var userId = UserId.From("test-user");

        var entry = JournalEntry.Create(
            Enum.Parse<JournalEventType>(eventType),
            Guid.Parse(resourceId),
            DateTime.UtcNow,
            userId
        );

        DbContext.DbContext.JournalEntries.Add(entry);
        DbContext.DbContext.SaveChanges();

        _scenarioContext["CreatedEntry"] = entry;
    }

    [Then("the entry should be created successfully")]
    public void ThenEntryCreated()
    {
        var entry = (JournalEntry)_scenarioContext["CreatedEntry"];
        Assert.NotNull(entry);
        Assert.NotEqual(default, entry.Id.Value);
    }

    [Then("the entry should have a timestamp")]
    public void ThenEntryHasTimestamp()
    {
        var entry = (JournalEntry)_scenarioContext["CreatedEntry"];
        Assert.NotEqual(default, entry.OccurredAt);
    }

    [Then("the entry should have a sequence number")]
    public void ThenEntryHasSequence()
    {
        var entry = (JournalEntry)_scenarioContext["CreatedEntry"];
        Assert.NotNull(entry.Sequence);
    }

    [Given("I have a journal entry")]
    public void GivenJournalEntry()
    {
        var userId = UserId.From("test-user");

        var entry = JournalEntry.Create(
            JournalEventType.TaskCompleted,
            Guid.NewGuid(),
            DateTime.UtcNow,
            userId
        );

        DbContext.DbContext.JournalEntries.Add(entry);
        DbContext.DbContext.SaveChanges();

        _scenarioContext["CurrentEntry"] = entry;
    }

    [When("I add a comment \"([^\"]*)\"")]
    public void WhenAddComment(string text)
    {
        var entry = (JournalEntry)_scenarioContext["CurrentEntry"];

        var result = entry.AddComment(text);
        Assert.True(result.IsSuccess);

        DbContext.DbContext.JournalEntries.Update(entry);
        DbContext.DbContext.SaveChanges();

        _scenarioContext["CurrentEntry"] = entry;
    }

    [Then("the comment should be linked to the entry")]
    public void ThenCommentLinked()
    {
        var entry = (JournalEntry)_scenarioContext["CurrentEntry"];
        Assert.NotEmpty(entry.Comments);
    }

    [Then("the entry should have (\\d+) comment")]
    public void ThenEntryHasComments(int count)
    {
        var entry = (JournalEntry)_scenarioContext["CurrentEntry"];
        Assert.Equal(count, entry.Comments.Count);
    }

    [Given("I have the following journal entries:")]
    public void GivenJournalEntries(DataTable table)
    {
        var userId = UserId.From("test-user");
        var entries = new List<JournalEntry>();

        foreach (var row in table.Rows)
        {
            var eventType = Enum.Parse<JournalEventType>(row["EventType"]);
            var count = int.Parse(row["Count"]);

            for (int i = 0; i < count; i++)
            {
                var entry = JournalEntry.Create(
                    eventType,
                    Guid.NewGuid(),
                    DateTime.UtcNow,
                    userId
                );

                entries.Add(entry);
            }
        }

        DbContext.DbContext.JournalEntries.AddRange(entries);
        DbContext.DbContext.SaveChanges();

        _scenarioContext["AllEntries"] = entries;
    }

    [When("I retrieve all journal entries")]
    public void WhenRetrieveJournalEntries()
    {
        var entries = DbContext.DbContext.JournalEntries
            .OrderBy(e => e.OccurredAt)
            .ToList();

        _scenarioContext["RetrievedEntries"] = entries;
    }

    [Then("I should have (\\d+) entries in chronological order")]
    public void ThenEntriesInOrder(int count)
    {
        var entries = (List<JournalEntry>)_scenarioContext["RetrievedEntries"];
        Assert.Equal(count, entries.Count);

        for (int i = 1; i < entries.Count; i++)
        {
            Assert.True(entries[i - 1].OccurredAt <= entries[i].OccurredAt);
        }
    }
}
