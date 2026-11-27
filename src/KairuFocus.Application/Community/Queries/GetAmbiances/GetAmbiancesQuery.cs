using KairuFocus.Application.Community.Common;
using Monbsoft.BrilliantMediator.Abstractions.Queries;

namespace KairuFocus.Application.Community.Queries.GetAmbiances;

/// <summary>The catalogue of ambiances a member can choose from, in declaration order.</summary>
public sealed record GetAmbiancesQuery : IQuery<IReadOnlyList<AmbianceViewModel>>;
