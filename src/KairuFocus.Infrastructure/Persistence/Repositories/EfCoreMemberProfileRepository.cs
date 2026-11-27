using KairuFocus.Domain.Community;
using KairuFocus.Domain.Identity;
using KairuFocus.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace KairuFocus.Infrastructure.Persistence.Repositories;

internal sealed class EfCoreMemberProfileRepository : IMemberProfileRepository
{
    private readonly KairuFocusDbContext _context;

    public EfCoreMemberProfileRepository(KairuFocusDbContext context)
    {
        _context = context;
    }

    public async Task<MemberProfile?> GetByUserIdAsync(UserId userId, CancellationToken ct = default) =>
        await _context.MemberProfiles.FindAsync([userId], ct);

    // Compared on the stored key (lowercase): uniqueness does not depend on the database collation.
    public async Task<bool> IsNameTakenAsync(ProfileName name, CancellationToken ct = default)
    {
        var key = name.Key;
        return await _context.MemberProfiles.AnyAsync(p => p.NameKey == key, ct);
    }

    public async Task<bool> TryAddAsync(MemberProfile profile, CancellationToken ct = default)
    {
        await _context.MemberProfiles.AddAsync(profile, ct);
        try
        {
            await _context.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException ex) when (UniquenessViolation.IsOn(
            ex, MemberProfileConfiguration.PrimaryKeyName, MemberProfileConfiguration.NameKeyIndexName))
        {
            // A concurrent join of the same user (key) or a concurrent claim of the same name.
            // Detach the orphan so the scoped DbContext stays usable: the caller re-reads the profile
            // by UserId right after, and FindAsync must hit the database, not this rejected entity.
            _context.Entry(profile).State = EntityState.Detached;
            return false;
        }
    }

    public async Task<bool> TryUpdateAsync(MemberProfile profile, CancellationToken ct = default)
    {
        _context.MemberProfiles.Update(profile);
        try
        {
            await _context.SaveChangesAsync(ct);
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
        catch (DbUpdateException ex) when (UniquenessViolation.IsOn(ex, MemberProfileConfiguration.NameKeyIndexName))
        {
            // The new name was taken concurrently. Detach the rejected change (same reason as above).
            _context.Entry(profile).State = EntityState.Detached;
            return false;
        }
    }

    public async Task<bool> TryRemoveAsync(MemberProfile profile, CancellationToken ct = default)
    {
        _context.MemberProfiles.Remove(profile);
        try
        {
            await _context.SaveChangesAsync(ct);
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
}
