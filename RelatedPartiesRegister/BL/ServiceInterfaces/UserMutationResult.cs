namespace RBBH.ConnectedParties.BL.ServiceInterfaces;

public enum UserMutationStatus
{
    Success, InvalidRoles, NotFound, FinalAdmin, SelfRoles, SelfDeactivate, SelfDelete, Busy
}

/// <summary>Application outcome; HTTP status codes and localized payloads belong to the controller.</summary>
public sealed record UserMutationResult(UserMutationStatus Status, IReadOnlyList<string>? Roles = null);
