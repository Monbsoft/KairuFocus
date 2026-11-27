using KairuFocus.Application.Common;
using KairuFocus.Domain.Community;
using Monbsoft.BrilliantMediator.Abstractions.Commands;
using CommunityErrors = KairuFocus.Domain.Community.DomainErrors;

namespace KairuFocus.Application.Community.Commands.ChooseAmbiance;

public sealed class ChooseAmbianceCommandHandler : ICommandHandler<ChooseAmbianceCommand, ChooseAmbianceResult>
{
    private readonly IMemberProfileRepository _profileRepository;
    private readonly AmbianceCatalogue _catalogue;
    private readonly ICurrentUserService _currentUserService;

    public ChooseAmbianceCommandHandler(
        IMemberProfileRepository profileRepository,
        AmbianceCatalogue catalogue,
        ICurrentUserService currentUserService)
    {
        _profileRepository = profileRepository;
        _catalogue = catalogue;
        _currentUserService = currentUserService;
    }

    public async Task<ChooseAmbianceResult> Handle(
        ChooseAmbianceCommand command,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.CurrentUserId;

        var profile = await _profileRepository.GetByUserIdAsync(userId, cancellationToken);
        if (profile is null)
            return ChooseAmbianceResult.Failure(CommunityErrors.Community.NotMember);

        var choice = profile.ChooseAmbiance(command.Code, _catalogue);
        if (choice.IsFailure)
            return ChooseAmbianceResult.Failure(choice.Error);

        if (!await _profileRepository.TryUpdateAsync(profile, cancellationToken))
            return ChooseAmbianceResult.Failure(CommunityErrors.Community.NotMember);

        return ChooseAmbianceResult.Success();
    }
}
