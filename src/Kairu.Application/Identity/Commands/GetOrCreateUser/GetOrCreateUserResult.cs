using Kairu.Domain.Identity;

namespace Kairu.Application.Identity.Commands.GetOrCreateUser;

public sealed record GetOrCreateUserResult(UserId UserId, string Login, string DisplayName);
