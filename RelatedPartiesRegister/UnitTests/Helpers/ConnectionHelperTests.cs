using NSubstitute;
using Microsoft.Extensions.Configuration;
using RBBH.ConnectedParties.Helpers.Utils;

namespace UnitTests.Helpers
{
    public class ConnectionHelperTests
    {
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
