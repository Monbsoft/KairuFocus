using Monbsoft.BrilliantMediator.Abstractions;
using Monbsoft.BrilliantMediator.Abstractions.Commands;
using Monbsoft.BrilliantMediator.Abstractions.Events;
using Monbsoft.BrilliantMediator.Abstractions.Queries;

namespace KairuFocus.Api.Tests.Auth;

/// <summary>
/// Configurable mediator stub for integration tests.
/// Set <see cref="DispatchResult"/> to control what DispatchAsync returns
/// for any command with a typed response, and <see cref="SendResult"/> to control
/// what SendAsync returns for any query.
/// </summary>
public sealed class TestMediator : IMediator
{
    /// <summary>
    /// Factory function called for each DispatchAsync&lt;TCommand, TResponse&gt; call.
    /// Return the boxed TResponse value.
    /// </summary>
    public Func<object, object>? DispatchResult { get; set; }

    /// <summary>
    /// Factory function called for each SendAsync&lt;TQuery, TResponse&gt; call,
    /// with the query as argument. Return the boxed TResponse value.
    /// </summary>
    public Func<object, object>? SendResult { get; set; }

    public Task DispatchAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand
        => throw new NotSupportedException($"TestMediator does not support void dispatch for {typeof(TCommand).Name}");

    public Task<TResponse> DispatchAsync<TCommand, TResponse>(
        TCommand command,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand<TResponse>
    {
        if (DispatchResult is null)
            throw new InvalidOperationException(
                $"TestMediator.DispatchResult is not configured for command {typeof(TCommand).Name}.");

        var result = DispatchResult(command);
        return Task.FromResult((TResponse)result);
    }

    public Task<TResponse> SendAsync<TQuery, TResponse>(
        TQuery query,
        CancellationToken cancellationToken = default)
        where TQuery : IQuery<TResponse>
    {
        if (SendResult is null)
            throw new InvalidOperationException(
                $"TestMediator.SendResult is not configured for query {typeof(TQuery).Name}.");

        var result = SendResult(query);
        return Task.FromResult((TResponse)result);
    }

    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IEvent
        => throw new NotSupportedException($"TestMediator does not support event publishing for {typeof(TEvent).Name}");
}
