using Monbsoft.BrilliantMediator.Abstractions.Queries;

namespace Kairu.Application.Pomodoro.Queries.GetSettings;

/// <summary>
/// Query to retrieve Pomodoro settings (no parameters needed).
/// </summary>
public sealed record GetSettingsQuery : IQuery<GetSettingsResult>;
