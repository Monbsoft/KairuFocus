using Kairu.Domain.Tasks;

namespace Kairu.Domain.Tests.Tasks;

public sealed class TaskTitleTests
{
    [Fact]
    public void Should_CreateTitle_When_ValueIsValid()
    {
        var result = TaskTitle.Create("Fix authentication bug");

        Assert.True(result.IsSuccess);
        Assert.Equal("Fix authentication bug", result.Value.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Should_ReturnFailure_When_TitleIsEmpty(string? value)
    {
        var result = TaskTitle.Create(value!);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Tasks.EmptyTitle, result.Error);
    }

    [Fact]
    public void Should_ReturnFailure_When_TitleExceedsMaxLength()
    {
        var longTitle = new string('a', TaskTitle.MaxLength + 1);

        var result = TaskTitle.Create(longTitle);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Tasks.TitleTooLong, result.Error);
    }

    [Fact]
    public void Should_TrimTitle_When_ValueHasLeadingOrTrailingSpaces()
    {
        var result = TaskTitle.Create("  Fix bug  ");

        Assert.True(result.IsSuccess);
        Assert.Equal("Fix bug", result.Value.Value);
    }

    [Fact]
    public void Should_AcceptTitle_When_ExactlyAtMaxLength()
    {
        var title = new string('a', TaskTitle.MaxLength);

        var result = TaskTitle.Create(title);

        Assert.True(result.IsSuccess);
    }
}
