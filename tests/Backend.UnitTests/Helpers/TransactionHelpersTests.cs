using RBBH.ConnectedParties.Helpers.Utils;

namespace UnitTests.Helpers
{
    public class TransactionHelpersTests
    {
        [Fact]
        public void TransactionHelpers_ThrowsArgumentOutOfRangeException_WhenTransactionIsInvalid()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => TransactionHelpers.ToRBBHTransactionCode("ABC"));
            Assert.Contains("Invalid paymentType provided!", ex.Message);
        }


        [Theory]
        [InlineData("110", "Uplata")]
        [InlineData("124", "Isplata")]
        [InlineData("511", "Plata")]
        public void TransactionHelpers_ReturnsCorrectPaymentType(string code, string expected)
        {
            // Act
            var actual = TransactionHelpers.ToRBBHTransactionCode(code);

            // Assert
            Assert.Equal(actual, expected);
        }
    }
}
