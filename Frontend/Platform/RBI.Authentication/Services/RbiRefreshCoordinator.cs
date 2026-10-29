using Microsoft.Extensions.Hosting;

namespace RBI.Authentication.Internal;

internal sealed class RbiRefreshCoordinator : IHostedService, IDisposable
{
    private readonly object gate = new();
    private readonly Dictionary<string, IFlight> flights = new(StringComparer.Ordinal);
    private bool stopping;

    internal int ActiveOperations { get { lock (gate) return flights.Count; } }
    internal bool IsStopping { get { lock (gate) return stopping; } }
    internal bool IsSessionInvalidated(string sessionId)
    {
        lock (gate) return !flights.TryGetValue(sessionId, out var flight) || flight.SessionInvalidated;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) =>
        Task.WhenAll(CancelFlights()).WaitAsync(cancellationToken);

    public void Dispose() => CancelFlights();

    private Task[] CancelFlights()
    {
        lock (gate)
        {
            stopping = true;
            // Cancellation can synchronously finish/remove a flight; iterate a snapshot.
            var active = flights.Values.ToArray();
            foreach (var flight in active)
                if (!flight.Settled.Task.IsCompleted) flight.Cancellation.Cancel();
            return active.Select(flight => flight.Settled.Task).ToArray();
        }
    }

    internal ValueTask<T> RunAsync<T>(
        string sessionId,
        Func<CancellationToken, ValueTask<T>> action,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Flight<T> flight;
        bool owner;
        lock (gate)
        {
            if (stopping) throw new OperationCanceledException("The authentication host is stopping.");
            owner = !flights.TryGetValue(sessionId, out var existing);
            flight = owner ? new Flight<T>() : (Flight<T>)existing!;
            if (owner) flights.Add(sessionId, flight);
        }

        if (owner) _ = CompleteAsync(sessionId, flight, action);
        // Cancellation affects only this waiter; shared refresh has its own HTTP timeout.
        return new ValueTask<T>(flight.Completion.Task.WaitAsync(cancellationToken));
    }

    private async Task CompleteAsync<T>(string sessionId, Flight<T> flight, Func<CancellationToken, ValueTask<T>> action)
    {
        try
        {
            var result = await action(flight.Cancellation.Token);
            Finish(() => flight.Completion.TrySetResult(result));
        }
        catch (OperationCanceledException exception)
        {
            Finish(() => flight.Completion.TrySetCanceled(exception.CancellationToken));
        }
        catch (Exception exception)
        {
            Finish(() => flight.Completion.TrySetException(exception));
            // All request waiters may already have cancelled. Observe the shared fault
            // here too, without changing what remaining waiters receive.
            _ = flight.Completion.Task.Exception;
        }

        void Finish(Action complete)
        {
            lock (gate)
            {
                // No entry is removed while its refresh operation is still running.
                if (flights.TryGetValue(sessionId, out var current) && ReferenceEquals(current, flight))
                    flights.Remove(sessionId);
                complete();
                flight.Cancellation.Dispose();
                flight.Settled.TrySetResult();
            }
        }
    }

    internal void Forget(string sessionId)
    {
        lock (gate)
        {
            // Logout invalidates work, but must never remove a still-active gate.
            if (flights.TryGetValue(sessionId, out var flight))
            {
                flight.SessionInvalidated = true;
                flight.Cancellation.Cancel();
            }
        }
    }

    private interface IFlight
    {
        CancellationTokenSource Cancellation { get; }
        TaskCompletionSource Settled { get; }
        bool SessionInvalidated { get; set; }
    }

    private sealed class Flight<T> : IFlight
    {
        public CancellationTokenSource Cancellation { get; } = new();
        public TaskCompletionSource Settled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool SessionInvalidated { get; set; }
        internal TaskCompletionSource<T> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
