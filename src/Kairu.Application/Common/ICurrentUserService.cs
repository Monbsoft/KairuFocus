using Kairu.Domain.Identity;

namespace Kairu.Application.Common;

public interface ICurrentUserService
{
    UserId CurrentUserId { get; }
}
