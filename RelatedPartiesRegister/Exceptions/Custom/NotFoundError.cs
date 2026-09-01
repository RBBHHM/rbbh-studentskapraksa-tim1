namespace RBBH.ConnectedParties.Exceptions.Custom
{
    public class NotFoundError : Exception
    {
        public NotFoundError() : base() { }
        public NotFoundError(string message) : base(message) { }
        public NotFoundError(string message, Exception innerException)
          : base(message, innerException) { }
    }
}
