using Microsoft.Data.SqlClient;
using NSubstitute;
using Microsoft.Extensions.Configuration;
using RBBH.ConnectedParties.Helpers.Utils;

namespace UnitTests.Helpers
{
    public class ConnectionHelperTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void DataHubOverridesServerAndDatabaseWhileReusingAuthentication(bool integratedSecurity)
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:ServerName"] = "registry",
                ["Database:Name"] = "registrydb",
                ["Database:IntegratedSecurity"] = integratedSecurity.ToString(),
                ["Database:User"] = "fixture-user",
                ["Database:Password"] = "fixture-password"
            }).Build();
            var registry = new SqlConnectionStringBuilder(ConnectionHelper.BuildConnection(configuration));
            var dataHub = new SqlConnectionStringBuilder(ConnectionHelper.BuildConnection(configuration, "datahub", "kod"));
            Assert.Equal("datahub", dataHub.DataSource);
            Assert.Equal("kod", dataHub.InitialCatalog);
            Assert.Equal(integratedSecurity, dataHub.IntegratedSecurity);
            Assert.Equal(registry.UserID, dataHub.UserID);
            Assert.Equal(registry.Password, dataHub.Password);
        }

        [Fact]
        public void BuildConnection_ReturnsExpectedConnectionString()
        {
            // Arrange
            var configuration = Substitute.For<IConfiguration>();
            const string dbName = "mydatabase";

            configuration["Database:ServerName"].Returns("myserver");
            configuration["Database:Name"].Returns(dbName);
            configuration["Database:User"].Returns("myuser");
            configuration["Database:Password"].Returns("mypassword");
            configuration["ASPNETCORE_ENVIRONMENT"].Returns("Development");

            // Act
            var result = ConnectionHelper.BuildConnection(configuration);

            // Assert
            Assert.Contains("Data Source=myserver", result);
            Assert.Contains("Initial Catalog=mydatabase", result);
            Assert.Contains("User ID=myuser", result);
            Assert.Contains("Password=mypassword", result);
            Assert.Contains("Encrypt=True", result);
            Assert.Contains("Trust Server Certificate=True", result);
        }
    }
}
