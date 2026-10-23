using KairuFocus.Web.Services;

namespace KairuFocus.Web.Helpers;

/// <summary>
/// State of one independent section of a page: its last data, whether the last load failed,
/// and whether a load is in flight. Each section loads, fails and retries on its own.
/// <para>
/// A reload keeps the previous data on screen. The skeleton only replaces it once the load has
/// lasted too long (<see cref="MarkSlow"/>), or when there is nothing to keep (first load, retry).
/// </para>
/// Not thread-safe by design: Blazor WebAssembly runs components on a single thread.
/// </summary>
public sealed class LoadZone<T> where T : class
{
    // Cancel() only: no timer and no wait handle is ever created from this source, so there is
    // nothing to release, and a disposed source could break a delay still holding its token.
    private CancellationTokenSource? _cts;

    public T? Data { get; private set; }

    /// <summary>The period the current <see cref="Data"/> belongs to, when the section depends on one.</summary>
    public CommunityPeriod? Period { get; private set; }

    public bool Failed { get; private set; }

    public bool Busy { get; private set; }

    public bool Slow { get; private set; }

    /// <summary>True when a skeleton must be shown instead of the content.</summary>
    public bool ShowSkeleton => Busy && (Data is null || Slow);

    /// <summary>True when the error state must be shown.</summary>
    public bool ShowError => !Busy && Failed;

    /// <summary>True during a load that has nothing yet to show (first load or retry).</summary>
    public bool IsInitialLoad => Busy && Data is null;

    /// <summary>Starts a load: cancels the one in flight and returns the token of the new one.</summary>
    public CancellationToken Begin()
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        Busy = true;
        Slow = false;
        Failed = false;
        return _cts.Token;
    }

    /// <summary>Switches a still-running load to the skeleton. Ignored when it has already ended.</summary>
    public void MarkSlow(CancellationToken loadToken)
    {
        if (Busy && !loadToken.IsCancellationRequested) Slow = true;
    }

    /// <summary>Ends the load. A null result is a failure and drops the previous data.</summary>
    public void Complete(T? data, CommunityPeriod? period = null)
    {
        Data = data;
        Period = data is null ? null : period;
        Failed = data is null;
        Busy = false;
        Slow = false;
    }

    /// <summary>Abandons the load in flight (page left).</summary>
    public void Cancel()
    {
        _cts?.Cancel();
        _cts = null;
    }
}
