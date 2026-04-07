using Monbsoft.BrilliantMediator.Abstractions.Queries;

namespace Kairu.Application.Journal.Queries.GetJournalByDate;

public sealed record GetJournalByDateQuery(DateOnly Date) : IQuery<GetJournalByDateResult>;
