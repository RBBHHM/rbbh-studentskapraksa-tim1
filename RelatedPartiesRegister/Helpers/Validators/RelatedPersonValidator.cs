using RBBH.ConnectedParties.DL.DTO.RelatedPersons;
using RBBH.ConnectedParties.DL.Entities.RelatedPersons;
using RBBH.ConnectedParties.Helpers.Validators;

namespace RBBH.ConnectedParties.Helpers.Validators;

/// <summary>
/// Poslovna pravila validacije za modul "Povezana fizička lica i članovi porodice".
///
/// Pravila:
/// - Rezident: JMBG je obavezan i mora biti 13 cifara s ispravnom kontrolnom cifrom (<see cref="JMBGValidator"/>).
/// - Nerezident: JMBG nije obavezan; obavezni su broj pasoša i numerički FBA ID.
/// - Ako je JMBG unesen (bez obzira na rezidentnost), mora biti validan.
/// - Ako "Izjava o nepostojanju članova porodice" = DA, unos članova porodice nije dozvoljen.
/// </summary>
public static class RelatedPersonValidator
{
    /// <summary>
    /// Validira identifikacione podatke (JMBG / pasoš / FBA ID) u skladu s rezidentnošću.
    /// Vraća poruku greške ili null ako je validno.
    /// </summary>
    public static string? ValidateIdentification(ResidencyType residency, string? jmbg, string? passportNumber, string? fbaId)
    {
        if (!Enum.IsDefined(residency) || residency is not (ResidencyType.Resident or ResidencyType.NonResident))
            return "Odaberite rezidentnost: rezident ili nerezident.";

        if (residency == ResidencyType.Resident)
        {
            if (string.IsNullOrWhiteSpace(jmbg))
                return "JMBG je obavezan za rezidenta.";

            try
            {
                JMBGValidator.ValidateJMBG(jmbg);
            }
            catch (Exceptions.ValidationException ex)
            {
                return ex.ErrorMessage;
            }
        }
        else // NonResident
        {
            if (string.IsNullOrWhiteSpace(passportNumber) && string.IsNullOrWhiteSpace(fbaId))
                return "Za nerezidenta je potrebno unijeti broj pasoša ili FBA ID.";

            // Ako je JMBG ipak unesen, mora biti validan
            if (!string.IsNullOrWhiteSpace(jmbg))
            {
                try
                {
                    JMBGValidator.ValidateJMBG(jmbg);
                }
                catch (Exceptions.ValidationException ex)
                {
                    return ex.ErrorMessage;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Validira datumski period povezanosti (DateFrom/DateTo).
    /// </summary>
    public static string? ValidateDateRange(DateTime? dateFrom, DateTime? dateTo)
    {
        if (dateFrom.HasValue && dateTo.HasValue && dateTo.Value < dateFrom.Value)
            return "Datum do ne može biti prije datuma od.";

        return null;
    }

    /// <summary>
    /// Provjerava da li je dozvoljen unos člana porodice za dato matično lice,
    /// na osnovu izjave o nepostojanju članova porodice.
    /// </summary>
    public static string? ValidateCanAddFamilyMember(RelatedPerson relatedPerson)
    {
        if (relatedPerson.DeclarationNoFamilyMembers)
            return "Nije moguće dodati člana porodice — odabrana je izjava o nepostojanju članova porodice.";

        return null;
    }

    /// <summary>
    /// Validira identifikacione podatke za DTO kreiranja povezanog fizičkog lica.
    /// </summary>
    public static string? Validate(CreateRelatedPersonDTO dto)
    {
        var personError = ValidatePersonFields(dto.FirstName, dto.LastName, dto.PassportNumber);
        if (personError is not null) return personError;
        var idError = ValidateIdentification(dto.Residency, dto.JMBG, dto.PassportNumber, dto.FBAId);
        if (idError is not null) return idError;

        var nonResidentError = ValidateMainNonResident(dto.Residency, dto.PassportNumber, dto.FBAId);
        if (nonResidentError is not null) return nonResidentError;

        return ValidateBusinessFields(dto.GCCNumber, dto.GCCName, dto.RelationBasis,
            dto.RelationDescription, dto.SpecialRelationBasis, dto.IsIdentifiedStaff, dto.RelatedToPersonId,
            dto.FamilyRelationshipType, dto.DateFrom, dto.DateTo);
    }

    /// <summary>
    /// Validira identifikacione podatke za DTO izmjene povezanog fizičkog lica.
    /// </summary>
    public static string? Validate(UpdateRelatedPersonDTO dto)
    {
        var personError = ValidatePersonFields(dto.FirstName, dto.LastName, dto.PassportNumber);
        if (personError is not null) return personError;
        var idError = ValidateIdentification(dto.Residency, dto.JMBG, dto.PassportNumber, dto.FBAId);
        if (idError is not null) return idError;

        var nonResidentError = ValidateMainNonResident(dto.Residency, dto.PassportNumber, dto.FBAId);
        if (nonResidentError is not null) return nonResidentError;

        return ValidateBusinessFields(dto.GCCNumber, dto.GCCName, dto.RelationBasis,
            dto.RelationDescription, dto.SpecialRelationBasis, dto.IsIdentifiedStaff, dto.RelatedToPersonId,
            dto.FamilyRelationshipType, dto.DateFrom, dto.DateTo);
    }

    /// <summary>
    /// Validira identifikacione podatke za DTO kreiranja člana porodice.
    /// </summary>
    public static string? Validate(CreateFamilyMemberDTO dto)
    {
        return ValidateIdentification(dto.Residency, dto.JMBG, dto.PassportNumber, dto.FBAId);
    }

    /// <summary>
    /// Validira identifikacione podatke za DTO izmjene člana porodice.
    /// </summary>
    public static string? Validate(UpdateFamilyMemberDTO dto)
    {
        return ValidateIdentification(dto.Residency, dto.JMBG, dto.PassportNumber, dto.FBAId);
    }

    private static string? ValidateMainNonResident(ResidencyType residency, string? passportNumber, string? fbaId)
    {
        if (residency != ResidencyType.NonResident) return null;
        if (string.IsNullOrWhiteSpace(passportNumber)) return "Broj pasoša je obavezan za nerezidenta.";
        if (string.IsNullOrWhiteSpace(fbaId)) return "FBA ID je obavezan za nerezidenta.";
        if (!System.Text.RegularExpressions.Regex.IsMatch(fbaId.Trim(), @"^\d{1,10}$"))
            return "FBA ID mora sadržavati najviše 10 cifara.";
        return null;
    }

    private static string? ValidatePersonFields(string? firstName, string? lastName, string? passportNumber)
    {
        const string personNamePattern = @"^[\p{L}][\p{L} '\-]{1,99}$";
        if (string.IsNullOrWhiteSpace(firstName) || !System.Text.RegularExpressions.Regex.IsMatch(firstName.Trim(), personNamePattern))
            return "Ime mora sadržavati najmanje dva slova i ne smije sadržavati brojeve.";
        if (string.IsNullOrWhiteSpace(lastName) || !System.Text.RegularExpressions.Regex.IsMatch(lastName.Trim(), personNamePattern))
            return "Prezime mora sadržavati najmanje dva slova i ne smije sadržavati brojeve.";
        if (!string.IsNullOrWhiteSpace(passportNumber) && !System.Text.RegularExpressions.Regex.IsMatch(passportNumber.Trim(), @"^[A-Za-z0-9][A-Za-z0-9-]{4,19}$"))
            return "Broj pasoša mora sadržavati 5 do 20 slova, cifara ili crtica.";
        return null;
    }

    private static string? ValidateBusinessFields(string? gccNumber, string? gccName, string? relationBasis,
        string? relationDescription, string? specialRelationBasis, bool isIdentifiedStaff, Guid? relatedToPersonId,
        FamilyRelationshipType? familyRelationshipType,
        DateTime? dateFrom, DateTime? dateTo)
    {
        if (string.IsNullOrWhiteSpace(gccNumber) || !gccNumber.All(char.IsDigit))
            return "GCC broj je obavezan i mora sadržavati samo cifre.";
        if (string.IsNullOrWhiteSpace(gccName))
            return "GCC naziv je obavezan.";
        if (string.IsNullOrWhiteSpace(relationBasis))
            return "Osnov povezanosti je obavezan.";
        if (string.IsNullOrWhiteSpace(relationDescription))
            return "Opis osnova povezanosti je obavezan.";
        if (string.IsNullOrWhiteSpace(specialRelationBasis))
            return "Osnov posebnog odnosa je obavezan.";
        if (!IsImmediateFamily(specialRelationBasis) && !isIdentifiedStaff)
            return "Fizičko lice koje nije član uže porodice mora biti identifikovani zaposlenik.";
        if (IsImmediateFamily(specialRelationBasis) && !relatedToPersonId.HasValue)
            return "Za člana uže porodice odaberite fizičko lice s kojim je povezan.";
        if (IsImmediateFamily(specialRelationBasis) && !familyRelationshipType.HasValue)
            return "Za člana uže porodice odaberite vrstu porodičnog odnosa.";
        if (!dateFrom.HasValue)
            return "Datum početka povezanosti je obavezan.";
        if (!dateTo.HasValue)
            return "Datum završetka povezanosti je obavezan.";
        return ValidateDateRange(dateFrom, dateTo);
    }

    public static bool IsImmediateFamily(string? value) =>
        string.Equals(value?.Trim(), "UZA_PORODICA", StringComparison.OrdinalIgnoreCase)
        || string.Equals(value?.Trim(), "Član uže porodice povezanog lica", StringComparison.OrdinalIgnoreCase);
}
