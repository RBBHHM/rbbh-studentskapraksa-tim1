namespace RBBH.ConnectedParties.Exceptions;

/// <summary>Only server-authored safe messages/codes belong in this exception.</summary>
public sealed class BusinessRuleException(int status, string code, string message) : Exception(message)
{
    public int Status { get; } = status;
    public string Code { get; } = code;
}
