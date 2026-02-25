using Kairudev.Application.Pomodoro.CompleteSession;
using Microsoft.AspNetCore.Mvc;

namespace Kairudev.Api.Pomodoro.Presenters;

public sealed class CompleteSessionHttpPresenter : ICompleteSessionPresenter
{
    public IActionResult? Result { get; private set; }

    public void PresentSuccess(CompleteSessionResult result) =>
        Result = new OkObjectResult(result);

    public void PresentFailure(string reason) =>
        Result = new BadRequestObjectResult(new { error = reason });
}
