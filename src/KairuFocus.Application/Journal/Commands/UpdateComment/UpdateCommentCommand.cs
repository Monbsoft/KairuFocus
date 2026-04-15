using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace KairuFocus.Application.Journal.Commands.UpdateComment;

public sealed record UpdateCommentCommand(Guid EntryId, Guid CommentId, string Text) : ICommand<UpdateCommentResult>;
