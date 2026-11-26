using KairuFocus.Application.Community.Common;
using KairuFocus.Application.Community.Queries.GetAmbiances;
using KairuFocus.Domain.Community;

namespace KairuFocus.Application.Tests.Community;

public sealed class GetAmbiancesQueryHandlerTests
{
    // Test data only: never part of AmbianceCatalogue.Default.
    private static readonly AmbianceCatalogue Catalogue = new(
    [
        new Ambiance("test-b", "Test B", AmbianceCategory.JazzBlues, "https://www.youtube.com/watch?v=testB"),
        new Ambiance("test-a", "Test A", AmbianceCategory.Lofi, "https://www.youtube.com/watch?v=testA"),
    ]);

    [Fact]
    public async Task Should_ReturnCatalogueInOrder_When_Queried()
    {
        var sut = new GetAmbiancesQueryHandler(Catalogue);

        var result = await sut.Handle(new GetAmbiancesQuery());

        Assert.Equal(
            [
                new AmbianceViewModel("test-b", "Test B", "JazzBlues", "https://www.youtube.com/watch?v=testB"),
                new AmbianceViewModel("test-a", "Test A", "Lofi", "https://www.youtube.com/watch?v=testA"),
            ],
            result);
    }

    [Fact]
    public async Task Should_ReturnEmpty_When_CatalogueIsEmpty()
    {
        var sut = new GetAmbiancesQueryHandler(new AmbianceCatalogue([]));

        Assert.Empty(await sut.Handle(new GetAmbiancesQuery()));
    }
}
