using RBBH.ConnectedParties.Helpers.Constants;

namespace UnitTests.Constants;

public class ApplicationAccessRolesTests
{
    [Fact]
    public void All_ContainsExactlySixUniqueFunctionalAccesses()
    {
        Assert.Equal(6, ApplicationAccessRoles.All.Length);
        Assert.Equal(6, ApplicationAccessRoles.All.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Equal(
            ["FL", "PL", "kapital", "limiti", "admin", "gost"],
            ApplicationAccessRoles.All);
    }
}
