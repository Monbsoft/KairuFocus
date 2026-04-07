using Kairu.Application.Journal.Common;
using Kairu.Domain.Journal;
using Microsoft.Extensions.Logging;
using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace Kairu.Application.Journal.Commands.AddComment;

public sealed class AddCommentCommandHandler : ICommandHandler<AddCommentCommand, AddCommentResult>
{
    private readonly IJournalEntryRepository _repository;
    private readonly ILogger<AddCommentCommandHandler> _logger;

    public AddCommentCommandHandler(
        IJournalEntryRepository repository,
        ILogger<AddCommentCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<AddCommentResult> Handle(
        AddCommentCommand command,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Adding comment to journal entry {EntryId}", command.EntryId);

        var entry = await _repository.GetByIdAsync(JournalEntryId.From(command.EntryId), cancellationToken);
        if (entry is null)
        {
            _logger.LogWarning("Journal entry {EntryId} not found", command.EntryId);
            return AddCommentResult.NotFound();
        }

        var addResult = entry.AddComment(command.Text);
        if (addResult.IsFailure)
            return AddCommentResult.Validation(addResult.Error);

        await _repository.UpdateAsync(entry, cancellationToken);
        _logger.LogInformation("Comment added to journal entry {EntryId}", command.EntryId);
        return AddCommentResult.Success(JournalEntryViewModel.From(entry));
    }
}
