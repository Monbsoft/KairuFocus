using System.Data;
using System.Data.Common;
using KairuFocus.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace KairuFocus.Infrastructure.Tests.Persistence.TestSupport;

/// <summary>
/// Captures the SQL that the SQL Server provider generates for a repository call, without any database:
/// opening the connection is suppressed, and the first command is recorded then aborted before execution
/// (same technique as <c>UserSettingsConfigurationTests</c>). A capture also proves that the LINQ query
/// translates to SQL: an untranslatable query fails before any command is created.
/// </summary>
internal static class SqlCommandCapture
{
    private const string FakeConnectionString =
        "Server=tests.invalid;Database=tests;Integrated Security=true;Encrypt=false;Connect Timeout=1";

    public static async Task<CapturedCommand> CaptureAsync(Func<KairuFocusDbContext, Task> query)
    {
        var capture = new CommandCaptureInterceptor();
        var options = new DbContextOptionsBuilder<KairuFocusDbContext>()
            .UseSqlServer(FakeConnectionString)
            .AddInterceptors(new SuppressConnectionOpeningInterceptor(), capture)
            .Options;
        await using var context = new KairuFocusDbContext(options);

        // The interceptor aborts the command once captured, so an exception is expected here.
        var outcome = await Record.ExceptionAsync(() => query(context));

        Assert.True(capture.Command is not null, $"No command was captured. Query outcome: {outcome}");
        return capture.Command;
    }

    private sealed class CommandCapturedException() : Exception("Command captured: execution aborted on purpose.");

    /// <summary>Never lets EF open the (non-existent) connection.</summary>
    private sealed class SuppressConnectionOpeningInterceptor : DbConnectionInterceptor
    {
        public override InterceptionResult ConnectionOpening(
            DbConnection connection,
            ConnectionEventData eventData,
            InterceptionResult result)
            => InterceptionResult.Suppress();

        public override ValueTask<InterceptionResult> ConnectionOpeningAsync(
            DbConnection connection,
            ConnectionEventData eventData,
            InterceptionResult result,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult(InterceptionResult.Suppress());
    }

    /// <summary>
    /// Records the first command EF is about to send, then aborts it. Covers readers and scalars:
    /// which of the two an aggregate (COUNT, SUM, MIN) goes through is a provider detail.
    /// </summary>
    private sealed class CommandCaptureInterceptor : DbCommandInterceptor
    {
        public CapturedCommand? Command { get; private set; }

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
            => throw Capture(command);

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
            => throw Capture(command);

        public override InterceptionResult<object> ScalarExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<object> result)
            => throw Capture(command);

        public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<object> result,
            CancellationToken cancellationToken = default)
            => throw Capture(command);

        private CommandCapturedException Capture(DbCommand command)
        {
            Command ??= new CapturedCommand(
                command.CommandText,
                command.Parameters
                    .Cast<DbParameter>()
                    .Select(p => new CapturedParameter(p.ParameterName, p.Value, p.DbType))
                    .ToList());
            return new CommandCapturedException();
        }
    }
}

internal sealed record CapturedParameter(string Name, object? Value, DbType DbType);

internal sealed record CapturedCommand(string Text, IReadOnlyList<CapturedParameter> Parameters)
{
    /// <summary>
    /// Expressions of the outermost SELECT list: the text between the first <c>SELECT</c> and the first
    /// <c>FROM</c>, split on top-level commas. Enough for the shapes asserted here (no subquery in a
    /// select list); a subquery in the FROM clause is not part of the outer projection.
    /// </summary>
    public IReadOnlyList<string> OuterProjection
    {
        get
        {
            var selectStart = Text.IndexOf("SELECT ", StringComparison.Ordinal);
            Assert.True(selectStart >= 0, $"No SELECT found in: {Text}");
            var listStart = selectStart + "SELECT ".Length;
            var listEnd = Text.IndexOf("FROM", listStart, StringComparison.Ordinal);
            Assert.True(listEnd >= 0, $"No FROM found in: {Text}");

            return SplitTopLevel(Text[listStart..listEnd]);
        }
    }

    private static List<string> SplitTopLevel(string selectList)
    {
        var expressions = new List<string>();
        var depth = 0;
        var start = 0;
        for (var i = 0; i < selectList.Length; i++)
        {
            switch (selectList[i])
            {
                case '(':
                    depth++;
                    break;
                case ')':
                    depth--;
                    break;
                case ',' when depth == 0:
                    expressions.Add(selectList[start..i].Trim());
                    start = i + 1;
                    break;
            }
        }

        expressions.Add(selectList[start..].Trim());
        return expressions;
    }
}
