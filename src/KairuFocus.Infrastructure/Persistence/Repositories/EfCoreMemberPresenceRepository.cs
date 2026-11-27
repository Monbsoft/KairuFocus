using KairuFocus.Domain.Community;
using KairuFocus.Domain.Identity;
using KairuFocus.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace KairuFocus.Infrastructure.Persistence.Repositories;

internal sealed class EfCoreMemberPresenceRepository : IMemberPresenceRepository
{
    private readonly KairuFocusDbContext _context;

    public EfCoreMemberPresenceRepository(KairuFocusDbContext context)
    {
        _context = context;
    }

    public async Task RecordSeenAsync(UserId userId, DateTime seenAtUtc, CancellationToken ct = default)
    {
        var presence = await _context.MemberPresences.FindAsync([userId], ct);
        if (presence is null)
        {
            presence = MemberPresence.Record(userId, seenAtUtc);
            await _context.MemberPresences.AddAsync(presence, ct);
        }
        else
        {
            presence.SeenAt(seenAtUtc);
        }

        try
        {
            await SaveOrForgetAsync(presence, ct);
        }
        catch (DbUpdateException ex) when (UniquenessViolation.IsOn(ex, MemberPresenceConfiguration.PrimaryKeyName))
        {
            // Two first signals of the same member raced and the other one won the insert. Detach the
            // rejected row so the scoped DbContext stays usable, read the winner back and move it forward:
            // the later of the two instants is kept, and the race never reaches the caller.
            _context.Entry(presence).State = EntityState.Detached;
            var existing = await _context.MemberPresences.FindAsync([userId], ct);
            if (existing is null)
                return;

            existing.SeenAt(seenAtUtc);
            await SaveOrForgetAsync(existing, ct);
        }
    }

    /// <summary>
    /// Saves the presence, unless the member left the community meanwhile: the update finds no row (the cascade
    /// deleted it) or the insert no profile. The signal is then dropped (a lost signal is harmless: the next read
    /// finds no member) and the rejected row detached, so that the scoped DbContext stays usable.
    /// </summary>
    private async Task SaveOrForgetAsync(MemberPresence presence, CancellationToken ct)
    {
        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex) when (ex.Entries.All(e => e.Entity is MemberPresence))
        {
            _context.Entry(presence).State = EntityState.Detached;
        }
        catch (DbUpdateException ex) when (ForeignKeyViolation.IsOn(ex, MemberPresenceConfiguration.ProfileForeignKeyName))
        {
            _context.Entry(presence).State = EntityState.Detached;
        }
    }
}
