using KairuFocus.Application.Journal.Commands.AddComment;
using KairuFocus.Domain.Identity;
using KairuFocus.Domain.Journal;
using Microsoft.Extensions.Logging.Abstractions;

namespace KairuFocus.Application.Tests.Journal;

public sealed class AddCommentCommandHandlerTests
{
    private static readonly UserId OwnerId = UserId.New();

    private readonly FakeJournalEntryRepository _repository = new();
    private readonly AddCommentCommandHandler _sut;

    public AddCommentCommandHandlerTests() =>
        _sut = new AddCommentCommandHandler(_repository, NullLogger<AddCommentCommandHandler>.Instance);

    [Fact]
    public async Task Should_ReturnSuccess_When_EntryExistsAndTextIsValid()
    {
        // Arrange
        var entry = CreateAndAddEntry();
        var command = new AddCommentCommand(entry.Id.Value, "A relevant note");

        // Act
        var result = await _sut.Handle(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Entry);
    }

    [Fact]
    public async Task Should_AddCommentToEntry_When_TextIsValid()
    {
        // Arrange
        var entry = CreateAndAddEntry();
        var command = new AddCommentCommand(entry.Id.Value, "A relevant note");

        // Act
        await _sut.Handle(command);

        // Assert
        Assert.Single(entry.Comments);
        Assert.Equal("A relevant note", entry.Comments[0].Text);
    }

    [Fact]
    public async Task Should_ExposeCommentInResult_When_TextIsValid()
    {
        // Arrange
        var entry = CreateAndAddEntry();
        var command = new AddCommentCommand(entry.Id.Value, "A relevant note");

        // Act
        var result = await _sut.Handle(command);

        // Assert
        Assert.Single(result.Entry!.Comments);
        Assert.Equal("A relevant note", result.Entry!.Comments[0].Text);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_EntryDoesNotExist()
    {
        // Arrange
        var command = new AddCommentCommand(Guid.NewGuid(), "A relevant note");

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
        var entry = CreateAndAddEntry();
        var command = new AddCommentCommand(entry.Id.Value, "   ");

        // Act
        var result = await _sut.Handle(command);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrors.Journal.EmptyComment, result.ValidationError);
        Assert.Empty(entry.Comments);
    }

    [Fact]
    public async Task Should_ReturnValidationError_When_TextIsTooLong()
    {
        // Arrange
        var entry = CreateAndAddEntry();
        var command = new AddCommentCommand(entry.Id.Value, new string('x', 1001));

        // Act
        var result = await _sut.Handle(command);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrors.Journal.CommentTooLong, result.ValidationError);
        Assert.Empty(entry.Comments);
    }

    [Fact]
    public async Task Should_ReturnSuccess_When_TextIsExactlyMaxLength()
    {
        // Arrange
        var entry = CreateAndAddEntry();
        var text = new string('x', 1000);
        var command = new AddCommentCommand(entry.Id.Value, text);

        // Act
        var result = await _sut.Handle(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(entry.Comments);
        Assert.Equal(text, entry.Comments[0].Text);
    }

    private JournalEntry CreateAndAddEntry()
    {
        var entry = JournalEntry.Create(
            JournalEventType.SprintCompleted, Guid.NewGuid(), DateTime.UtcNow, OwnerId);
        _repository.Entries.Add(entry);
        return entry;
    }
}
