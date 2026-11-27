using KairuFocus.Application.Community.Common;
using KairuFocus.Domain.Community;
using Monbsoft.BrilliantMediator.Abstractions.Queries;

namespace KairuFocus.Application.Community.Queries.GetAmbiances;

public sealed class GetAmbiancesQueryHandler : IQueryHandler<GetAmbiancesQuery, IReadOnlyList<AmbianceViewModel>>
{
    private readonly AmbianceCatalogue _catalogue;

    public GetAmbiancesQueryHandler(AmbianceCatalogue catalogue)
    {
        _catalogue = catalogue;
    }

    public Task<IReadOnlyList<AmbianceViewModel>> Handle(
        GetAmbiancesQuery query,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<AmbianceViewModel> ambiances = _catalogue.All.Select(AmbianceViewModel.From).ToList();
        return Task.FromResult(ambiances);
    }
}
