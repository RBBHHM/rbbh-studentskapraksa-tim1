namespace RBBH.ConnectedParties.Helpers.Utils
{
    public class TransactionHelpers
    {
        private static readonly Dictionary<string, string> transactionCodes = new()
        {
            ["110"] = "Uplata",
            ["124"] = "Isplata",
            ["511"] = "Plata"
        };

        public static string ToRBBHTransactionCode(string paymentType)
        {
            transactionCodes.TryGetValue(paymentType, out string? tipTransakcije);

            if (tipTransakcije is null)
            {
                throw new ArgumentOutOfRangeException(nameof(paymentType), "Invalid paymentType provided!");
            }

            return tipTransakcije;
        }

    }
}
