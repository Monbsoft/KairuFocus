namespace Kairudev.Application.Journal.Commands.RemoveComment;

public sealed record RemoveCommentCommand(Guid EntryId, Guid CommentId);
