using KairuFocus.Domain.Common;

namespace KairuFocus.Domain.Community;

/// <summary>One step of a group session: a sprint or a break, with its exact bounds.</summary>
public sealed record GroupStep
{
    // Parameter names match property names: EF Core can materialize the record via constructor binding.
    private GroupStep(int number, GroupStepType type, DateTime startedAtUtc, DateTime endsAtUtc)
    {
        Number = number;
        Type = type;
        StartedAtUtc = startedAtUtc;
        EndsAtUtc = endsAtUtc;
    }

    /// <summary>1-based position of the step in the group session.</summary>
    public int Number { get; }

    public GroupStepType Type { get; }
    public DateTime StartedAtUtc { get; }
    public DateTime EndsAtUtc { get; }

    public static Result<GroupStep> Create(int number, GroupStepType type, DateTime startedAtUtc, DateTime endsAtUtc)
    {
        if (number < 1 || endsAtUtc <= startedAtUtc)
            return Result.Failure<GroupStep>(DomainErrors.Community.InvalidGroupStep);

        return Result.Success(new GroupStep(number, type, startedAtUtc, endsAtUtc));
    }
}
