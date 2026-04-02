using Kairu.Domain.Common;

namespace Kairu.Domain.Tasks;

public sealed class TaskTag
{
    public const int MaxLength = 30;

    private TaskTag(string value) => Value = value;

    public string Value { get; }

    public static Result<TaskTag> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<TaskTag>.Failure(DomainErrors.Tasks.TagEmpty);

        var trimmed = value.Trim();

        if (trimmed.Length > MaxLength)
            return Result<TaskTag>.Failure(DomainErrors.Tasks.TagTooLong);

        return Result<TaskTag>.Success(new TaskTag(trimmed));
    }

    public override string ToString() => Value;

    public override bool Equals(object? obj) =>
        obj is TaskTag other && string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

    public override int GetHashCode() =>
        StringComparer.OrdinalIgnoreCase.GetHashCode(Value);
}
