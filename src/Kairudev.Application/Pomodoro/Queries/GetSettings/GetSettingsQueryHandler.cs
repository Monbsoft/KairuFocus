using Kairudev.Application.Common;
using Kairudev.Application.Pomodoro.Common;
using Kairudev.Domain.Pomodoro;
using Microsoft.Extensions.Logging;
using Monbsoft.BrilliantMediator.Abstractions.Queries;

namespace Kairudev.Application.Pomodoro.Queries.GetSettings;

public sealed class GetSettingsQueryHandler : IQueryHandler<GetSettingsQuery, GetSettingsResult>
{
    private readonly IPomodoroSettingsRepository _repository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetSettingsQueryHandler> _logger;

    public GetSettingsQueryHandler(
        IPomodoroSettingsRepository repository,
        ICurrentUserService currentUserService,
        ILogger<GetSettingsQueryHandler> logger)
    {
        _repository = repository;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<GetSettingsResult> Handle(
        GetSettingsQuery query,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.CurrentUserId;
        _logger.LogDebug("Fetching Pomodoro settings for user {UserId}", userId);
        var settings = await _repository.GetByUserIdAsync(userId, cancellationToken);
        var viewModel = PomodoroSettingsViewModel.From(settings);
        return new GetSettingsResult(viewModel);
    }
}
