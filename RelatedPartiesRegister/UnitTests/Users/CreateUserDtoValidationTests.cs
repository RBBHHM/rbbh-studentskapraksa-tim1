using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using RBBH.ConnectedParties.DL.DTO.Users;

namespace UnitTests.Users;

public class CreateUserDtoValidationTests
{
    [Theory]
    [InlineData("hana.mahmutovic@raiffeisengroup.ba")]
    [InlineData("hana.mahmutovic@RAIFFEISENGROUP.BA")]
    public void Email_WithRequiredCorporateDomain_IsValid(string email)
    {
        Validate(NewDto(email)).Should().BeEmpty();
    }

    [Theory]
    [InlineData("hana.mahmutovic@gmail.com")]
    [InlineData("hana.mahmutovic@raiffeisenbank.ba")]
    [InlineData("hana.mahmutovic@sub.raiffeisengroup.ba")]
    public void Email_WithoutExactCorporateDomain_IsRejected(string email)
    {
        Validate(NewDto(email))
            .Should().ContainSingle(error => error.MemberNames.Contains(nameof(CreateUserDTO.Email)))
            .Which.ErrorMessage.Should().Contain("@raiffeisengroup.ba");
    }

    private static CreateUserDTO NewDto(string email) => new()
    {
        Username = "h.mahmutovic",
        FirstName = "Hana",
        LastName = "Mahmutović",
        Email = email
    };

    private static List<ValidationResult> Validate(CreateUserDTO dto)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(dto, new ValidationContext(dto), results, validateAllProperties: true);
        return results;
    }
}
