using Kairudev.Application.Pomodoro.GetSuggestedSessionType;
using Microsoft.AspNetCore.Mvc;

namespace Kairudev.Api.Pomodoro.Presenters;

public sealed class GetSuggestedSessionTypeHttpPresenter : IGetSuggestedSessionTypePresenter
{
    public IActionResult? Result { get; private set; }

    public void PresentSuggestion(string suggestedType, int sprintDuration, int shortBreakDuration, int longBreakDuration)
    {
        Result = new OkObjectResult(new
        {
            SuggestedType = suggestedType,
            SprintDurationMinutes = sprintDuration,
            ShortBreakDurationMinutes = shortBreakDuration,
            LongBreakDurationMinutes = longBreakDuration
        });
    }
}
