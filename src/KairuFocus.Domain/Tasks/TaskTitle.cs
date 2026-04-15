using KairuFocus.Domain.Common;

namespace KairuFocus.Domain.Tasks;

public sealed record TaskTitle
{
    public const int MaxLength = 200;

    public string Value { get; }

    private TaskTitle(string value) => Value = value;

    public static Result<TaskTitle> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result.Failure<TaskTitle>(DomainErrors.Tasks.EmptyTitle);

        if (value.Length > MaxLength)
            return Result.Failure<TaskTitle>(DomainErrors.Tasks.TitleTooLong);

        return Result.Success(new TaskTitle(value.Trim()));
    }

    public override string ToString() => Value;
}
