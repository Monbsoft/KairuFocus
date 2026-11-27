using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace KairuFocus.Infrastructure.Persistence;

internal static class UniquenessViolation
{
    // SQL Server error numbers for a unique index/constraint violation.
    private const int DuplicateKeyRowError = 2601;
    private const int DuplicateKeyConstraintError = 2627;

    // Restricted to the named constraints: any other unique constraint added later on a table must
    // not be silently reported as a uniqueness conflict. The names come from the EF configuration that
    // declares them, so the two cannot drift.
    public static bool IsOn(DbUpdateException exception, params string[] constraintNames) =>
        exception.InnerException is SqlException sqlException
        && sqlException.Number is DuplicateKeyRowError or DuplicateKeyConstraintError
        && constraintNames.Any(name => sqlException.Message.Contains(name, StringComparison.Ordinal));
}
