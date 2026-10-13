using System.Data.Common;
using KairuFocus.Domain.Identity;
using KairuFocus.Domain.Settings;
using KairuFocus.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace KairuFocus.Infrastructure.Tests.Persistence;

/// <summary>
/// Guards the store-default configuration of <see cref="UserSettings"/>: a CLR default value
/// (<see cref="RingtonePreference.None"/>, volume 0) is a valid user choice and must reach the database.
/// No database is ever contacted: building the model does not connect, opening the connection is
/// suppressed, and the INSERT command is captured then aborted before execution.
/// </summary>
public sealed class UserSettingsConfigurationTests
{
    private const string FakeConnectionString =
        "Server=tests.invalid;Database=tests;Integrated Security=true;Encrypt=false;Connect Timeout=1";

    [Fact]
    public async Task Should_SendRingtonePreferenceNone_When_UserSettingsIsInsertedWithNone()
    {
        var insert = await CaptureInsertAsync(RingtonePreference.None, alarmVolume: 80);

        Assert.Contains("[RingtonePreference]", insert.ColumnList);
        Assert.Contains<object?>("None", insert.ParameterValues);
    }

    [Fact]
    public async Task Should_SendAlarmVolumeZero_When_UserSettingsIsInsertedWithVolumeZero()
    {
        // A ringtone other than None isolates the variable under test.
        var insert = await CaptureInsertAsync(RingtonePreference.Bird, alarmVolume: 0);

        Assert.Contains("[AlarmVolume]", insert.ColumnList);
        Assert.Contains<object?>(0, insert.ParameterValues);
    }

    // Validates the whole model, not only UserSettings: EF raises this warning for any bool or enum property
    // that has a store default and no sentinel. It does not cover AlarmVolume (int), hence the test above.
    [Fact]
    public void Should_NotRaiseDefaultValueSentinelWarning_When_ModelIsBuilt()
    {
        var options = new DbContextOptionsBuilder<KairuFocusDbContext>()
            .UseSqlServer(FakeConnectionString)
            .ConfigureWarnings(w => w.Throw(RelationalEventId.BoolWithDefaultWarning))
            .Options;
        using var context = new KairuFocusDbContext(options);

        var exception = Record.Exception(() => context.Model);

        Assert.Null(exception);
    }

    private static async Task<CapturedInsert> CaptureInsertAsync(RingtonePreference ringtonePreference, int alarmVolume)
    {
        var capture = new CommandCaptureInterceptor();
        var options = new DbContextOptionsBuilder<KairuFocusDbContext>()
            .UseSqlServer(FakeConnectionString)
            .AddInterceptors(new SuppressConnectionOpeningInterceptor(), capture)
            .Options;
        await using var context = new KairuFocusDbContext(options);
        context.Add(UserSettings.Reconstitute(
            UserId.From(Guid.NewGuid()),
            ThemePreference.System,
            ringtonePreference,
            alarmVolume,
            jiraBaseUrl: null,
            jiraEmail: null,
            jiraApiToken: null));

        // The interceptor aborts the save once the command is captured, so an exception is expected here.
        var saveOutcome = await Record.ExceptionAsync(() => context.SaveChangesAsync());

        Assert.True(capture.CommandText is not null, $"No command was captured. SaveChangesAsync outcome: {saveOutcome}");
        return new CapturedInsert(ExtractInsertColumnList(capture.CommandText), capture.ParameterValues);
    }

    private static string ExtractInsertColumnList(string commandText)
    {
        const string insertPrefix = "INSERT INTO [UserSettings] (";

        var prefixStart = commandText.IndexOf(insertPrefix, StringComparison.Ordinal);
        Assert.True(prefixStart >= 0, $"No INSERT into [UserSettings] found in: {commandText}");

        var listStart = prefixStart + insertPrefix.Length;
        var listEnd = commandText.IndexOf(')', listStart);
        return commandText[listStart..listEnd];
    }

    private sealed record CapturedInsert(string ColumnList, IReadOnlyList<object?> ParameterValues);

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

    /// <summary>Records the command EF is about to send, then aborts it instead of executing it.</summary>
    private sealed class CommandCaptureInterceptor : DbCommandInterceptor
    {
        public string? CommandText { get; private set; }

        public IReadOnlyList<object?> ParameterValues { get; private set; } = [];

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
        {
            Capture(command);
            throw new CommandCapturedException();
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Capture(command);
            throw new CommandCapturedException();
        }

        private void Capture(DbCommand command)
        {
            CommandText = command.CommandText;
            ParameterValues = command.Parameters.Cast<DbParameter>().Select(p => p.Value).ToList();
        }
    }
}
