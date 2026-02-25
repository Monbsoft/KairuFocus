using Kairudev.Application.Pomodoro.Common;
using Kairudev.Application.Pomodoro.GetSettings;
using Microsoft.AspNetCore.Mvc;

namespace Kairudev.Api.Pomodoro.Presenters;

public sealed class GetSettingsHttpPresenter : IGetSettingsPresenter
{
    public IActionResult? Result { get; private set; }

    public void PresentSuccess(PomodoroSettingsViewModel settings) =>
        Result = new OkObjectResult(settings);
}
