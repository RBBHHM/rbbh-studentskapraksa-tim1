namespace RBBH.ConnectedParties.Exceptions
{
    public class ValidationException : Exception
    {
        public string? Field { get; set; }

        public string ErrorMessage { get; set; }

        public ValidationException(string field, string errorMessage)
            : base($"Validacija nije prošla za polje '{field}': {errorMessage}")
        {
            Field = field;
            ErrorMessage = errorMessage;
        }

        public ValidationException(string errorMessage)
            : base(errorMessage)
        {
            Field = null;
            ErrorMessage = errorMessage;
        }
    }
}
