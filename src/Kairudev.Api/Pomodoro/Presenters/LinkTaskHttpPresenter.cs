using Kairudev.Application.Pomodoro.LinkTask;
using Microsoft.AspNetCore.Mvc;

namespace Kairudev.Api.Pomodoro.Presenters;

public sealed class LinkTaskHttpPresenter : ILinkTaskPresenter
{
    public IActionResult? Result { get; private set; }

    public void PresentSuccess() =>
        Result = new NoContentResult();

    public void PresentNotFound() =>
        Result = new NotFoundResult();

    public void PresentFailure(string reason) =>
        Result = new BadRequestObjectResult(new { error = reason });
}
