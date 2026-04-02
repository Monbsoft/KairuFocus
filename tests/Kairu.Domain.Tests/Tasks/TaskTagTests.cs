using Kairu.Domain.Tasks;

namespace Kairu.Domain.Tests.Tasks;

public sealed class TaskTagTests
{
    [Fact]
    public void Should_CreateTag_When_ValidValue()
    {
        var result = TaskTag.Create("bug");

        Assert.True(result.IsSuccess);
        Assert.Equal("bug", result.Value.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Should_FailCreate_When_EmptyValue(string? value)
    {
        var result = TaskTag.Create(value);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Tasks.TagEmpty, result.Error);
    }

    [Fact]
    public void Should_FailCreate_When_TooLongValue()
    {
        var longTag = new string('a', TaskTag.MaxLength + 1);

        var result = TaskTag.Create(longTag);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Tasks.TagTooLong, result.Error);
    }

    [Fact]
    public void Should_TrimTag_When_Created()
    {
        var result = TaskTag.Create("  urgent  ");

        Assert.True(result.IsSuccess);
        Assert.Equal("urgent", result.Value.Value);
    }

    [Fact]
    public void Should_AcceptTag_When_ExactlyAtMaxLength()
    {
        var tag = new string('a', TaskTag.MaxLength);

        var result = TaskTag.Create(tag);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Should_BeEqualCaseInsensitive_When_SameTagDifferentCase()
    {
        var tag1 = TaskTag.Create("Bug").Value;
        var tag2 = TaskTag.Create("bug").Value;

        Assert.Equal(tag1, tag2);
    }
}
