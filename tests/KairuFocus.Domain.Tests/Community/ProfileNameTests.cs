using KairuFocus.Domain.Community;
using CommunityErrors = KairuFocus.Domain.Community.DomainErrors;

namespace KairuFocus.Domain.Tests.Community;

public sealed class ProfileNameTests
{
    public static TheoryData<string> ValidNames =>
    [
        "abc",
        "a-b",
        "a_b",
        "1ab",
        new string('a', ProfileName.MaxLength),
    ];

    public static TheoryData<string?> MalformedNames =>
    [
        null,
        "",
        "ab",
        new string('a', ProfileName.MaxLength + 1),
        "-ab",
        "_ab",
        "a b",
        "é12",
        "ab\n",
        " abc",
        // A reserved word shorter than MinLength is rejected by the format first.
        "me",
    ];

    [Theory]
    [MemberData(nameof(ValidNames))]
    public void Should_Accept_When_ValueIsValid(string value)
    {
        var result = ProfileName.Create(value);

        Assert.True(result.IsSuccess);
        Assert.Equal(value, result.Value.Value);
    }

    [Theory]
    [MemberData(nameof(MalformedNames))]
    public void Should_RejectWithInvalidName_When_ValueIsMalformed(string? value)
    {
        var result = ProfileName.Create(value);

        Assert.True(result.IsFailure);
        Assert.Equal(CommunityErrors.Community.InvalidName, result.Error);
    }

    [Theory]
    [InlineData("admin")]
    [InlineData("kairu")]
    [InlineData("kairufocus")]
    [InlineData("support")]
    [InlineData("moi")]
    [InlineData("profil")]
    [InlineData("membres")]
    [InlineData("null")]
    [InlineData("undefined")]
    [InlineData("ADMIN")]
    public void Should_RejectWithReservedName_When_ValueIsReserved(string value)
    {
        var result = ProfileName.Create(value);

        Assert.True(result.IsFailure);
        Assert.Equal(CommunityErrors.Community.ReservedName, result.Error);
    }

    [Fact]
    public void Should_ExposeLowercaseKey_When_ValueHasMixedCase()
    {
        var name = ProfileName.Create("Lea_Dev").Value;

        Assert.Equal("Lea_Dev", name.Value);
        Assert.Equal("lea_dev", name.Key);
    }

    [Fact]
    public void Should_KeepValueAndKey_When_RestoredFromStoredValue()
    {
        var name = ProfileName.Restore("Lea_Dev");

        Assert.Equal("Lea_Dev", name.Value);
        Assert.Equal("lea_dev", name.Key);
    }

    [Fact]
    public void Should_BeEqualToCreatedName_When_RestoredFromSameValue()
    {
        Assert.Equal(ProfileName.Create("Lea_Dev").Value, ProfileName.Restore("Lea_Dev"));
    }
}
