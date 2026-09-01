using RBBH.ConnectedParties.Exceptions.Custom;
namespace RBBH.ConnectedParties.Exceptions.Validations
{
    public enum ResultException
    {
        None,
        ValidationException,
        NotFoundException,
        InternalException
    }

    public class Result<T>
    {
        public T? Value { get; }
        public string? ExceptionMessage { get; }
        public ResultException ExceptionType { get; private set; }

        private Result(T? value, string? exceptionMessage, ResultException status)
        {
            Value = value;
            ExceptionMessage = exceptionMessage;
            ExceptionType = status;
        }

        public static Result<T> Success(T value)
        {
            return new Result<T>(value, null, ResultException.None);
        }

        public static Result<T> ValidationError(string errorMessage, string? invalidProperty = default)
        {
            return new Result<T>(default, errorMessage, ResultException.ValidationException);
        }

        public static Result<T> NotFoundError(string errorMessage, string? invalidProperty = default)
        {
            return new Result<T>(default, errorMessage, ResultException.NotFoundException);
        }
        public static Result<T> InternalServerError(string errorMessage)
        {
            return new Result<T>(default, errorMessage, ResultException.InternalException);
        }

        #region implicit operators

        public static implicit operator Result<T>(T value)
        {
            return new Result<T>(value, null, ResultException.None);
        }

        public static implicit operator Result<T>(ApplicationException ex)
        {
            return new Result<T>(default, ex.Message, ResultException.ValidationException);
        }

        public static implicit operator Result<T>(ValidationError ex)
        {
            return new Result<T>(default, ex.Message, ResultException.ValidationException);
        }

        public static implicit operator Result<T>(NotFoundError ex)
        {
            return new Result<T>(default, ex.Message, ResultException.NotFoundException);
        }

        public static implicit operator Result<T>(InternalServerError ex)
        {
            return new Result<T>(default, ex.Message, ResultException.InternalException);
        }

        public static implicit operator Result<T>(Exception ex)
        {
            return new Result<T>(default, ex.Message, ResultException.InternalException);
        }

        #endregion

        #region Casting Result types

        /// <summary>
        /// Use to Cast errors while preserving Exception Message and Exception Type
        /// E.g. cast Result List U to Result T
        /// </summary>
        /// <typeparam name="O">Original type</typeparam>
        /// <returns>Error result of different type</returns>
        public Result<O> CastToError<O>()
        {
            return new Result<O>(default, ExceptionMessage, ExceptionType);
        }

        #endregion

        [System.Diagnostics.CodeAnalysis.MemberNotNullWhen(true, nameof(Value))]
        public bool IsSuccessful => ExceptionType == ResultException.None;
        public bool IsValidationError => ExceptionType == ResultException.ValidationException;
        public bool IsNotFoundError => ExceptionType == ResultException.NotFoundException;
        public bool IsInternalServerError =>
            ExceptionType != ResultException.None &&
            ExceptionType != ResultException.NotFoundException &&
            ExceptionType != ResultException.ValidationException;
    }

}
