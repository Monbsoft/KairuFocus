using Kairudev.Application.Pomodoro.CreateTaskDuringSession;
using Kairudev.Application.Tasks.Common;
using Microsoft.AspNetCore.Mvc;

namespace Kairudev.Api.Pomodoro.Presenters;

public sealed class CreateTaskDuringSessionHttpPresenter : ICreateTaskDuringSessionPresenter
{
    public IActionResult? Result { get; private set; }

    public void PresentSuccess(TaskViewModel task) =>
        Result = new CreatedAtActionResult(
            actionName: "GetById",
            controllerName: "Tasks",
            routeValues: new { id = task.Id },
            value: task);

    public void PresentValidationError(string error) =>
        Result = new BadRequestObjectResult(new { error });

    public void PresentFailure(string reason) =>
        Result = new BadRequestObjectResult(new { error = reason });
}
