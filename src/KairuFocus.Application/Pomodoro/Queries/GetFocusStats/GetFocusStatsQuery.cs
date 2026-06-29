using Monbsoft.BrilliantMediator.Abstractions.Queries;

namespace KairuFocus.Application.Pomodoro.Queries.GetFocusStats;

/// <summary>
/// Query pour la page « Statistiques du focus ».
/// OffsetMinutes : décalage UTC du client en minutes (local = UTC + offset).
/// Défaut 0 = comportement UTC (rétro-compat / tests).
/// </summary>
public sealed record GetFocusStatsQuery(int OffsetMinutes = 0) : IQuery<GetFocusStatsResult>;
