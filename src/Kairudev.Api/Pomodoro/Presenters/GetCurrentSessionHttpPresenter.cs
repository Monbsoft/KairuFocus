using Kairudev.Application.Pomodoro.Common;
using Kairudev.Application.Pomodoro.GetCurrentSession;
using Microsoft.AspNetCore.Mvc;

namespace Kairudev.Api.Pomodoro.Presenters;

public sealed class GetCurrentSessionHttpPresenter : IGetCurrentSessionPresenter
{
    public IActionResult? Result { get; private set; }

    public void PresentSession(PomodoroSessionViewModel session) =>
        Result = new OkObjectResult(session);

    public void PresentNoSession() =>
        Result = new NoContentResult();
}
