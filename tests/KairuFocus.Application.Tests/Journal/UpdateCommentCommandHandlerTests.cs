using KairuFocus.Application.Journal.Commands.UpdateComment;
using KairuFocus.Domain.Identity;
using KairuFocus.Domain.Journal;
using Microsoft.Extensions.Logging.Abstractions;

namespace KairuFocus.Application.Tests.Journal;

public sealed class UpdateCommentCommandHandlerTests
{
    private static readonly UserId OwnerId = UserId.New();

    private readonly FakeJournalEntryRepository _repository = new();
    private readonly UpdateCommentCommandHandler _sut;

    public UpdateCommentCommandHandlerTests() =>
        _sut = new UpdateCommentCommandHandler(_repository, NullLogger<UpdateCommentCommandHandler>.Instance);

    [Fact]
    public async Task Should_ReturnSuccess_When_CommentExistsAndTextIsValid()
    {
        // Arrange
        var (entry, commentId) = CreateEntryWithComment("Original text");
        var command = new UpdateCommentCommand(entry.Id.Value, commentId.Value, "Updated text");

        // Act
        var result = await _sut.Handle(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Entry);
    }

    [Fact]
    public async Task Should_ChangeCommentText_When_TextIsValid()
    {
        // Arrange
        var (entry, commentId) = CreateEntryWithComment("Original text");
        var command = new UpdateCommentCommand(entry.Id.Value, commentId.Value, "Updated text");

        // Act
        await _sut.Handle(command);

        // Assert
        Assert.Single(entry.Comments);
        Assert.Equal("Updated text", entry.Comments[0].Text);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_EntryDoesNotExist()
    {
        // Arrange
        var command = new UpdateCommentCommand(Guid.NewGuid(), Guid.NewGuid(), "Updated text");

        // Act
        var result = await _sut.Handle(command);

        // Assert
        Assert.True(result.IsNotFound);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_CommentDoesNotExistInEntry()
    {
        // Arrange
        var (entry, _) = CreateEntryWithComment("Original text");
        var command = new UpdateCommentCommand(entry.Id.Value, Guid.NewGuid(), "Updated text");

        // Act
        var result = await _sut.Handle(command);

        // Assert
        Assert.True(result.IsNotFound);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Should_ReturnValidationError_When_TextIsEmpty()
    {
        // Arrange
        var (entry, commentId) = CreateEntryWithComment("Original text");
        var command = new UpdateCommentCommand(entry.Id.Value, commentId.Value, "   ");

        // Act
        var result = await _sut.Handle(command);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrors.Journal.EmptyComment, result.ValidationError);
        Assert.Equal("Original text", entry.Comments[0].Text);
    }

    [Fact]
    public async Task Should_ReturnValidationError_When_TextIsTooLong()
    {
        // Arrange
        var (entry, commentId) = CreateEntryWithComment("Original text");
        var command = new UpdateCommentCommand(entry.Id.Value, commentId.Value, new string('x', 1001));

        // Act
        var result = await _sut.Handle(command);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrors.Journal.CommentTooLong, result.ValidationError);
        Assert.Equal("Original text", entry.Comments[0].Text);
    }

    [Fact]
    public async Task Should_ReturnSuccess_When_TextIsExactlyMaxLength()
    {
        // Arrange
        var (entry, commentId) = CreateEntryWithComment("Original text");
        var text = new string('x', 1000);
        var command = new UpdateCommentCommand(entry.Id.Value, commentId.Value, text);

        // Act
        var result = await _sut.Handle(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(entry.Comments);
        Assert.Equal(text, entry.Comments[0].Text);
    }

    private (JournalEntry entry, JournalCommentId commentId) CreateEntryWithComment(string text)
    {
        var entry = JournalEntry.Create(
            JournalEventType.SprintCompleted, Guid.NewGuid(), DateTime.UtcNow, OwnerId);
        var commentId = entry.AddComment(text).Value;
        _repository.Entries.Add(entry);
        return (entry, commentId);
    }
}
