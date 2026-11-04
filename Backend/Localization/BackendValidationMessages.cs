namespace RBBH.ConnectedParties.Localization;

public static class BackendValidationMessages
{
    public static string Required => BackendMessages.Message("validation.field.required");
    public static string StringLength => BackendMessages.Message("validation.field.string.length");
    public static string MinLength => BackendMessages.Message("validation.field.min.length");
    public static string MaxLength => BackendMessages.Message("validation.field.max.length");
    public static string Range => BackendMessages.Message("validation.field.range");
    public static string Email => BackendMessages.Message("validation.field.email");
    public static string Format => BackendMessages.Message("validation.field.format");
    public static string RegistrationDigits => BackendMessages.Message("validation.registry.registration.digits");
    public static string FbaDigits => BackendMessages.Message("validation.registry.fba.digits");
    public static string NameRequired => BackendMessages.Message("validation.registry.name.required");
    public static string ConnectionRequired => BackendMessages.Message("validation.registry.connection.required");
    public static string FirstNameRequired => BackendMessages.Message("validation.person.first.name.required");
    public static string LastNameRequired => BackendMessages.Message("validation.person.last.name.required");
    public static string ResidencyRequired => BackendMessages.Message("validation.person.residency.required");
}
