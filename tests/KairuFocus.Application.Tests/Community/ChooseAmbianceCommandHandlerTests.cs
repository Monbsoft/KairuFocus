using KairuFocus.Application.Community.Commands.ChooseAmbiance;
using KairuFocus.Domain.Community;
using KairuFocus.Domain.Identity;
using CommunityErrors = KairuFocus.Domain.Community.DomainErrors;

namespace KairuFocus.Application.Tests.Community;

public sealed class ChooseAmbianceCommandHandlerTests
{
    // Test data only: never part of AmbianceCatalogue.Default.
    private static readonly AmbianceCatalogue Catalogue = new(
    [
        new Ambiance("test1", "Test 1", AmbianceCategory.Lofi, "https://www.youtube.com/watch?v=test1"),
    ]);

    private readonly FakeMemberProfileRepository _profiles = new();
    private readonly UserId _me = UserId.New();
    private readonly ChooseAmbianceCommandHandler _sut;

    public ChooseAmbianceCommandHandlerTests() =>
        _sut = new ChooseAmbianceCommandHandler(_profiles, Catalogue, new MembershipCurrentUser(_me));

    [Fact]
    public async Task Should_SaveCode_When_InCatalogue()
    {
        _profiles.Seed(_me, "bobby");

        var result = await _sut.Handle(new ChooseAmbianceCommand("test1"));

        Assert.True(result.IsSuccess);
        Assert.Equal("test1", Assert.Single(_profiles.Stored).AmbianceCode);
    }

    [Fact]
    public async Task Should_ClearCode_When_Null()
    {
        _profiles.Seed(_me, "bobby", ambianceCode: "test1");

        var result = await _sut.Handle(new ChooseAmbianceCommand(null));

        Assert.True(result.IsSuccess);
        Assert.Null(Assert.Single(_profiles.Stored).AmbianceCode);
    }

    [Fact]
    public async Task Should_FailUnknownAmbianceAndSaveNothing_When_CodeNotInCatalogue()
    {
        _profiles.Seed(_me, "bobby", ambianceCode: "test1");

        var result = await _sut.Handle(new ChooseAmbianceCommand("nope"));

        Assert.False(result.IsSuccess);
        Assert.Equal(CommunityErrors.Community.UnknownAmbiance, result.Error);
        Assert.Equal(0, _profiles.TryUpdateCallCount);
        Assert.Equal("test1", Assert.Single(_profiles.Stored).AmbianceCode);
    }

    [Fact]
    public async Task Should_FailNotMember_When_ChoosingWithoutProfile()
    {
        var result = await _sut.Handle(new ChooseAmbianceCommand("test1"));

        Assert.False(result.IsSuccess);
        Assert.Equal(CommunityErrors.Community.NotMember, result.Error);
    }
}
