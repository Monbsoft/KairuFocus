namespace KairuFocus.Domain.Community;

/// <summary>Where a group session stands at a given instant.</summary>
public enum GroupPhase
{
    /// <summary>The current step is running.</summary>
    Running,

    /// <summary>The current step is over and the host has not launched the next one yet.</summary>
    WaitingForHost,

    /// <summary>The group is over.</summary>
    Ended
}
