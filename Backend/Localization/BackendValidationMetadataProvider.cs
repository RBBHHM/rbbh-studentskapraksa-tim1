using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace RBBH.ConnectedParties.Localization;

/// <summary>Framework validators use the same runtime resources as explicit DTO validators.</summary>
public sealed class BackendValidationMetadataProvider : IValidationMetadataProvider
{
    public void CreateValidationMetadata(ValidationMetadataProviderContext context)
    {
        foreach (var attribute in context.ValidationMetadata.ValidatorMetadata.OfType<ValidationAttribute>())
        {
            if (!string.IsNullOrEmpty(attribute.ErrorMessage) || attribute.ErrorMessageResourceType is not null) continue;
            var property = attribute switch
            {
                RequiredAttribute => nameof(BackendValidationMessages.Required),
                StringLengthAttribute => nameof(BackendValidationMessages.StringLength),
                MinLengthAttribute => nameof(BackendValidationMessages.MinLength),
                MaxLengthAttribute => nameof(BackendValidationMessages.MaxLength),
                RangeAttribute => nameof(BackendValidationMessages.Range),
                EmailAddressAttribute => nameof(BackendValidationMessages.Email),
                RegularExpressionAttribute => nameof(BackendValidationMessages.Format),
                _ => null
            };
            if (property is null) continue;
            attribute.ErrorMessageResourceType = typeof(BackendValidationMessages);
            attribute.ErrorMessageResourceName = property;
        }
    }
}
