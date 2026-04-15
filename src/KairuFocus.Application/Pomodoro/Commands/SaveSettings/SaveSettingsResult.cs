namespace KairuFocus.Application.Pomodoro.Commands.SaveSettings;

public sealed record SaveSettingsResult
{
    public bool IsSuccess { get; init; }
    public string? ValidationError { get; init; }

    private SaveSettingsResult() { }

    public static SaveSettingsResult Success() => new() { IsSuccess = true };
    public static SaveSettingsResult Validation(string error) => new() { ValidationError = error };
}
