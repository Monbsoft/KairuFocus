using KairuFocus.Application.Journal.Commands.RemoveComment;
using KairuFocus.Domain.Identity;
using KairuFocus.Domain.Journal;
using Microsoft.Extensions.Logging.Abstractions;

namespace KairuFocus.Application.Tests.Journal;

public sealed class RemoveCommentCommandHandlerTests
{
    private static readonly UserId OwnerId = UserId.New();

    private readonly FakeJournalEntryRepository _repository = new();
    private readonly RemoveCommentCommandHandler _sut;

    public RemoveCommentCommandHandlerTests() =>
        _sut = new RemoveCommentCommandHandler(_repository, NullLogger<RemoveCommentCommandHandler>.Instance);

    [Fact]
    public async Task Should_ReturnSuccess_When_CommentExists()
    {
        // Arrange
        var (entry, commentId) = CreateEntryWithComment("A note");
        var command = new RemoveCommentCommand(entry.Id.Value, commentId.Value);

        // Act
        var result = await _sut.Handle(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.IsNotFound);
    }

    [Fact]
    public async Task Should_RemoveCommentFromEntry_When_CommentExists()
    {
        // Arrange
        var (entry, commentId) = CreateEntryWithComment("A note");
        var command = new RemoveCommentCommand(entry.Id.Value, commentId.Value);

        // Act
        await _sut.Handle(command);

        // Assert
        Assert.Empty(entry.Comments);
    }

    [Fact]
    public async Task Should_OnlyRemoveTargetedComment_When_EntryHasSeveralComments()
    {
        // Arrange
        var entry = JournalEntry.Create(
            JournalEventType.SprintCompleted, Guid.NewGuid(), DateTime.UtcNow, OwnerId);
        var firstId = entry.AddComment("First note").Value;
        entry.AddComment("Second note");
        _repository.Entries.Add(entry);
        var command = new RemoveCommentCommand(entry.Id.Value, firstId.Value);

        // Act
        await _sut.Handle(command);

        // Assert
        Assert.Single(entry.Comments);
        Assert.Equal("Second note", entry.Comments[0].Text);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_EntryDoesNotExist()
    {
        // Arrange
        var command = new RemoveCommentCommand(Guid.NewGuid(), Guid.NewGuid());

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
        var (entry, _) = CreateEntryWithComment("A note");
        var command = new RemoveCommentCommand(entry.Id.Value, Guid.NewGuid());

        // Act
        var result = await _sut.Handle(command);

        // Assert
        Assert.True(result.IsNotFound);
        Assert.False(result.IsSuccess);
        Assert.Single(entry.Comments);
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
