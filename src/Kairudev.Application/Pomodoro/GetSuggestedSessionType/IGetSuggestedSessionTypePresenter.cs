namespace Kairudev.Application.Pomodoro.GetSuggestedSessionType;

public interface IGetSuggestedSessionTypePresenter
{
    void PresentSuggestion(string suggestedType, int sprintDuration, int shortBreakDuration, int longBreakDuration);
}
