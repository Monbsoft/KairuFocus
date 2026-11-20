namespace KairuFocus.Domain.Community;

public enum GroupSessionEndReason
{
    /// <summary>The host stopped the group.</summary>
    Stopped,

    /// <summary>The host did not launch the next step within <see cref="GroupRules.HostWaitLimit"/>.</summary>
    HostAbsent,

    /// <summary>The host stopped waiting after the last member left.</summary>
    HostAlone,

    /// <summary>The host left the community.</summary>
    HostLeftCommunity
}
