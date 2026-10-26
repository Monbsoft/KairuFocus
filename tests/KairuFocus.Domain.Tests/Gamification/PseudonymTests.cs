using KairuFocus.Domain.Gamification;
using GamificationErrors = KairuFocus.Domain.Gamification.DomainErrors;

namespace KairuFocus.Domain.Tests.Gamification;

public sealed class PseudonymTests
{
    // ── nominal ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("LoutreTenace")]
    [InlineData("HibouAttentif")]
    [InlineData("Ab")]
    public void Should_CreatePseudonym_When_ValueIsPascalCaseLetters(string value)
    {
        var result = Pseudonym.Create(value);

        Assert.True(result.IsSuccess);
        Assert.Equal(value, result.Value.Value);
        Assert.Equal(value, result.Value.ToString());
    }

    [Theory]
    [InlineData("LoutreTenace27")]
    [InlineData("MesangeVive99")]
    public void Should_CreatePseudonym_When_ValueEndsWithTwoDigitSuffix(string value)
    {
        var result = Pseudonym.Create(value);

        Assert.True(result.IsSuccess);
        Assert.Equal(value, result.Value.Value);
    }

    [Fact]
    public void Should_CreatePseudonym_When_ValueIsExactly60Characters()
    {
        var value = "L" + new string('o', Pseudonym.MaxLength - 1);

        var result = Pseudonym.Create(value);

        Assert.True(result.IsSuccess);
    }

    // ── failure cases ─────────────────────────────────────────────────────────

    [Fact]
    public void Should_Fail_When_ValueIsEmpty()
    {
        var result = Pseudonym.Create(string.Empty);

        Assert.True(result.IsFailure);
        Assert.Equal(GamificationErrors.Gamification.InvalidPseudonym, result.Error);
    }

    [Fact]
    public void Should_Fail_When_ValueExceeds60Characters()
    {
        var value = "L" + new string('o', Pseudonym.MaxLength);

        var result = Pseudonym.Create(value);

        Assert.True(result.IsFailure);
        Assert.Equal(GamificationErrors.Gamification.InvalidPseudonym, result.Error);
    }

    [Theory]
    [InlineData("MésangeVive")]
    [InlineData("HéronPatient")]
    [InlineData("Loutre Tenace")]
    [InlineData("LoutreTenace ")]
    [InlineData("LoutreTenace\n")]
    public void Should_Fail_When_ValueContainsAccentOrSpace(string value)
    {
        var result = Pseudonym.Create(value);

        Assert.True(result.IsFailure);
        Assert.Equal(GamificationErrors.Gamification.InvalidPseudonym, result.Error);
    }

    [Theory]
    [InlineData("loutreTenace")]
    [InlineData("2LoutreTenace")]
    public void Should_Fail_When_ValueStartsWithLowercaseOrDigit(string value)
    {
        var result = Pseudonym.Create(value);

        Assert.True(result.IsFailure);
        Assert.Equal(GamificationErrors.Gamification.InvalidPseudonym, result.Error);
    }

    [Fact]
    public void Should_Fail_When_SuffixHasThreeDigits()
    {
        var result = Pseudonym.Create("LoutreTenace100");

        Assert.True(result.IsFailure);
        Assert.Equal(GamificationErrors.Gamification.InvalidPseudonym, result.Error);
    }

    [Theory]
    [InlineData("Loutre2Tenace")]
    [InlineData("L27")]
    public void Should_Fail_When_DigitsAreNotASuffixOfAtLeastTwoLetters(string value)
    {
        var result = Pseudonym.Create(value);

        Assert.True(result.IsFailure);
        Assert.Equal(GamificationErrors.Gamification.InvalidPseudonym, result.Error);
    }
}
