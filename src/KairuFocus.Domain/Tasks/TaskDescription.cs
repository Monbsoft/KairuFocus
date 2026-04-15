using KairuFocus.Domain.Common;

namespace KairuFocus.Domain.Tasks;

public sealed record TaskDescription
{
    public const int MaxLength = 4000;

    public string Value { get; }

    private TaskDescription(string value) => Value = value;

    public static Result<TaskDescription?> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result.Success<TaskDescription?>(null);

        if (value.Length > MaxLength)
            return Result.Failure<TaskDescription?>(DomainErrors.Tasks.DescriptionTooLong);

        return Result.Success<TaskDescription?>(new TaskDescription(value.Trim()));
    }

    public override string ToString() => Value;
}
