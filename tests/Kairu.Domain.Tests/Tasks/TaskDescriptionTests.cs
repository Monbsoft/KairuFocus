using Kairu.Domain.Tasks;

namespace Kairu.Domain.Tests.Tasks;

public sealed class TaskDescriptionTests
{
    [Fact]
    public void Should_ReturnNull_When_ValueIsNull()
    {
        var result = TaskDescription.Create(null);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value);
    }

    [Fact]
    public void Should_ReturnNull_When_ValueIsEmpty()
    {
        var result = TaskDescription.Create(string.Empty);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value);
    }

    [Fact]
    public void Should_ReturnNull_When_ValueIsWhitespace()
    {
        var result = TaskDescription.Create("   ");

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value);
    }

    [Fact]
    public void Should_ReturnSuccess_When_ValueIsValid()
    {
        var result = TaskDescription.Create("This is a valid description");

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("This is a valid description", result.Value.Value);
    }

    [Fact]
    public void Should_TrimValue_When_ValueHasWhitespace()
    {
        var result = TaskDescription.Create("  Trimmed description  ");

        Assert.True(result.IsSuccess);
        Assert.Equal("Trimmed description", result.Value!.Value);
    }

    [Fact]
    public void Should_ReturnFailure_When_ValueExceedsMaxLength()
    {
        var longDescription = new string('x', TaskDescription.MaxLength + 1);

        var result = TaskDescription.Create(longDescription);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Tasks.DescriptionTooLong, result.Error);
    }

    [Fact]
    public void Should_ReturnSuccess_When_ValueIsExactlyMaxLength()
    {
        var description = new string('x', TaskDescription.MaxLength);

        var result = TaskDescription.Create(description);

        Assert.True(result.IsSuccess);
        Assert.Equal(TaskDescription.MaxLength, result.Value!.Value.Length);
    }
}
