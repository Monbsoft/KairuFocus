using KairuFocus.Domain.Gamification;
using KairuFocus.Domain.Identity;
using KairuFocus.Infrastructure.Persistence.Configurations;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace KairuFocus.Infrastructure.Persistence.Repositories;

internal sealed class EfCoreLeaderboardProfileRepository : ILeaderboardProfileRepository
{
    // SQL Server error numbers for a unique index/constraint violation.
    private const int DuplicateKeyRowError = 2601;
    private const int DuplicateKeyConstraintError = 2627;

    private readonly KairuFocusDbContext _context;

    public EfCoreLeaderboardProfileRepository(KairuFocusDbContext context)
    {
        _context = context;
    }

    public async Task<LeaderboardProfile?> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken = default) =>
        await _context.LeaderboardProfiles.FindAsync([userId], cancellationToken);

    // The database collation is case-insensitive, like the unique index; the generator always
    // produces the same casing, so an equality is enough.
    public async Task<bool> IsPseudonymTakenAsync(Pseudonym pseudonym, CancellationToken cancellationToken = default) =>
        await _context.LeaderboardProfiles.AnyAsync(p => p.Pseudonym == pseudonym, cancellationToken);

    public async Task<bool> TryAddAsync(LeaderboardProfile profile, CancellationToken cancellationToken = default)
    {
        await _context.LeaderboardProfiles.AddAsync(profile, cancellationToken);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException ex) when (IsUniquenessViolation(
            ex, LeaderboardProfileConfiguration.PrimaryKeyName, LeaderboardProfileConfiguration.PseudonymIndexName))
        {
            // A concurrent join of the same user (key) or a concurrent draw of the same pseudonym.
            // Detach the orphan so the scoped DbContext stays usable: the caller re-reads the profile
            // by UserId right after, and FindAsync must hit the database, not this rejected entity.
            _context.Entry(profile).State = EntityState.Detached;
            return false;
        }
    }

    public async Task<bool> TryUpdateAsync(LeaderboardProfile profile, CancellationToken cancellationToken = default)
    {
        _context.LeaderboardProfiles.Update(profile);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            // The UPDATE affected no row: the profile was deleted concurrently (the user left from another
            // tab). Detach the rejected change so the scoped DbContext stays usable: the caller re-reads the
            // profile by UserId right after, and FindAsync must hit the database, not this rejected entity.
            _context.Entry(profile).State = EntityState.Detached;
            return false;
        }
        catch (DbUpdateException ex) when (IsUniquenessViolation(ex, LeaderboardProfileConfiguration.PseudonymIndexName))
        {
            // The drawn pseudonym was taken concurrently. Detach the rejected change (same reason as above).
            _context.Entry(profile).State = EntityState.Detached;
            return false;
        }
    }

    public async Task<bool> TryRemoveAsync(LeaderboardProfile profile, CancellationToken cancellationToken = default)
    {
        _context.LeaderboardProfiles.Remove(profile);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            // The DELETE affected no row: a concurrent request (another tab) already removed the profile.
            // Detach the rejected change so the scoped DbContext stays usable.
            _context.Entry(profile).State = EntityState.Detached;
            return false;
        }
    }

    // Restricted to the named constraints: any other unique constraint added later on this table must
    // not be silently reported as a uniqueness conflict. The names come from the EF configuration that
    // declares them, so the two cannot drift.
    private static bool IsUniquenessViolation(DbUpdateException exception, params string[] constraintNames) =>
        exception.InnerException is SqlException sqlException
        && sqlException.Number is DuplicateKeyRowError or DuplicateKeyConstraintError
        && constraintNames.Any(name => sqlException.Message.Contains(name, StringComparison.Ordinal));
}
