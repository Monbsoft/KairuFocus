using KairuFocus.Domain.Community;
using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace KairuFocus.Application.Community.Commands.ChangeProfileName;

/// <summary>UC-CO-02 — the current member changes their profile name (GitHub login or chosen name).</summary>
/// <param name="Source">Where the new name comes from.</param>
/// <param name="ChosenName">The name typed by the member; ignored when <paramref name="Source"/> is GitHub.</param>
public sealed record ChangeProfileNameCommand(ProfileNameSource Source, string? ChosenName)
    : ICommand<ChangeProfileNameResult>;
