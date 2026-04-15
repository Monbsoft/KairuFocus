using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace KairuFocus.Application.Journal.Commands.AddComment;

public sealed record AddCommentCommand(Guid EntryId, string Text) : ICommand<AddCommentResult>;
