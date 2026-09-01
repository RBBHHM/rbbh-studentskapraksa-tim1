using RBBH.ConnectedParties.Helpers.Constants;

namespace RBBH.ConnectedParties.Helpers.Utils
{
    public class DateHelpers
    {
        public static string ToBADateFormat(DateTime? transactionDate)
        {
            if (transactionDate is null)
            {
                throw new ArgumentOutOfRangeException(nameof(transactionDate), "Invalid transactionDate provided!");
            }

            return transactionDate.Value.ToString(AppConstants.BA_DATE_FORMAT);
        }
    }
}
