using FluentAssertions;
using RBBH.ConnectedParties.Exceptions.Custom;
using RBBH.ConnectedParties.Exceptions.Validations;

namespace UnitTests.Exceptions
{
    public class ResultTests
    {
        [Fact]
        public void Success_WithValue_SetsValueAndNoException()
        {
            // Act
            var result = Result<string>.Success("ok");

            // Assert
            result.IsSuccessful.Should().BeTrue();
            result.Value.Should().Be("ok");
            result.ExceptionType.Should().Be(ResultException.None);
            result.ExceptionMessage.Should().BeNull();
        }

        [Fact]
        public void ValidationError_SetsValidationExceptionTypeAndMessage()
        {
            // Act
            var result = Result<string>.ValidationError("nevalidno");

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.IsValidationError.Should().BeTrue();
            result.ExceptionType.Should().Be(ResultException.ValidationException);
            result.ExceptionMessage.Should().Be("nevalidno");
        }

        [Fact]
        public void NotFoundError_SetsNotFoundExceptionType()
        {
            // Act
            var result = Result<int>.NotFoundError("nije pronađeno");

            // Assert
            result.IsNotFoundError.Should().BeTrue();
            result.IsSuccessful.Should().BeFalse();
            result.ExceptionType.Should().Be(ResultException.NotFoundException);
        }

        [Fact]
        public void InternalServerError_SetsInternalExceptionType()
        {
            // Act
            var result = Result<int>.InternalServerError("greška");

            // Assert
            result.IsInternalServerError.Should().BeTrue();
            result.ExceptionType.Should().Be(ResultException.InternalException);
        }

        [Fact]
        public void ImplicitOperator_FromValue_CreatesSuccessfulResult()
        {
            // Act
            Result<string> result = "vrijednost";

            // Assert
            result.IsSuccessful.Should().BeTrue();
            result.Value.Should().Be("vrijednost");
        }

        [Fact]
        public void ImplicitOperator_FromValidationError_CreatesValidationResult()
        {
            // Act
            Result<string> result = new ValidationError("polje je obavezno");

            // Assert
            result.IsValidationError.Should().BeTrue();
            result.ExceptionMessage.Should().Be("polje je obavezno");
        }

        [Fact]
        public void ImplicitOperator_FromNotFoundError_CreatesNotFoundResult()
        {
            // Act
            Result<string> result = new NotFoundError("nema zapisa");

            // Assert
            result.IsNotFoundError.Should().BeTrue();
            result.ExceptionMessage.Should().Be("nema zapisa");
        }

        [Fact]
        public void CastToError_PreservesMessageAndType()
        {
            // Arrange
            var original = Result<string>.NotFoundError("nije pronađeno");

            // Act
            var casted = original.CastToError<int>();

            // Assert
            casted.IsNotFoundError.Should().BeTrue();
            casted.ExceptionMessage.Should().Be("nije pronađeno");
        }
    }
}
