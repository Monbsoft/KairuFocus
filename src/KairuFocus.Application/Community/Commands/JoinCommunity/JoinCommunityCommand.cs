using KairuFocus.Domain.Community;
using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace KairuFocus.Application.Community.Commands.JoinCommunity;

/// <summary>UC-CO-01 — the current user joins the community with their GitHub login or a chosen name.</summary>
/// <param name="Source">Where the name comes from.</param>
/// <param name="ChosenName">The name typed by the member; ignored when <paramref name="Source"/> is GitHub.</param>
public sealed record JoinCommunityCommand(ProfileNameSource Source, string? ChosenName)
    : ICommand<JoinCommunityResult>;
