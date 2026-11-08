using KairuFocus.Application.Gamification.Commands.CreditSprintXp;
using KairuFocus.Application.Journal.Commands.CreateEntry;
using KairuFocus.Application.Pomodoro.Commands.CompleteSession;
using KairuFocus.Application.Pomodoro.Commands.InterruptSession;
using KairuFocus.Domain.Journal;
using Microsoft.Extensions.Logging.Abstractions;
using Monbsoft.BrilliantMediator.Abstractions;
using Monbsoft.BrilliantMediator.Abstractions.Commands;
using Monbsoft.BrilliantMediator.Abstractions.Events;
using Monbsoft.BrilliantMediator.Abstractions.Queries;

namespace KairuFocus.Application.Tests.Common;

/// <summary>
/// Fake mediator for unit tests. Supports CreateEntryCommand dispatching (delegates to a
/// real CreateEntryCommandHandler backed by an IJournalEntryRepository) and, optionally,
/// CreditSprintXpCommand (delegates to a real CreditSprintXpCommandHandler when supplied), as well as
/// CompleteSessionCommand and InterruptSessionCommand (delegated to the real handlers when supplied).
/// Set <see cref="ThrowOnCreditSprintXp"/> to simulate an XP credit crash.
/// </summary>
public sealed class FakeMediator : IMediator
{
    private readonly CreateEntryCommandHandler _createEntryHandler;
    private readonly CreditSprintXpCommandHandler? _creditSprintXpHandler;
    private readonly CompleteSessionCommandHandler? _completeSessionHandler;
    private readonly InterruptSessionCommandHandler? _interruptSessionHandler;

    /// <summary>When true, dispatching CreditSprintXpCommand throws (simulated credit failure).</summary>
    public bool ThrowOnCreditSprintXp { get; set; }

    /// <summary>When true, dispatching CreateEntryCommand throws (simulated journal crash).</summary>
    public bool ThrowOnCreateEntry { get; set; }

    /// <summary>When set, dispatching CreditSprintXpCommand returns a clean Failure with this error.</summary>
    public string? CreditSprintXpFailureError { get; set; }

    public FakeMediator(
        IJournalEntryRepository journalRepository,
        CreditSprintXpCommandHandler? creditSprintXpHandler = null,
        CompleteSessionCommandHandler? completeSession = null,
        InterruptSessionCommandHandler? interruptSession = null)
    {
        _createEntryHandler = new CreateEntryCommandHandler(
            journalRepository,
            NullLogger<CreateEntryCommandHandler>.Instance);
        _creditSprintXpHandler = creditSprintXpHandler;
        _completeSessionHandler = completeSession;
        _interruptSessionHandler = interruptSession;
    }

    public Task DispatchAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand
        => throw new NotSupportedException($"FakeMediator does not support void dispatch for {typeof(TCommand).Name}");

    public async Task<TResponse> DispatchAsync<TCommand, TResponse>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand<TResponse>
    {
        if (command is CreateEntryCommand createEntryCommand)
        {
            if (ThrowOnCreateEntry)
                throw new InvalidOperationException("Simulated journal entry failure.");

            var result = await _createEntryHandler.Handle(createEntryCommand, cancellationToken);
            return (TResponse)(object)result;
        }

        if (command is CreditSprintXpCommand creditSprintXpCommand)
        {
            if (ThrowOnCreditSprintXp)
                throw new InvalidOperationException("Simulated XP credit failure.");

            if (CreditSprintXpFailureError is not null)
                return (TResponse)(object)CreditSprintXpResult.Failure(CreditSprintXpFailureError);

            if (_creditSprintXpHandler is not null)
            {
                var result = await _creditSprintXpHandler.Handle(creditSprintXpCommand, cancellationToken);
                return (TResponse)(object)result;
            }

            // No handler configured: tests that do not care about XP get a neutral result.
            return (TResponse)(object)CreditSprintXpResult.Success(0);
        }

        if (command is CompleteSessionCommand completeSessionCommand && _completeSessionHandler is not null)
        {
            var result = await _completeSessionHandler.Handle(completeSessionCommand, cancellationToken);
            return (TResponse)(object)result;
        }

        if (command is InterruptSessionCommand interruptSessionCommand && _interruptSessionHandler is not null)
        {
            var result = await _interruptSessionHandler.Handle(interruptSessionCommand, cancellationToken);
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
