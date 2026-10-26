namespace KairuFocus.Application.Gamification.Commands.RerollPseudonym;

public sealed record RerollPseudonymResult
{
    public bool IsSuccess { get; init; }
    public string? Pseudonym { get; init; }
    public string? Error { get; init; }

    private RerollPseudonymResult() { }

    public static RerollPseudonymResult Success(string pseudonym) => new() { IsSuccess = true, Pseudonym = pseudonym };
    public static RerollPseudonymResult Failure(string error) => new() { Error = error };
}
