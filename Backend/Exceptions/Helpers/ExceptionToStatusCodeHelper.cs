namespace RBBH.ConnectedParties.Exceptions.Helpers
{
    /// <summary>
    /// .NET Exception to HTTP Status Code
    /// </summary>
    internal class ExceptionToStatusCodeHelper
    {
        /// <summary>
        /// Returns appropriate status code based on the provided Exception
        /// </summary>
        /// <param name="exception"></param>
        /// <returns></returns>
        internal static int GetStatus(Exception exception)
        {
            return exception switch
            {
                BusinessRuleException business => business.Status,
                Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException => StatusCodes.Status409Conflict,
                RBBH.ConnectedParties.Helpers.MissingActorIdentityException => StatusCodes.Status401Unauthorized,
                ValidationException => StatusCodes.Status400BadRequest,
                ApplicationException => StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status500InternalServerError
            };
        }
    }
}
