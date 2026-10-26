namespace KairuFocus.Domain.Gamification;

// Suggested alias at call sites to avoid conflicts with other bounded contexts:
// using GamificationErrors = KairuFocus.Domain.Gamification.DomainErrors;
public static class DomainErrors
{
    public static class Gamification
    {
        public const string SessionNotFound = "Session not found for XP credit.";

        public const string AlreadyParticipating = "The user already participates in the community leaderboard.";
        public const string NotParticipating = "The user does not participate in the community leaderboard.";
        public const string PseudonymPoolExhausted = "No free pseudonym could be drawn. Please retry in a moment.";
        public const string InvalidPseudonym =
            "A pseudonym must start with an uppercase ASCII letter, contain only ASCII letters, " +
            "optionally end with one or two digits, and be at most 60 characters long.";
    }
}
