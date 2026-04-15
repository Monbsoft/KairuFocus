using KairuFocus.Application.Journal.Common;
using KairuFocus.Application.Journal.GetTodayJournal;
using KairuFocus.Domain.Journal;

namespace KairuFocus.Application.Tests.Journal;

public sealed class GetTodayJournalInteractorTests
{
    private readonly FakeJournalEntryRepository _repository = new();
    private readonly FakeGetTodayJournalPresenter _presenter = new();
    private readonly GetTodayJournalInteractor _sut;

    public GetTodayJournalInteractorTests() =>
        _sut = new GetTodayJournalInteractor(_repository, _presenter);

    [Fact]
    public async Task Should_PresentSuccess_When_TodayHasEntries()
    {
        _repository.Entries.Add(
            JournalEntry.Create(JournalEventType.SprintStarted, Guid.NewGuid(), DateTime.UtcNow));

        await _sut.Execute(new GetTodayJournalRequest());

        Assert.True(_presenter.IsSuccess);
        Assert.NotNull(_presenter.Entries);
        Assert.Single(_presenter.Entries);
    }

    [Fact]
    public async Task Should_PresentEmpty_When_NoEntriesToday()
    {
        await _sut.Execute(new GetTodayJournalRequest());

        Assert.False(_presenter.IsSuccess);
        Assert.True(_presenter.IsEmpty);
    }

    [Fact]
    public async Task Should_NotReturnYesterdayEntries_When_FilteringByToday()
    {
        var yesterday = DateTime.UtcNow.AddDays(-1);
        _repository.Entries.Add(
            JournalEntry.Create(JournalEventType.SprintCompleted, Guid.NewGuid(), yesterday));

        await _sut.Execute(new GetTodayJournalRequest());

        Assert.True(_presenter.IsEmpty);
    }

    private sealed class FakeGetTodayJournalPresenter : IGetTodayJournalPresenter
    {
        public IReadOnlyList<JournalEntryViewModel>? Entries { get; private set; }
        public bool IsSuccess { get; private set; }
        public bool IsEmpty { get; private set; }

        public void PresentSuccess(IReadOnlyList<JournalEntryViewModel> entries)
        {
            Entries = entries;
            IsSuccess = true;
        }

        public void PresentEmpty() => IsEmpty = true;
    }
}
