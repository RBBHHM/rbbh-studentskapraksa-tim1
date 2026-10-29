using RBBH.ConnectedParties.Exceptions;
using System.Text.RegularExpressions;

namespace RBBH.ConnectedParties.Helpers.Validators
{
    /// <summary>
    /// Validator za JMBG (Jedinstveni Matični Broj Građana)
    /// JMBG je 13-cifreni broj s definiranom strukturom
    /// </summary>
    public static class JMBGValidator
    {
        private const string JMBGPattern = @"^\d{13}$";

        /// <summary>
        /// Validira JMBG format i strukturu
        /// </summary>
        /// <param name="jmbg">JMBG za validaciju (bez razmaka i znakova)</param>
        /// <returns>True ako je JMBG ispravan, inače baca ValidationException</returns>
        public static bool ValidateJMBG(string jmbg)
        {
            if (string.IsNullOrWhiteSpace(jmbg))
            {
                throw new ValidationException("JMBG", "JMBG ne može biti prazan.");
            }

            jmbg = jmbg.Trim();

            if (!Regex.IsMatch(jmbg, JMBGPattern))
            {
                throw new ValidationException("JMBG", 
                    "JMBG mora biti 13-cifreni broj (npr. 1234567890123).");
            }

            var day = int.Parse(jmbg[..2]);
            var month = int.Parse(jmbg.Substring(2, 2));
            var encodedYear = int.Parse(jmbg.Substring(4, 3));
            var year = encodedYear > 900 ? 1000 + encodedYear : 2000 + encodedYear;
            if (!DateTime.TryParseExact($"{day:D2}.{month:D2}.{year:D4}", "dd.MM.yyyy",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var birthDate)
                || birthDate.Date > DateTime.Today)
            {
                throw new ValidationException("JMBG", "JMBG sadrži neispravan datum rođenja.");
            }

            if (!IsValidControlDigit(jmbg))
            {
                throw new ValidationException("JMBG", 
                    "JMBG nije ispravan - kontrolna cifra nije validna.");
            }

            return true;
        }

        /// <summary>
        /// Provjeri validnost kontrolne cifre JMBG-a
        /// </summary>
        private static bool IsValidControlDigit(string jmbg)
        {
            int[] weights = { 7, 6, 5, 4, 3, 2, 7, 6, 5, 4, 3, 2 };
            int sum = 0;

            for (int i = 0; i < 12; i++)
            {
                sum += int.Parse(jmbg[i].ToString()) * weights[i];
            }

            int remainder = sum % 11;
            int controlDigit = remainder is 0 or 1 ? 0 : 11 - remainder;

            return controlDigit == int.Parse(jmbg[12].ToString());
        }
    }
}
