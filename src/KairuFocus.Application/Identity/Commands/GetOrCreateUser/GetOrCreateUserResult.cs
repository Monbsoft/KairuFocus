using KairuFocus.Domain.Identity;

namespace KairuFocus.Application.Identity.Commands.GetOrCreateUser;

public sealed record GetOrCreateUserResult(UserId UserId, string Login, string DisplayName);
