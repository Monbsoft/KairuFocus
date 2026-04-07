using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace Kairu.Application.Journal.Commands.AddComment;

public sealed record AddCommentCommand(Guid EntryId, string Text) : ICommand<AddCommentResult>;
