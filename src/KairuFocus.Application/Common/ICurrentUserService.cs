using KairuFocus.Domain.Identity;

namespace KairuFocus.Application.Common;

public interface ICurrentUserService
{
    UserId CurrentUserId { get; }
}
