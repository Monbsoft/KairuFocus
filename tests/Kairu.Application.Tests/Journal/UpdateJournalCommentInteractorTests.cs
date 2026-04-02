using Kairu.Application.Journal.Common;
using Kairu.Application.Journal.UpdateJournalComment;
using Kairu.Domain.Journal;

namespace Kairu.Application.Tests.Journal;

public sealed class UpdateJournalCommentInteractorTests
{
    private readonly FakeJournalEntryRepository _repository = new();
    private readonly FakeUpdateJournalCommentPresenter _presenter = new();
    private readonly UpdateJournalCommentInteractor _sut;

    public UpdateJournalCommentInteractorTests() =>
        _sut = new UpdateJournalCommentInteractor(_repository, _presenter);

    private (JournalEntry entry, JournalCommentId commentId) AddEntryWithComment()
    {
        var entry = JournalEntry.Create(JournalEventType.SprintStarted, Guid.NewGuid(), DateTime.UtcNow);
        var commentResult = entry.AddComment("Initial comment");
        _repository.Entries.Add(entry);
        return (entry, commentResult.Value);
    }

    [Fact]
    public async Task Should_PresentSuccess_When_EntryAndCommentExistAndTextIsValid()
    {
        var (entry, commentId) = AddEntryWithComment();

        await _sut.Execute(new UpdateJournalCommentRequest(entry.Id.Value, commentId.Value, "Updated comment"));

        Assert.True(_presenter.IsSuccess);
        Assert.NotNull(_presenter.Entry);
        Assert.Equal("Updated comment", _presenter.Entry.Comments[0].Text);
    }

    [Fact]
    public async Task Should_PresentNotFound_When_EntryDoesNotExist()
    {
        await _sut.Execute(new UpdateJournalCommentRequest(Guid.NewGuid(), Guid.NewGuid(), "text"));

        Assert.True(_presenter.IsNotFound);
    }

    [Fact]
    public async Task Should_PresentFailure_When_CommentDoesNotExist()
    {
        var entry = JournalEntry.Create(JournalEventType.SprintStarted, Guid.NewGuid(), DateTime.UtcNow);
        _repository.Entries.Add(entry);
        var unknownCommentId = Guid.NewGuid();

        await _sut.Execute(new UpdateJournalCommentRequest(entry.Id.Value, unknownCommentId, "text"));

        Assert.True(_presenter.IsFailure);
        Assert.Equal(DomainErrors.Journal.CommentNotFound, _presenter.FailureReason);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Should_PresentFailure_When_TextIsEmpty(string text)
    {
        var (entry, commentId) = AddEntryWithComment();

        await _sut.Execute(new UpdateJournalCommentRequest(entry.Id.Value, commentId.Value, text));

        Assert.True(_presenter.IsFailure);
        Assert.Equal(DomainErrors.Journal.EmptyComment, _presenter.FailureReason);
    }

    [Fact]
    public async Task Should_PresentFailure_When_TextIsTooLong()
    {
        var (entry, commentId) = AddEntryWithComment();
        var longText = new string('x', 1001);

        await _sut.Execute(new UpdateJournalCommentRequest(entry.Id.Value, commentId.Value, longText));

        Assert.True(_presenter.IsFailure);
        Assert.Equal(DomainErrors.Journal.CommentTooLong, _presenter.FailureReason);
    }

    private sealed class FakeUpdateJournalCommentPresenter : IUpdateJournalCommentPresenter
    {
        public JournalEntryViewModel? Entry { get; private set; }
        public bool IsSuccess { get; private set; }
        public bool IsNotFound { get; private set; }
        public bool IsFailure { get; private set; }
        public string? ValidationError { get; private set; }
        public string? FailureReason { get; private set; }

        public void PresentSuccess(JournalEntryViewModel entry) { Entry = entry; IsSuccess = true; }
        public void PresentNotFound() => IsNotFound = true;
        public void PresentValidationError(string error) { ValidationError = error; IsFailure = true; }
        public void PresentFailure(string reason) { FailureReason = reason; IsFailure = true; }
    }
}
