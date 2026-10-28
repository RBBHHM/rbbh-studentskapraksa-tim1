namespace RBBH.ConnectedParties.Exceptions.Custom
{

    public class InternalServerError : Exception
    {
        public InternalServerError() : base() { }
        public InternalServerError(string message) : base(message) { }
        public InternalServerError(string message, Exception innerException)
          : base(message, innerException) { }
    }
}
