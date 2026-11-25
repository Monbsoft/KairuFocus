using KairuFocus.Web.Helpers;
using KairuFocus.Web.Services;

namespace KairuFocus.Web.Tests.Helpers;

public sealed class JournalEntryMetaTests
{
    private static JournalEntryDto Entry(string type, int? sequence = null) =>
        new(Guid.NewGuid(), DateTime.UtcNow, type, Guid.NewGuid(), sequence, [], [], []);

    [Fact]
    public void Should_UseSequenceKey_When_SprintHasSequence()
    {
        var label = JournalEntryMeta.GetLabelKey(Entry("SprintStarted", 3))!.Value;

        Assert.Equal("Journal.Entry.SprintStarted.Seq", label.Key);
        Assert.Equal(new object[] { 3 }, label.Args);
    }

    [Fact]
    public void Should_UsePlainKey_When_BreakHasNoSequence()
    {
        var label = JournalEntryMeta.GetLabelKey(Entry("BreakCompleted"))!.Value;

        Assert.Equal("Journal.Entry.BreakCompleted", label.Key);
        Assert.Empty(label.Args);
    }

    [Fact]
    public void Should_UsePlainKey_When_TaskEventHasSequence() =>
        Assert.Equal("Journal.Entry.TaskStarted", JournalEntryMeta.GetLabelKey(Entry("TaskStarted", 2))!.Value.Key);

    [Fact]
    public void Should_ReturnNoKey_When_EventTypeUnknown() =>
        Assert.Null(JournalEntryMeta.GetLabelKey(Entry("Whatever")));
}
