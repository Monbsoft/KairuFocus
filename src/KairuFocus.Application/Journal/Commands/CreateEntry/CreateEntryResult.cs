namespace KairuFocus.Application.Journal.Commands.CreateEntry;

public sealed record CreateEntryResult
{
    public bool IsSuccess { get; init; }

    private CreateEntryResult() { }

    public static CreateEntryResult Success() => new() { IsSuccess = true };
}
