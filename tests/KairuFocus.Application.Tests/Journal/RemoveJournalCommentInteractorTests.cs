using KairuFocus.Application.Journal.RemoveJournalComment;
using KairuFocus.Domain.Journal;

namespace KairuFocus.Application.Tests.Journal;

public sealed class RemoveJournalCommentInteractorTests
{
    private readonly FakeJournalEntryRepository _repository = new();
    private readonly FakeRemoveJournalCommentPresenter _presenter = new();
    private readonly RemoveJournalCommentInteractor _sut;

    public RemoveJournalCommentInteractorTests() =>
        _sut = new RemoveJournalCommentInteractor(_repository, _presenter);

    private (JournalEntry entry, JournalCommentId commentId) AddEntryWithComment()
    {
        var entry = JournalEntry.Create(JournalEventType.SprintStarted, Guid.NewGuid(), DateTime.UtcNow);
        var commentResult = entry.AddComment("A comment");
        _repository.Entries.Add(entry);
        return (entry, commentResult.Value);
    }

    [Fact]
    public async Task Should_PresentSuccess_When_EntryAndCommentExist()
    {
        var (entry, commentId) = AddEntryWithComment();

        await _sut.Execute(new RemoveJournalCommentRequest(entry.Id.Value, commentId.Value));

        Assert.True(_presenter.IsSuccess);
        Assert.Empty(entry.Comments);
    }

    [Fact]
    public async Task Should_PresentNotFound_When_EntryDoesNotExist()
    {
        await _sut.Execute(new RemoveJournalCommentRequest(Guid.NewGuid(), Guid.NewGuid()));

        Assert.True(_presenter.IsNotFound);
    }

    [Fact]
    public async Task Should_PresentFailure_When_CommentDoesNotExist()
    {
        var entry = JournalEntry.Create(JournalEventType.SprintStarted, Guid.NewGuid(), DateTime.UtcNow);
        _repository.Entries.Add(entry);
        var unknownCommentId = Guid.NewGuid();

        await _sut.Execute(new RemoveJournalCommentRequest(entry.Id.Value, unknownCommentId));

        Assert.True(_presenter.IsFailure);
        Assert.Equal(DomainErrors.Journal.CommentNotFound, _presenter.FailureReason);
    }

    private sealed class FakeRemoveJournalCommentPresenter : IRemoveJournalCommentPresenter
    {
        public bool IsSuccess { get; private set; }
        public bool IsNotFound { get; private set; }
        public bool IsFailure { get; private set; }
        public string? FailureReason { get; private set; }

        public void PresentSuccess() => IsSuccess = true;
        public void PresentNotFound() => IsNotFound = true;
        public void PresentFailure(string reason) { FailureReason = reason; IsFailure = true; }
    }
}
