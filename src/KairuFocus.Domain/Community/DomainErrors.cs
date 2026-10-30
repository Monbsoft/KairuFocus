namespace KairuFocus.Domain.Community;

// Suggested alias at call sites to avoid conflicts with other bounded contexts:
// using CommunityErrors = KairuFocus.Domain.Community.DomainErrors;
public static class DomainErrors
{
    public static class Community
    {
        public const string InvalidName =
            "A profile name must be 3 to 39 characters long, contain only ASCII letters, digits, '-' or '_', " +
            "and start with a letter or a digit.";
        public const string ReservedName = "This profile name is reserved.";
        public const string NameTaken = "This profile name is already taken.";
        public const string LoginOfAnotherUser = "This profile name is the GitHub login of another user.";
        public const string AlreadyMember = "The user is already a member of the community.";
        public const string NotMember = "The user is not a member of the community.";
        public const string SprintNotJoinable = "This sprint cannot be joined.";
        public const string CannotJoinOwnSprint = "A member cannot join their own sprint.";
        public const string NotSprintHost = "Only the host of the sprint can open or close it.";
        public const string NoActiveSprint = "No sprint is currently active.";
        public const string UserNotFound = "User not found.";
    }
}
