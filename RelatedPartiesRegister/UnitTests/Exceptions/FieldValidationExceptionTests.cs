using FluentAssertions;
using RBBH.ConnectedParties.Exceptions;

namespace UnitTests.Exceptions
{
    /// <summary>
    /// Testovi za <see cref="ValidationException"/> iz namespace-a Exceptions
    /// (verzija s poljem Field + ErrorMessage koju koriste JMBG/Tax validatori).
    /// </summary>
    public class FieldValidationExceptionTests
    {
        [Fact]
        public void Constructor_WithFieldAndMessage_PopulatesFieldAndComposesMessage()
        {
            // Act
            var ex = new ValidationException("JMBG", "JMBG ne može biti prazan.");

            // Assert
            ex.Field.Should().Be("JMBG");
            ex.ErrorMessage.Should().Be("JMBG ne može biti prazan.");
            ex.Message.Should().Contain("JMBG").And.Contain("JMBG ne može biti prazan.");
        }

        [Fact]
        public void Constructor_WithMessageOnly_LeavesFieldNull()
        {
            // Act
            var ex = new ValidationException("Opšta greška.");

            // Assert
            ex.Field.Should().BeNull();
            ex.ErrorMessage.Should().Be("Opšta greška.");
            ex.Message.Should().Be("Opšta greška.");
        }
    }
}
