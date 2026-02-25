using Kairudev.Application.Pomodoro.Common;
using Kairudev.Application.Pomodoro.StartSession;
using Microsoft.AspNetCore.Mvc;

namespace Kairudev.Api.Pomodoro.Presenters;

public sealed class StartSessionHttpPresenter : IStartSessionPresenter
{
    public IActionResult? Result { get; private set; }

    public void PresentSuccess(PomodoroSessionViewModel session) =>
        Result = new CreatedAtActionResult(
            actionName: "GetCurrentSession",
            controllerName: "Pomodoro",
            routeValues: null,
            value: session);

    public void PresentFailure(string reason) =>
        Result = new ConflictObjectResult(new { error = reason });
}
