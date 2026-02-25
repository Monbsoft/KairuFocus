using Kairudev.Application.Pomodoro.InterruptSession;
using Microsoft.AspNetCore.Mvc;

namespace Kairudev.Api.Pomodoro.Presenters;

public sealed class InterruptSessionHttpPresenter : IInterruptSessionPresenter
{
    public IActionResult? Result { get; private set; }

    public void PresentSuccess() =>
        Result = new NoContentResult();

    public void PresentFailure(string reason) =>
        Result = new BadRequestObjectResult(new { error = reason });
}
