using Kairudev.Application.Journal.Common;
using Kairudev.Domain.Journal;

namespace Kairudev.Application.Journal.Commands.AddComment;

public sealed class AddCommentCommandHandler
{
    private readonly IJournalEntryRepository _repository;

    public AddCommentCommandHandler(IJournalEntryRepository repository)
    {
        _repository = repository;
    }

    public async Task<AddCommentResult> HandleAsync(
        AddCommentCommand command,
        CancellationToken cancellationToken = default)
    {
        var entry = await _repository.GetByIdAsync(JournalEntryId.From(command.EntryId), cancellationToken);
        if (entry is null)
            return AddCommentResult.NotFound();

        var addResult = entry.AddComment(command.Text);
        if (addResult.IsFailure)
            return AddCommentResult.Validation(addResult.Error);

        await _repository.UpdateAsync(entry, cancellationToken);
        return AddCommentResult.Success(JournalEntryViewModel.From(entry));
    }
}
