using Kairudev.Application.Journal.Common;
using Kairudev.Domain.Journal;

namespace Kairudev.Application.Journal.Commands.UpdateComment;

public sealed class UpdateCommentCommandHandler
{
    private readonly IJournalEntryRepository _repository;

    public UpdateCommentCommandHandler(IJournalEntryRepository repository)
    {
        _repository = repository;
    }

    public async Task<UpdateCommentResult> HandleAsync(
        UpdateCommentCommand command,
        CancellationToken cancellationToken = default)
    {
        var entry = await _repository.GetByIdAsync(JournalEntryId.From(command.EntryId), cancellationToken);
        if (entry is null)
            return UpdateCommentResult.NotFound();

        var updateResult = entry.UpdateComment(JournalCommentId.From(command.CommentId), command.Text);
        if (updateResult.IsFailure)
            return UpdateCommentResult.NotFound();

        await _repository.UpdateAsync(entry, cancellationToken);
        return UpdateCommentResult.Success(JournalEntryViewModel.From(entry));
    }
}
