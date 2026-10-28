using FluentAssertions;
using RBBH.ConnectedParties.Exceptions;
using RBBH.ConnectedParties.Helpers.Validators;

namespace UnitTests.Helpers.Validators
{
    public class TaxNumberValidatorTests
    {
        // Isti algoritam kontrolne cifre kao JMBG -> "1111111111111" je validan.
        private const string ValidTaxNumber = "1111111111111";

        [Fact]
        public void ValidateTaxNumber_WithValidNumber_ReturnsTrue()
        {
            // Act
            var result = TaxNumberValidator.ValidateTaxNumber(ValidTaxNumber);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void ValidateTaxNumber_WithSurroundingWhitespace_TrimsAndReturnsTrue()
        {
            // Act
            var result = TaxNumberValidator.ValidateTaxNumber($"  {ValidTaxNumber}  ");

            // Assert
            result.Should().BeTrue();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ValidateTaxNumber_WithEmptyOrWhitespace_ThrowsValidationException(string? taxNumber)
        {
            // Act
            var act = () => TaxNumberValidator.ValidateTaxNumber(taxNumber!);

            // Assert
            act.Should().Throw<ValidationException>()
                .Which.Field.Should().Be("PoreaskiBroj");
        }

        [Theory]
        [InlineData("123")]
        [InlineData("12345678901234")]
        [InlineData("11111111111x1")]
        public void ValidateTaxNumber_WithInvalidFormat_ThrowsValidationException(string taxNumber)
        {
            // Act
            var act = () => TaxNumberValidator.ValidateTaxNumber(taxNumber);

            // Assert
            act.Should().Throw<ValidationException>()
                .WithMessage("*13-cifreni broj*");
        }

        [Fact]
        public void ValidateTaxNumber_WithInvalidControlDigit_ThrowsValidationException()
        {
            // Arrange
            var taxNumber = "1111111111110";

            // Act
            var act = () => TaxNumberValidator.ValidateTaxNumber(taxNumber);

            // Assert
            act.Should().Throw<ValidationException>()
                .WithMessage("*kontrolna cifra*");
        }
    }
}
