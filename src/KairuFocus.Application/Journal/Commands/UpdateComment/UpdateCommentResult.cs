using KairuFocus.Application.Journal.Common;

namespace KairuFocus.Application.Journal.Commands.UpdateComment;

public sealed record UpdateCommentResult
{
    public bool IsSuccess { get; init; }
    public bool IsNotFound { get; init; }
    public JournalEntryViewModel? Entry { get; init; }
    public string? ValidationError { get; init; }

    private UpdateCommentResult() { }

    public static UpdateCommentResult Success(JournalEntryViewModel entry) =>
        new() { IsSuccess = true, Entry = entry };

    public static UpdateCommentResult NotFound() =>
        new() { IsNotFound = true };

    public static UpdateCommentResult Validation(string error) =>
        new() { ValidationError = error };
}
