using NSubstitute;
using Microsoft.AspNetCore.Hosting;
using RBBH.ConnectedParties.IoC.Extensions.Environment;

namespace UnitTests.Extensions
{
    public class EnvironmentExtensionsTests
    {
        [Theory]
        [InlineData("Development", false)]
        [InlineData("Production", true)]
        [InlineData("UAT", false)]
        public void IsProduction_ReturnsExpected(string environmentName, bool expected)
        {
            // Arrange
            var env = Substitute.For<IWebHostEnvironment>();
            env.EnvironmentName.Returns(environmentName);

            // Act
            var result = EnvironmentExtensions.IsProduction(env);

            // Assert
            Assert.Equal(expected, result);
        }
    }
}
