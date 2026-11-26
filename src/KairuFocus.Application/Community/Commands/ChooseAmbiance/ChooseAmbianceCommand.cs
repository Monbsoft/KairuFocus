using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace KairuFocus.Application.Community.Commands.ChooseAmbiance;

/// <summary>The current member chooses their ambiance; a null code clears it.</summary>
public sealed record ChooseAmbianceCommand(string? Code) : ICommand<ChooseAmbianceResult>;
