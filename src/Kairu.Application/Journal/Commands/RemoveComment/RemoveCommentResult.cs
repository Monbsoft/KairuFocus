namespace Kairu.Application.Journal.Commands.RemoveComment;

public sealed record RemoveCommentResult
{
    public bool IsSuccess { get; init; }
    public bool IsNotFound { get; init; }

    private RemoveCommentResult() { }

    public static RemoveCommentResult Success() => new() { IsSuccess = true };
    public static RemoveCommentResult NotFound() => new() { IsNotFound = true };
}
