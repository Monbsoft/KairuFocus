using KairuFocus.Application.Journal.AddJournalComment;
using KairuFocus.Application.Journal.Common;
using KairuFocus.Domain.Journal;

namespace KairuFocus.Application.Tests.Journal;

public sealed class AddJournalCommentInteractorTests
{
    private readonly FakeJournalEntryRepository _repository = new();
    private readonly FakeAddJournalCommentPresenter _presenter = new();
    private readonly AddJournalCommentInteractor _sut;

    public AddJournalCommentInteractorTests() =>
        _sut = new AddJournalCommentInteractor(_repository, _presenter);

    private JournalEntry AddEntryToRepository()
    {
        var entry = JournalEntry.Create(JournalEventType.SprintStarted, Guid.NewGuid(), DateTime.UtcNow);
        _repository.Entries.Add(entry);
        return entry;
    }

    [Fact]
    public async Task Should_PresentSuccess_When_EntryExistsAndTextIsValid()
    {
        var entry = AddEntryToRepository();

        await _sut.Execute(new AddJournalCommentRequest(entry.Id.Value, "Great sprint!"));

        Assert.True(_presenter.IsSuccess);
        Assert.NotNull(_presenter.Entry);
        Assert.Single(_presenter.Entry.Comments);
        Assert.Equal("Great sprint!", _presenter.Entry.Comments[0].Text);
    }

    [Fact]
    public async Task Should_PresentNotFound_When_EntryDoesNotExist()
    {
        await _sut.Execute(new AddJournalCommentRequest(Guid.NewGuid(), "comment"));

        Assert.True(_presenter.IsNotFound);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Should_PresentValidationError_When_CommentTextIsEmpty(string text)
    {
        var entry = AddEntryToRepository();

        await _sut.Execute(new AddJournalCommentRequest(entry.Id.Value, text));

        Assert.True(_presenter.IsValidationError);
        Assert.Equal(DomainErrors.Journal.EmptyComment, _presenter.ValidationError);
    }

    [Fact]
    public async Task Should_PresentValidationError_When_CommentTextTooLong()
    {
        var entry = AddEntryToRepository();
        var longText = new string('x', 1001);

        await _sut.Execute(new AddJournalCommentRequest(entry.Id.Value, longText));

        Assert.True(_presenter.IsValidationError);
        Assert.Equal(DomainErrors.Journal.CommentTooLong, _presenter.ValidationError);
    }

    private sealed class FakeAddJournalCommentPresenter : IAddJournalCommentPresenter
    {
        public JournalEntryViewModel? Entry { get; private set; }
        public bool IsSuccess { get; private set; }
        public bool IsNotFound { get; private set; }
        public bool IsValidationError { get; private set; }
        public string? ValidationError { get; private set; }
        public string? FailureReason { get; private set; }

        public void PresentSuccess(JournalEntryViewModel entry) { Entry = entry; IsSuccess = true; }
        public void PresentNotFound() => IsNotFound = true;
        public void PresentValidationError(string error) { ValidationError = error; IsValidationError = true; }
        public void PresentFailure(string reason) => FailureReason = reason;
    }
}
