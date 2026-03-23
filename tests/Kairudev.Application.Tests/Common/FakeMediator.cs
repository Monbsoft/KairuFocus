using Kairudev.Application.Journal.Commands.CreateEntry;
using Kairudev.Domain.Journal;
using Microsoft.Extensions.Logging.Abstractions;
using Monbsoft.BrilliantMediator.Abstractions;
using Monbsoft.BrilliantMediator.Abstractions.Commands;
using Monbsoft.BrilliantMediator.Abstractions.Events;
using Monbsoft.BrilliantMediator.Abstractions.Queries;

namespace Kairudev.Application.Tests.Common;

/// <summary>
/// Fake mediator for unit tests. Only supports CreateEntryCommand dispatching.
/// Delegates to a real CreateEntryCommandHandler backed by an IJournalEntryRepository.
/// </summary>
public sealed class FakeMediator : IMediator
{
    private readonly CreateEntryCommandHandler _createEntryHandler;

    public FakeMediator(IJournalEntryRepository journalRepository)
    {
        _createEntryHandler = new CreateEntryCommandHandler(
            journalRepository,
            NullLogger<CreateEntryCommandHandler>.Instance);
    }

    public Task DispatchAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand
        => throw new NotSupportedException($"FakeMediator does not support void dispatch for {typeof(TCommand).Name}");

    public async Task<TResponse> DispatchAsync<TCommand, TResponse>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand<TResponse>
    {
        if (command is CreateEntryCommand createEntryCommand)
        {
            var result = await _createEntryHandler.Handle(createEntryCommand, cancellationToken);
            return (TResponse)(object)result;
        }

        throw new NotSupportedException($"FakeMediator does not support dispatch of {typeof(TCommand).Name}");
    }

    public Task<TResponse> SendAsync<TQuery, TResponse>(TQuery query, CancellationToken cancellationToken = default)
        where TQuery : IQuery<TResponse>
        => throw new NotSupportedException($"FakeMediator does not support query {typeof(TQuery).Name}");

    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IEvent
        => throw new NotSupportedException($"FakeMediator does not support event publishing for {typeof(TEvent).Name}");
}
