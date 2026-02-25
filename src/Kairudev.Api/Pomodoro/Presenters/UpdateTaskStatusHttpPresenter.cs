using Kairudev.Application.Pomodoro.UpdateTaskStatus;
using Kairudev.Application.Tasks.Common;
using Microsoft.AspNetCore.Mvc;

namespace Kairudev.Api.Pomodoro.Presenters;

public sealed class UpdateTaskStatusHttpPresenter : IUpdateTaskStatusPresenter
{
    public IActionResult? Result { get; private set; }

    public void PresentSuccess(TaskViewModel task) =>
        Result = new OkObjectResult(task);

    public void PresentNotFound() =>
        Result = new NotFoundResult();

    public void PresentFailure(string reason) =>
        Result = new BadRequestObjectResult(new { error = reason });
}
