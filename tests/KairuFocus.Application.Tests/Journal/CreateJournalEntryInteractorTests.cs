using KairuFocus.Application.Journal.Common;
using KairuFocus.Application.Journal.CreateJournalEntry;
using KairuFocus.Domain.Journal;

namespace KairuFocus.Application.Tests.Journal;

public sealed class CreateJournalEntryInteractorTests
{
    private readonly FakeJournalEntryRepository _repository = new();
    private readonly FakeCreateJournalEntryPresenter _presenter = new();
    private readonly CreateJournalEntryInteractor _sut;

    public CreateJournalEntryInteractorTests() =>
        _sut = new CreateJournalEntryInteractor(_repository, _presenter);

    [Fact]
    public async Task Should_PresentSuccess_When_RequestIsValid()
    {
        var resourceId = Guid.NewGuid();
        var occurredAt = DateTime.UtcNow;

        await _sut.Execute(new CreateJournalEntryRequest(
            JournalEventType.SprintStarted, resourceId, occurredAt));

        Assert.True(_presenter.IsSuccess);
        Assert.NotNull(_presenter.Entry);
        Assert.Equal(resourceId, _presenter.Entry.ResourceId);
        Assert.Equal(nameof(JournalEventType.SprintStarted), _presenter.Entry.EventType);
        Assert.Equal(occurredAt, _presenter.Entry.OccurredAt);
    }

    [Fact]
    public async Task Should_PersistEntry_When_RequestIsValid()
    {
        await _sut.Execute(new CreateJournalEntryRequest(
            JournalEventType.TaskCompleted, Guid.NewGuid(), DateTime.UtcNow));

        Assert.Single(_repository.Entries);
    }

    private sealed class FakeCreateJournalEntryPresenter : ICreateJournalEntryPresenter
    {
        public JournalEntryViewModel? Entry { get; private set; }
        public bool IsSuccess { get; private set; }
        public string? FailureReason { get; private set; }

        public void PresentSuccess(JournalEntryViewModel entry) { Entry = entry; IsSuccess = true; }
        public void PresentFailure(string reason) => FailureReason = reason;
    }
}
