using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace KairuFocus.Application.Journal.Commands.RemoveComment;

public sealed record RemoveCommentCommand(Guid EntryId, Guid CommentId) : ICommand<RemoveCommentResult>;
