using RBBH.ConnectedParties.Helpers.Constants;

namespace UnitTests.Constants;

public class ApplicationAccessRolesTests
{
    [Fact]
    public void All_ContainsExactlyFourUniqueFunctionalAccesses()
    {
        Assert.Equal(4, ApplicationAccessRoles.All.Count);
        Assert.Equal(4, ApplicationAccessRoles.All.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Equal(
            ["physical-persons", "legal-persons", "limits", "regulatory-reporting"],
            ApplicationAccessRoles.All);
    }
}
