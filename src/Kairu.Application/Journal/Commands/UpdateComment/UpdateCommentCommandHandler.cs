using Kairu.Application.Journal.Common;
using Kairu.Domain.Journal;
using Microsoft.Extensions.Logging;
using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace Kairu.Application.Journal.Commands.UpdateComment;

public sealed class UpdateCommentCommandHandler : ICommandHandler<UpdateCommentCommand, UpdateCommentResult>
{
    private readonly IJournalEntryRepository _repository;
    private readonly ILogger<UpdateCommentCommandHandler> _logger;

    public UpdateCommentCommandHandler(
        IJournalEntryRepository repository,
        ILogger<UpdateCommentCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<UpdateCommentResult> Handle(
        UpdateCommentCommand command,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Updating comment {CommentId} in journal entry {EntryId}", command.CommentId, command.EntryId);

        var entry = await _repository.GetByIdAsync(JournalEntryId.From(command.EntryId), cancellationToken);
        if (entry is null)
        {
            _logger.LogWarning("Journal entry {EntryId} not found", command.EntryId);
            return UpdateCommentResult.NotFound();
        }

        var updateResult = entry.UpdateComment(JournalCommentId.From(command.CommentId), command.Text);
        if (updateResult.IsFailure)
        {
            if (updateResult.Error == DomainErrors.Journal.CommentNotFound)
            {
                _logger.LogWarning("Comment {CommentId} not found in journal entry {EntryId}", command.CommentId, command.EntryId);
                return UpdateCommentResult.NotFound();
            }

            if (updateResult.Error == DomainErrors.Journal.EmptyComment ||
                updateResult.Error == DomainErrors.Journal.CommentTooLong)
                return UpdateCommentResult.Validation(updateResult.Error);

            throw new InvalidOperationException($"Unexpected error while updating comment: {updateResult.Error}");
        }

        await _repository.UpdateAsync(entry, cancellationToken);
        _logger.LogInformation("Comment {CommentId} updated in journal entry {EntryId}", command.CommentId, command.EntryId);
        return UpdateCommentResult.Success(JournalEntryViewModel.From(entry));
    }
}
