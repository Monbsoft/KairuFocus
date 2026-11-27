namespace KairuFocus.Domain.Community;

/// <summary>
/// The closed list of ambiances a member can choose from. Codes are unique (ordinal) and every video is a
/// YouTube video: the URL is validated at construction so that no other host can be embedded.
/// </summary>
public sealed class AmbianceCatalogue
{
    public const string YouTubeVideoPrefix = "https://www.youtube.com/watch?v=";
    public const int MaxCodeLength = 40;

    // Empty until the real list is provided: no URL is invented here.
    private static readonly Ambiance[] DefaultEntries = [];

    private readonly Ambiance[] _entries;
    private readonly Dictionary<string, Ambiance> _byCode;

    /// <summary>Guards throw: a malformed catalogue is a programmer error, not part of the normal domain flow.</summary>
    public AmbianceCatalogue(IEnumerable<Ambiance> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        _entries = entries.ToArray();
        _byCode = new Dictionary<string, Ambiance>(_entries.Length, StringComparer.Ordinal);

        foreach (var entry in _entries)
        {
            if (entry is null)
                throw new ArgumentException("An ambiance cannot be null.", nameof(entries));
            if (string.IsNullOrWhiteSpace(entry.Code))
                throw new ArgumentException("An ambiance code cannot be empty.", nameof(entries));
            if (entry.Code.Length > MaxCodeLength)
                throw new ArgumentException($"The ambiance code '{entry.Code}' is longer than {MaxCodeLength} characters.", nameof(entries));
            if (entry.VideoUrl is null || !entry.VideoUrl.StartsWith(YouTubeVideoPrefix, StringComparison.Ordinal))
                throw new ArgumentException($"The ambiance '{entry.Code}' must point to a YouTube video ({YouTubeVideoPrefix}...).", nameof(entries));
            if (!_byCode.TryAdd(entry.Code, entry))
                throw new ArgumentException($"The ambiance code '{entry.Code}' is declared twice.", nameof(entries));
        }
    }

    public static AmbianceCatalogue Default { get; } = new(DefaultEntries);

    /// <summary>The ambiances, in declaration order.</summary>
    public IReadOnlyList<Ambiance> All => _entries;

    /// <summary>The ambiance of <paramref name="code"/> (ordinal comparison), or null when null, empty or unknown.</summary>
    public Ambiance? Find(string? code) =>
        code is not null && _byCode.TryGetValue(code, out var ambiance) ? ambiance : null;
}
