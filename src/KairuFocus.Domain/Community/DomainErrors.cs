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
        public const string AlreadyMember = "The user is already a member of the community.";
        public const string NotMember = "The user is not a member of the community.";
        public const string SprintNotJoinable = "This sprint cannot be joined.";
        public const string CannotJoinOwnSprint = "A member cannot join their own sprint.";
        public const string NotSprintHost = "Only the host of the sprint can open or close it.";
        public const string NoActiveSprint = "No sprint is currently active.";
        public const string UserNotFound = "User not found.";
        public const string GroupEnded = "This group session is over.";
        public const string GroupWaitingForHost = "The group is waiting for its host to launch the next step.";
        public const string GroupStepRunning = "A step of the group is still running.";
        public const string GroupClosed = "This group is closed to new members.";
        public const string AlreadyInGroup = "The user is already in this group.";
        public const string NotInGroup = "The user is not in this group.";
        public const string GroupNotAlone = "The group still has members.";
        public const string InvalidGroupStep = "A group step must be numbered from 1 and end after it starts.";
    }
}
