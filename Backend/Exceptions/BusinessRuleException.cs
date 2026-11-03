namespace RBBH.ConnectedParties.Exceptions;

/// <summary>Only server-authored safe messages/codes belong in this exception.</summary>
public sealed class BusinessRuleException(int status, string code, string message) : Exception(message)
{
    public BusinessRuleException(int status, string code) : this(status, code, Localization.BackendMessages.Message("errors." + code.Replace('_', '.'))) { }

    public BusinessRuleException(int status, string code, object?[] parameters)
        : this(status, code, Localization.BackendMessages.Message("errors." + code.Replace('_', '.'), parameters))
        => Parameters = parameters;

    public IReadOnlyList<object?> Parameters { get; } = Array.Empty<object?>();
    public int Status { get; } = status;
    public string Code { get; } = code;
}
