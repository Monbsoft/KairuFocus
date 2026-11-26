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
            await _context.SaveChangesAsync(ct);
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
            await _context.SaveChangesAsync(ct);
        }
    }
}
