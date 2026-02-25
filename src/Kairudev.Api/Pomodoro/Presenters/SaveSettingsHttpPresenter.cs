using Kairudev.Application.Pomodoro.SaveSettings;
using Microsoft.AspNetCore.Mvc;

namespace Kairudev.Api.Pomodoro.Presenters;

public sealed class SaveSettingsHttpPresenter : ISaveSettingsPresenter
{
    public IActionResult? Result { get; private set; }

    public void PresentSuccess() =>
        Result = new NoContentResult();

    public void PresentValidationError(string error) =>
        Result = new BadRequestObjectResult(new { error });
}
