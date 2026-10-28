namespace RBBH.ConnectedParties.Exceptions.Custom
{
    public class ValidationError : Exception
    {
        public ValidationError() : base() { }
        public ValidationError(string message) : base(message) { }
        public ValidationError(string message, Exception innerException)
          : base(message, innerException) { }
    }
}
