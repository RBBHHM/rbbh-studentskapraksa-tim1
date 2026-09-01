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
                ApplicationException => StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status500InternalServerError
            };
        }
    }
}
