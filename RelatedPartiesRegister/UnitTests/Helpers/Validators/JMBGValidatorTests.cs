using FluentAssertions;
using RBBH.ConnectedParties.Exceptions;
using RBBH.ConnectedParties.Helpers.Validators;

namespace UnitTests.Helpers.Validators
{
    public class JMBGValidatorTests
    {
        // Validan datum rođenja i kontrolna cifra (isti realistični format koristi razvojni seed).
        private const string ValidJmbg = "0101990170003";

        [Fact]
        public void ValidateJMBG_WithValidJmbg_ReturnsTrue()
        {
            // Act
            var result = JMBGValidator.ValidateJMBG(ValidJmbg);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void ValidateJMBG_WithSurroundingWhitespace_TrimsAndReturnsTrue()
        {
            // Act
            var result = JMBGValidator.ValidateJMBG($"  {ValidJmbg}  ");

            // Assert
            result.Should().BeTrue();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ValidateJMBG_WithEmptyOrWhitespace_ThrowsValidationException(string? jmbg)
        {
            // Act
            var act = () => JMBGValidator.ValidateJMBG(jmbg!);

            // Assert
            act.Should().Throw<ValidationException>()
                .Which.Field.Should().Be("JMBG");
        }

        [Theory]
        [InlineData("123")]            // prekratak
        [InlineData("12345678901234")] // predugačak (14 cifara)
        [InlineData("11111111111a1")]  // sadrži slovo
        public void ValidateJMBG_WithInvalidFormat_ThrowsValidationException(string jmbg)
        {
            // Act
            var act = () => JMBGValidator.ValidateJMBG(jmbg);

            // Assert
            act.Should().Throw<ValidationException>()
                .WithMessage("*13-cifreni broj*");
        }

        [Fact]
        public void ValidateJMBG_WithInvalidControlDigit_ThrowsValidationException()
        {
            // Arrange — ispravan datum i struktura, ali pogrešna kontrolna cifra.
            var jmbg = "0101990170004";

            // Act
            var act = () => JMBGValidator.ValidateJMBG(jmbg);

            // Assert
            act.Should().Throw<ValidationException>()
                .WithMessage("*kontrolna cifra*");
        }
    }
}
