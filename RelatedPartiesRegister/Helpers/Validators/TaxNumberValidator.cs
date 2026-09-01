using RBBH.ConnectedParties.Exceptions;
using System.Text.RegularExpressions;

namespace RBBH.ConnectedParties.Helpers.Validators
{
    public static class TaxNumberValidator
    {
        private const string TaxNumberPattern = @"^\d{13}$";

        public static bool ValidateTaxNumber(string taxNumber)
        {
            if (string.IsNullOrWhiteSpace(taxNumber))
            {
                throw new ValidationException("PoreaskiBroj", "Porezni broj ne može biti prazan.");
            }

            taxNumber = taxNumber.Trim();

            if (!Regex.IsMatch(taxNumber, TaxNumberPattern))
            {
                throw new ValidationException("PoreaskiBroj", 
                    "Porezni broj mora biti 13-cifreni broj (npr. 1234567890123).");
            }

            if (!IsValidControlDigit(taxNumber))
            {
                throw new ValidationException("Kontrolna cifra poreskog broja nije validna.");
            }

            return true;
        }

        private static bool IsValidControlDigit(string taxNumber)
        {
            int[] weights = { 7, 6, 5, 4, 3, 2, 7, 6, 5, 4, 3, 2 };
            int sum = 0;

            for (int i = 0; i < 12; i++)
            {
                sum += int.Parse(taxNumber[i].ToString()) * weights[i];
            }

            int remainder = sum % 11;
            int controlDigit = (11 - remainder) >= 10 ? 0 : 11 - remainder;

            return controlDigit == int.Parse(taxNumber[12].ToString());
        }
    }
}
