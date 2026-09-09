using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using RBBH.ConnectedParties.Helpers.Constants;
using RBBH.ConnectedParties.IoC.Extensions.Authentication;

namespace UnitTests.Authentication;

public sealed class ApplicationAuthorizationPolicyTests
{
    private readonly IAuthorizationService _authorization;

    public ApplicationAuthorizationPolicyTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplicationAuthorization();
        _authorization = services.BuildServiceProvider().GetRequiredService<IAuthorizationService>();
    }

    [Fact]
    public async Task GuestCanReadBusinessAndReportsButCannotMutateOrAdminister()
    {
        var guest = PrincipalFor(ApplicationAccessRoles.Guest);

        await AssertAllowed(guest,
            ApplicationPolicies.PhysicalPersonsRead,
            ApplicationPolicies.LegalPersonsRead,
            ApplicationPolicies.CapitalRead,
            ApplicationPolicies.LimitsRead,
            ApplicationPolicies.CodeListsRead);
        await AssertDenied(guest,
            ApplicationPolicies.PhysicalPersonsCreate,
            ApplicationPolicies.LegalPersonsEdit,
            ApplicationPolicies.CapitalDelete,
            ApplicationPolicies.LimitsCreate,
            ApplicationPolicies.AdministrationRead);
    }

    [Theory]
    [InlineData(ApplicationAccessRoles.PhysicalPersons, ApplicationPolicies.PhysicalPersonsCreate, ApplicationPolicies.PhysicalPersonsEdit, ApplicationPolicies.PhysicalPersonsDelete, ApplicationPolicies.LegalPersonsEdit)]
    [InlineData(ApplicationAccessRoles.LegalPersons, ApplicationPolicies.LegalPersonsCreate, ApplicationPolicies.LegalPersonsEdit, ApplicationPolicies.LegalPersonsDelete, ApplicationPolicies.PhysicalPersonsEdit)]
    [InlineData(ApplicationAccessRoles.Capital, ApplicationPolicies.CapitalCreate, ApplicationPolicies.CapitalEdit, ApplicationPolicies.CapitalDelete, ApplicationPolicies.LimitsEdit)]
    [InlineData(ApplicationAccessRoles.Limits, ApplicationPolicies.LimitsCreate, ApplicationPolicies.LimitsEdit, ApplicationPolicies.LimitsDelete, ApplicationPolicies.CapitalEdit)]
    public async Task ModuleRoleCanCrudOwnModuleButOnlyReadOthers(
        string role,
        string createPolicy,
        string editPolicy,
        string deletePolicy,
        string otherModuleEditPolicy)
    {
        var principal = PrincipalFor(role);

        await AssertAllowed(principal, createPolicy, editPolicy, deletePolicy,
            ApplicationPolicies.PhysicalPersonsRead,
            ApplicationPolicies.LegalPersonsRead,
            ApplicationPolicies.CapitalRead,
            ApplicationPolicies.LimitsRead);
        await AssertDenied(principal, otherModuleEditPolicy, ApplicationPolicies.AdministrationRead);
    }

    [Fact]
    public async Task MultipleRolesCombinePermissionsWithoutAdministration()
    {
        var principal = PrincipalFor(ApplicationAccessRoles.LegalPersons, ApplicationAccessRoles.Capital);

        await AssertAllowed(principal, ApplicationPolicies.LegalPersonsEdit, ApplicationPolicies.CapitalEdit);
        await AssertDenied(principal, ApplicationPolicies.PhysicalPersonsEdit, ApplicationPolicies.AdministrationRead);
    }

    [Fact]
    public async Task AdministratorCanPerformEverySupportedOperation()
    {
        var administrator = PrincipalFor(ApplicationAccessRoles.Administrator);
        var policies = typeof(ApplicationPolicies).GetFields()
            .Where(field => field.IsLiteral && !field.IsInitOnly)
            .Select(field => (string)field.GetRawConstantValue()!)
            .ToArray();

        await AssertAllowed(administrator, policies);
    }

    [Fact]
    public async Task UnauthenticatedPrincipalNeverReceivesBaselineAccess()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            RolePermissionMatrix.Resolve([]).Select(permission => new Claim("permission", permission))));

        await AssertDenied(principal, ApplicationPolicies.PhysicalPersonsRead);
    }

    private static ClaimsPrincipal PrincipalFor(params string[] roles)
    {
        var permissions = RolePermissionMatrix.Resolve(roles);
        return new ClaimsPrincipal(new ClaimsIdentity(
            permissions.Select(permission => new Claim("permission", permission)), "Test"));
    }

    private async Task AssertAllowed(ClaimsPrincipal principal, params string[] policies)
    {
        foreach (var policy in policies)
            Assert.True((await _authorization.AuthorizeAsync(principal, null, policy)).Succeeded, policy);
    }

    private async Task AssertDenied(ClaimsPrincipal principal, params string[] policies)
    {
        foreach (var policy in policies)
            Assert.False((await _authorization.AuthorizeAsync(principal, null, policy)).Succeeded, policy);
    }
}