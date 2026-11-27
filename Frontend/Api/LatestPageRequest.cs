namespace RBBH.RelatedPartiesRegister.Web.Api;

/// <summary>Cancels superseded page requests, including searches still waiting for the typing delay.</summary>
internal sealed class LatestPageRequest
{
    private CancellationTokenSource? _current;

    public async Task RunAsync(CancellationToken lifetime, bool debounce,
        Func<CancellationToken, Task> load, Action<RegistryApiException> failure, Action finish)
    {
        _current?.Cancel();
        using var request = CancellationTokenSource.CreateLinkedTokenSource(lifetime);
        _current = request;
        try
        {
            if (debounce) await Task.Delay(250, request.Token);
            await load(request.Token);
        }
        catch (OperationCanceledException) when (request.IsCancellationRequested) { }
        catch (RegistryApiException exception)
        {
            if (!request.IsCancellationRequested) failure(exception);
        }
        finally
        {
            if (ReferenceEquals(_current, request))
            {
                _current = null;
                finish();
            }
        }
    }
}
