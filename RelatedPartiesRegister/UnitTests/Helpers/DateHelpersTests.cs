
using RBBH.ConnectedParties.Helpers.Utils;

namespace UnitTests.Helpers
{
    public class DateHelpersTests
    {
        [Fact]
        public void ToBADateFormat_ThrowsArgumentOutOfRangeException_WhenDateIsNull()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => DateHelpers.ToBADateFormat(null));
            Assert.Contains("Invalid transactionDate provided!", ex.Message);
        }


        [Fact]
        public void ToBADateFormat_FormatsBADate_WhenDateIsValid()
        {
            // Arrange
            var specifiedDateTime = new DateTime(2025, 9, 24, 14, 30, 0);
            var expected = "24.09.2025";

            // Act
            var actual = DateHelpers.ToBADateFormat(specifiedDateTime);

            // Assert
            Assert.Equal(actual, expected);
        }
    }
}
