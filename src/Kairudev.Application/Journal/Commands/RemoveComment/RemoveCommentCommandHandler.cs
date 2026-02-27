using Kairudev.Domain.Journal;

namespace Kairudev.Application.Journal.Commands.RemoveComment;

public sealed class RemoveCommentCommandHandler
{
    private readonly IJournalEntryRepository _repository;

    public RemoveCommentCommandHandler(IJournalEntryRepository repository)
    {
        _repository = repository;
    }

    public async Task<RemoveCommentResult> HandleAsync(
        RemoveCommentCommand command,
        CancellationToken cancellationToken = default)
    {
        var entry = await _repository.GetByIdAsync(JournalEntryId.From(command.EntryId), cancellationToken);
        if (entry is null)
            return RemoveCommentResult.NotFound();

        var removeResult = entry.RemoveComment(JournalCommentId.From(command.CommentId));
        if (removeResult.IsFailure)
            return RemoveCommentResult.NotFound();

        await _repository.UpdateAsync(entry, cancellationToken);
        return RemoveCommentResult.Success();
    }
}
