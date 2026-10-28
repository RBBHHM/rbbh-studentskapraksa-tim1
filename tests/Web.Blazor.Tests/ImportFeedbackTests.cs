using Moq;
using RBI.Localization;
using RBBH.RelatedPartiesRegister.Web.Components;
using RBBH.RelatedPartiesRegister.Web.Components.Registry;

namespace Web.Blazor.Tests;

public sealed class ImportFeedbackTests
{
    private static AppText Text()
    {
        var localizer = new Mock<IRbiLocalizer>();
        localizer.Setup(item => item[It.IsAny<string>()]).Returns((string key) => key);
        localizer.Setup(item => item[It.IsAny<string>(), It.IsAny<object?[]>()])
            .Returns((string key, object?[] arguments) => key + ":" + string.Join("|", arguments));
        return new(localizer.Object);
    }

    [Theory]
    [InlineData("Excel predložak mora sadržavati tačno 15 kolona, a pronađeno je 1. Nemojte dodavati...", "import.column-count:15|1")]
    [InlineData("Kolona A ima neispravno zaglavlje. Očekivano je 'Ime', a pronađeno 'private-marker'.", "import.header-error:A|person.first-name")]
    [InlineData("Excel datoteka je prazna. Additional private-marker", "import.empty-file")]
    [InlineData("Excel datoteka nema nijedan red podataka ispod zaglavlja.", "import.no-rows")]
    [InlineData("Excel datoteka sadrži 6000 redova. Dozvoljeno je najviše 5000 redova po uvozu.", "import.row-limit:6000|5000")]
    [InlineData("Red 4: Ime je obavezno.", "import.row-field-error:4|person.first-name|validation.required")]
    [InlineData("Red 5: Fizičko lice s ovim JMBG-om već postoji.", "import.row-field-error:5|person.jmbg|validation.duplicate")]
    [InlineData("Red 6: SQL private-marker stacktrace", "import.row-error:6")]
    [InlineData("SQL private-marker stacktrace", "import.schema-error")]
    public void KnownValidationIsActionableAndUnknownOrSensitiveTextIsNeverEchoed(string error, string expected)
    {
        var result = ImportFeedback.Format(error, Text(), ["person.first-name"]);
        Assert.Equal(expected, result); Assert.DoesNotContain("private-marker", result);
    }
}
