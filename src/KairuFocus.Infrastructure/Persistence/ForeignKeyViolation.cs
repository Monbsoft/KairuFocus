using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace KairuFocus.Infrastructure.Persistence;

internal static class ForeignKeyViolation
{
    // SQL Server error number of a statement conflicting with a FOREIGN KEY (or CHECK) constraint.
    private const int ConstraintConflictError = 547;

    // Restricted to the named constraints, as UniquenessViolation: the number also covers CHECK constraints and
    // any other foreign key, which must not be silently swallowed.
    public static bool IsOn(DbUpdateException exception, params string[] constraintNames) =>
        exception.InnerException is SqlException sqlException
        && sqlException.Number == ConstraintConflictError
        && constraintNames.Any(name => sqlException.Message.Contains(name, StringComparison.Ordinal));
}
