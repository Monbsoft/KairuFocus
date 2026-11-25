using KairuFocus.Web.Helpers;
using KairuFocus.Web.Services;

namespace KairuFocus.Web.Tests.Helpers;

public sealed class TodayJournalTests
{
    private static readonly DateTime D = new(2026, 11, 25, 10, 0, 0, DateTimeKind.Utc);

    private static JournalEntryDto Entry(string type, int minutes, int? seq = null) =>
        new(Guid.NewGuid(), D.AddMinutes(minutes), type, Guid.NewGuid(), seq, [], [], []);

    [Fact]
    public void Should_TargetOlderEntry_When_LatestEntriesRefuseNotes()
    {
        var sprintStarted = Entry("SprintStarted", 0, 3);
        var day = new[]
        {
            sprintStarted, Entry("SprintCompleted", 25, 3), Entry("BreakStarted", 25), Entry("BreakCompleted", 30),
            Entry("BreakInterrupted", 31),
        };

        Assert.Equal(sprintStarted.Id, TodayJournal.NoteTarget(day)!.Id);
        Assert.DoesNotContain(TodayJournal.Latest(day), e => e.Id == sprintStarted.Id);
    }

    [Fact]
    public void Should_ReturnNoTarget_When_NoEntryAcceptsNotes() =>
        Assert.Null(TodayJournal.NoteTarget([Entry("BreakStarted", 0), Entry("BreakCompleted", 5)]));

    [Fact]
    public void Should_KeepFourMostRecent_When_MoreEntries()
    {
        var day = Enumerable.Range(0, 6).Select(i => Entry("TaskStarted", i)).ToList();

        var latest = TodayJournal.Latest(day);

        Assert.Equal(4, latest.Count);
        Assert.Equal(day[5].Id, latest[0].Id);
        Assert.Equal(day[2].Id, latest[3].Id);
    }
}
