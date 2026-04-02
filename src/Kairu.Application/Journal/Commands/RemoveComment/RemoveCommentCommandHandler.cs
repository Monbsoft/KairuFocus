using Kairu.Domain.Journal;
using Microsoft.Extensions.Logging;
using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace Kairu.Application.Journal.Commands.RemoveComment;

public sealed class RemoveCommentCommandHandler : ICommandHandler<RemoveCommentCommand, RemoveCommentResult>
{
    private readonly IJournalEntryRepository _repository;
    private readonly ILogger<RemoveCommentCommandHandler> _logger;

    public RemoveCommentCommandHandler(
        IJournalEntryRepository repository,
        ILogger<RemoveCommentCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<RemoveCommentResult> Handle(
        RemoveCommentCommand command,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Removing comment {CommentId} from journal entry {EntryId}", command.CommentId, command.EntryId);

        var entry = await _repository.GetByIdAsync(JournalEntryId.From(command.EntryId), cancellationToken);
        if (entry is null)
        {
            _logger.LogWarning("Journal entry {EntryId} not found", command.EntryId);
            return RemoveCommentResult.NotFound();
        }

        var removeResult = entry.RemoveComment(JournalCommentId.From(command.CommentId));
        if (removeResult.IsFailure)
        {
            if (removeResult.Error == DomainErrors.Journal.CommentNotFound)
            {
                _logger.LogWarning("Comment {CommentId} not found in journal entry {EntryId}", command.CommentId, command.EntryId);
                return RemoveCommentResult.NotFound();
            }

            throw new InvalidOperationException($"Unexpected error while removing comment: {removeResult.Error}");
        }

        await _repository.UpdateAsync(entry, cancellationToken);
        _logger.LogInformation("Comment {CommentId} removed from journal entry {EntryId}", command.CommentId, command.EntryId);
        return RemoveCommentResult.Success();
    }
}
