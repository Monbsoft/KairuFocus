using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace Kairu.Application.Journal.Commands.RemoveComment;

public sealed record RemoveCommentCommand(Guid EntryId, Guid CommentId) : ICommand<RemoveCommentResult>;
