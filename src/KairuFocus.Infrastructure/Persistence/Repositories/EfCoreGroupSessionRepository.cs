using KairuFocus.Domain.Community;
using KairuFocus.Domain.Identity;
using KairuFocus.Infrastructure.Persistence.Configurations;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace KairuFocus.Infrastructure.Persistence.Repositories;

internal sealed class EfCoreGroupSessionRepository : IGroupSessionRepository
{
    // SQL Server error numbers for a unique index/constraint violation.
    private const int DuplicateKeyRowError = 2601;
    private const int DuplicateKeyConstraintError = 2627;

    private readonly KairuFocusDbContext _context;

    public EfCoreGroupSessionRepository(KairuFocusDbContext context)
    {
        _context = context;
    }

    public async Task<GroupSession?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _context.GroupSessions
            .Include(GroupSessionConfiguration.MembersField)
            .FirstOrDefaultAsync(g => g.Id == id, ct);

    // A group not ended comes first: a stopped group stays attached to its host and members, and the step its
    // host had launched may have started after the step of the group the user joined since. Two reads rather
    // than a second ordering key: the in-memory provider cannot translate a ThenBy on the owned current step.
    public async Task<GroupSession?> FindLatestForUserAsync(UserId userId, CancellationToken ct = default) =>
        await LatestAsync(ForUser(userId).Where(g => g.EndedAtUtc == null), ct)
        ?? await LatestAsync(ForUser(userId).Where(g => g.EndedAtUtc != null), ct);

    // The host and the members' users are private fields: they are reached through EF.Property for the
    // filter only, never projected (ADR-026).
    private IQueryable<GroupSession> ForUser(UserId userId) =>
        _context.GroupSessions
            .Include(GroupSessionConfiguration.MembersField)
            .Where(g => EF.Property<UserId>(g, GroupSessionConfiguration.HostField) == userId
                        || EF.Property<IEnumerable<GroupMember>>(g, GroupSessionConfiguration.MembersField)
                            .Any(m => EF.Property<UserId>(m, GroupMemberConfiguration.UserField) == userId
                                      && m.LeftAtUtc == null));

    private static Task<GroupSession?> LatestAsync(IQueryable<GroupSession> groups, CancellationToken ct) =>
        groups.OrderByDescending(g => g.CurrentStep.StartedAtUtc).FirstOrDefaultAsync(ct);

    public async Task<bool> TryAddAsync(GroupSession group, CancellationToken ct = default)
    {
        await _context.GroupSessions.AddAsync(group, ct);
        try
        {
            await _context.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException ex) when (IsIdConflict(ex))
        {
            // A concurrent request created the group with the same id. Detach the orphan so the scoped
            // DbContext stays usable.
            Detach(group);
            return false;
        }
    }

    // SaveChanges on the tracked aggregate, never Update(): only the modified columns are written, and
    // Join / Leave touch member rows without moving the Version of the group row.
    public async Task<bool> TryUpdateAsync(GroupSession group, CancellationToken ct = default)
    {
        try
        {
            await _context.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            // Another request wrote the group row first (stale Version). Detach the rejected change so the
            // scoped DbContext stays usable: the caller re-reads the group right after, and the read must
            // hit the database, not this rejected aggregate.
            Detach(group);
            return false;
        }
    }

    private void Detach(GroupSession group)
    {
        var entry = _context.Entry(group);
        foreach (var member in entry.Collection(GroupSessionConfiguration.MembersField).CurrentValue?.Cast<GroupMember>() ?? [])
            _context.Entry(member).State = EntityState.Detached;

        entry.State = EntityState.Detached;
    }

    private static bool IsIdConflict(DbUpdateException exception) =>
        exception.InnerException is SqlException sqlException
        && sqlException.Number is DuplicateKeyRowError or DuplicateKeyConstraintError
        && sqlException.Message.Contains(GroupSessionConfiguration.PrimaryKeyName, StringComparison.Ordinal);
}
