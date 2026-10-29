using System.Collections.Generic;

namespace RBBH.ConnectedParties.Exceptions.Custom
{
    public class ValidationException : Exception
    {
        public IDictionary<string, string[]> Errors { get; } = new Dictionary<string, string[]>();

        public ValidationException() : base() { }

        public ValidationException(string message) : base(message) { }

        public ValidationException(IDictionary<string, string[]> errors)
            : base("One or more validation errors occurred.")
        {
            Errors = errors ?? new Dictionary<string, string[]>();
        }

        public ValidationException(string message, IDictionary<string, string[]> errors)
            : base(message)
        {
            Errors = errors ?? new Dictionary<string, string[]>();
        }
    }
}
