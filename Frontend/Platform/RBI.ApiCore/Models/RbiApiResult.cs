namespace RBI.ApiCore;

/// <summary>Represents the success or failure of an API operation without a response value.</summary>
public sealed class RbiApiResult
{
    private RbiApiResult(bool isSuccess, RbiApiError? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>Gets whether the operation completed successfully.</summary>
    public bool IsSuccess { get; }

    /// <summary>Gets the normalized failure, or <see langword="null"/> on success.</summary>
    public RbiApiError? Error { get; }

    /// <summary>Creates a successful result.</summary>
    public static RbiApiResult Success() => new(true, null);

    /// <summary>Creates a failed result.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="error"/> is <see langword="null"/>.</exception>
    public static RbiApiResult Failure(RbiApiError error) =>
        new(false, error ?? throw new ArgumentNullException(nameof(error)));
}

/// <summary>Represents the success or failure of an API operation with a response value.</summary>
/// <typeparam name="T">The successful response type.</typeparam>
public sealed class RbiApiResult<T>
{
    private readonly T? _value;

    private RbiApiResult(bool isSuccess, T? value, RbiApiError? error)
    {
        IsSuccess = isSuccess;
        _value = value;
        Error = error;
    }

    /// <summary>Gets whether the operation completed successfully.</summary>
    public bool IsSuccess { get; }

    /// <summary>Gets the successful value and throws when the operation failed.</summary>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("A failed API result has no value.");

    /// <summary>Gets the normalized failure, or <see langword="null"/> on success.</summary>
    public RbiApiError? Error { get; }

    /// <summary>Creates a successful result.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>; use a non-generic result when no value is returned.</exception>
    public static RbiApiResult<T> Success(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new(true, value, null);
    }

    /// <summary>Creates a failed result.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="error"/> is <see langword="null"/>.</exception>
    public static RbiApiResult<T> Failure(RbiApiError error) =>
        new(false, default, error ?? throw new ArgumentNullException(nameof(error)));
}
