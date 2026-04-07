using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace Kairu.Application.Journal.Commands.UpdateComment;

public sealed record UpdateCommentCommand(Guid EntryId, Guid CommentId, string Text) : ICommand<UpdateCommentResult>;
