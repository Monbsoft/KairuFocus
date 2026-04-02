using Kairu.Application.Journal.Common;

namespace Kairu.Application.Journal.Commands.AddComment;

public sealed record AddCommentResult
{
    public bool IsSuccess { get; init; }
    public bool IsNotFound { get; init; }
    public JournalEntryViewModel? Entry { get; init; }
    public string? ValidationError { get; init; }

    private AddCommentResult() { }

    public static AddCommentResult Success(JournalEntryViewModel entry) =>
        new() { IsSuccess = true, Entry = entry };

    public static AddCommentResult NotFound() =>
        new() { IsNotFound = true };

    public static AddCommentResult Validation(string error) =>
        new() { ValidationError = error };
}
