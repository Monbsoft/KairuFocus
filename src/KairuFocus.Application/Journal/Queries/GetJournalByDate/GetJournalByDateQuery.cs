using Monbsoft.BrilliantMediator.Abstractions.Queries;

namespace KairuFocus.Application.Journal.Queries.GetJournalByDate;

public sealed record GetJournalByDateQuery(DateOnly Date) : IQuery<GetJournalByDateResult>;
