namespace RBI.Forms;

/// <summary>
/// Prevents concurrent form submissions while exposing the current submission state.
/// </summary>
public sealed class RbiFormSubmissionState
{
    private int isSubmitting;

    /// <summary>
    /// Gets whether a submission is currently running.
    /// </summary>
    public bool IsSubmitting => Volatile.Read(ref isSubmitting) == 1;

    /// <summary>
    /// Runs a submission unless another submission is already in progress.
    /// </summary>
    /// <param name="submission">The application-owned asynchronous submission.</param>
    /// <returns><see langword="true"/> when the submission ran; otherwise <see langword="false"/>.</returns>
    public async Task<bool> SubmitAsync(Func<Task> submission)
    {
        ArgumentNullException.ThrowIfNull(submission);

        if (Interlocked.CompareExchange(ref isSubmitting, 1, 0) != 0)
            return false;

        try
        {
            await submission();
            return true;
        }
        finally
        {
            Volatile.Write(ref isSubmitting, 0);
        }
    }
}
