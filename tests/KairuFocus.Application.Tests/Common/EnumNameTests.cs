using KairuFocus.Application.Common;

namespace KairuFocus.Application.Tests.Common;

public sealed class EnumNameTests
{
    public enum Color
    {
        Red,
        Green,
        Blue
    }

    [Theory]
    [InlineData("Red", Color.Red)]
    [InlineData("Green", Color.Green)]
    [InlineData("Blue", Color.Blue)]
    [InlineData("blue", Color.Blue)]
    [InlineData("GREEN", Color.Green)]
    [InlineData("rEd", Color.Red)]
    public void Should_ReturnTrueAndTheMember_When_ValueIsMemberName(string value, Color expected)
    {
        var parsed = EnumName.TryParse<Color>(value, out var result);

        Assert.True(parsed);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("7")]
    [InlineData("-1")]
    public void Should_ReturnFalse_When_ValueIsNumericOutsideEnum(string value)
    {
        var parsed = EnumName.TryParse<Color>(value, out var result);

        Assert.False(parsed);
        Assert.Equal(default, result);
    }

    // Names only: numeric values are not part of the contract, even when they map to a member.
    [Theory]
    [InlineData("0")]
    [InlineData("1")]
    [InlineData("2")]
    [InlineData("+1")]
    public void Should_ReturnFalse_When_ValueIsNumericMatchingMember(string value)
    {
        var parsed = EnumName.TryParse<Color>(value, out var result);

        Assert.False(parsed);
        Assert.Equal(default, result);
    }

    // Enum.TryParse ORs comma-separated names: "Red,Blue" would be Blue (0|2) and
    // "Green,Blue" the undefined value 3.
    [Theory]
    [InlineData("Red,Blue")]
    [InlineData("Green,Blue")]
    [InlineData("Red, Blue")]
    public void Should_ReturnFalse_When_ValueIsCommaSeparatedList(string value)
    {
        var parsed = EnumName.TryParse<Color>(value, out var result);

        Assert.False(parsed);
        Assert.Equal(default, result);
    }

    [Theory]
    [InlineData(" Blue ")]
    [InlineData(" Blue")]
    [InlineData("Blue ")]
    [InlineData("\tBlue")]
    public void Should_ReturnFalse_When_ValueHasSurroundingWhitespace(string value)
    {
        var parsed = EnumName.TryParse<Color>(value, out var result);

        Assert.False(parsed);
        Assert.Equal(default, result);
    }

    [Theory]
    [InlineData("Purple")]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_ReturnFalse_When_ValueIsUnknownOrBlank(string value)
    {
        var parsed = EnumName.TryParse<Color>(value, out var result);

        Assert.False(parsed);
        Assert.Equal(default, result);
    }

    [Fact]
    public void Should_ReturnFalse_When_ValueIsNull()
    {
        var parsed = EnumName.TryParse<Color>(null, out var result);

        Assert.False(parsed);
        Assert.Equal(default, result);
    }
}
