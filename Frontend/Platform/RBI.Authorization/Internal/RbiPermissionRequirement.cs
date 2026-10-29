using Microsoft.AspNetCore.Authorization;

namespace RBI.Authorization.Internal;

internal sealed record RbiPermissionRequirement(string Permission) : IAuthorizationRequirement;
