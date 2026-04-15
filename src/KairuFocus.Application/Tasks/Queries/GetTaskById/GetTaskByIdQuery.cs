using Monbsoft.BrilliantMediator.Abstractions.Queries;

namespace KairuFocus.Application.Tasks.Queries.GetTaskById;

/// <summary>
/// Query to retrieve a single task by its identifier.
/// </summary>
public sealed record GetTaskByIdQuery(Guid Id) : IQuery<GetTaskByIdResult>;
