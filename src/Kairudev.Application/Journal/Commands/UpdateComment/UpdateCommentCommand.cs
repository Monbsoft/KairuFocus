namespace Kairudev.Application.Journal.Commands.UpdateComment;

public sealed record UpdateCommentCommand(Guid EntryId, Guid CommentId, string Text);
