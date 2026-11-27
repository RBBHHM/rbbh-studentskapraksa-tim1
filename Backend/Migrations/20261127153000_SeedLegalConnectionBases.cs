using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RBBH.ConnectedParties.DL.Persistence;

namespace RBBH.ConnectedParties.Api.Migrations;

[DbContext(typeof(ConnectedPartiesDbContext))]
[Migration("20261127153000_SeedLegalConnectionBases")]
public sealed class SeedLegalConnectionBases : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        IF NOT EXISTS (SELECT 1 FROM dbo.CODELIST_DEFINITIONS WHERE NAME = 'OSNOV_POVEZANOSTI')
            INSERT dbo.CODELIST_DEFINITIONS (NAME, DESCRIPTION, IS_ACTIVE, IS_SYSTEM_REQUIRED, CREATED_BY, CREATED_AT,
                COLUMN_NAME_15, COLUMN_NAME_17, COLUMN_NAME_18, COLUMN_POSITION_15, COLUMN_POSITION_17, COLUMN_POSITION_18)
            VALUES ('OSNOV_POVEZANOSTI', 'Osnov povezanosti', 1, 1, 'migration', SYSUTCDATETIME(),
                'VALUE_KEY', 'NAME', 'DESCRIPTION', 1, 2, 3);

        DECLARE @basis bigint = (SELECT ID FROM dbo.CODELIST_DEFINITIONS WHERE NAME = 'OSNOV_POVEZANOSTI');
        DECLARE @packs bigint = (SELECT ID FROM dbo.CODELIST_DEFINITIONS WHERE NAME = 'LANGUAGE_PACKS');
        DECLARE @bases TABLE (Code nvarchar(100), Label nvarchar(4000), Description nvarchar(4000));
        INSERT @bases VALUES
                (N'ZOB-2-U-2', N'Zakon o bankama, član 2, paragraf u, odjeljak 2', N'Lice sa najmanje 5% učešća u banci ili u licima koja su članovi bankarske grupe u kojoj je banka i članovi uže porodice tih lica.'),
                (N'ZOB-2-V-1', N'Zakon o bankama, član 2, paragraf v, odjeljak 1', N'Član bankarske grupe u kojoj je banka.'),
                (N'ZOB-2-V-3', N'Zakon o bankama, član 2, paragraf v, odjeljak 3', N'Pravno lice u kojem banka ima kvalifikovano učešće.'),
                (N'ZOB-2-V-4', N'Zakon o bankama, član 2, paragraf v, odjeljak 4', N'Pravno lice u kojem član uprave, član nadzornog odbora ili prokurista banke i članovi uže porodice tih lica imaju kvalifikovano učešće.'),
                (N'ZOB-2-V-5', N'Zakon o bankama, član 2, paragraf v, odjeljak 5', N'Član nadzornog odbora, član uprave banke, nosilac ključnih funkcija banke, prokurista banke i članovi uže porodice tih lica.'),
                (N'ZOB-2-V-7', N'Zakon o bankama, član 2, paragraf v, odjeljak 7', N'Član organa upravljanja i rukovođenja i prokurista člana bankarske grupe u kojoj je banka i članovi uže porodice tih lica.'),
                (N'ZOB-2-V-8', N'Zakon o bankama, član 2, paragraf v, odjeljak 8', N'Lice koje sa bankom ima zaključen ugovor o radu koji tom licu omogućuje značajan uticaj na poslovanje banke ili određuje naknadu po posebnim kriterijima različitim od standardnih ugovora o radu, pod uslovom da se ne radi o licima iz podtačaka 2) i 5) ove tačke. Obuhvata i drugo lice koje zbog prirode odnosa sa bankom predstavlja lice u sukobu interesa u poslovanju sa bankom ili lice koje ima značajan uticaj na vršenje aktivnosti u banci.');

        UPDATE entry SET ID18 = seed.Description
        FROM dbo.CODELIST_VALUES entry
        JOIN @bases seed ON seed.Code = entry.ID15
        WHERE entry.DEFINITION_ID = @basis AND (NULLIF(LTRIM(RTRIM(entry.ID18)), N'') IS NULL
            OR (entry.CREATED_BY = 'seed' AND entry.MODIFIED_BY IS NULL));

        INSERT dbo.CODELIST_VALUES (DEFINITION_ID, IS_ACTIVE, ID15, ID17, ID18, CREATED_BY, CREATED_AT)
        SELECT @basis, 1, seed.Code, seed.Label, seed.Description, 'migration', SYSUTCDATETIME()
        FROM @bases seed
        WHERE NOT EXISTS (SELECT 1 FROM dbo.CODELIST_VALUES entry WHERE entry.DEFINITION_ID = @basis AND entry.ID15 = seed.Code);

        INSERT dbo.CODELIST_VALUES (DEFINITION_ID, IS_ACTIVE, ID12, ID13, ID14, ID16, ID19, CREATED_BY, CREATED_AT)
        SELECT @packs, 1, seed.LanguageCode, seed.ResourceKey, N'Legal entities', seed.Label, seed.Description, 'migration', SYSUTCDATETIME()
        FROM (VALUES
                (N'BS', N'legal.basis.zob-2-u-2', N'Zakon o bankama, član 2, paragraf u, odjeljak 2', N'Lice sa najmanje 5% učešća u banci ili u licima koja su članovi bankarske grupe u kojoj je banka i članovi uže porodice tih lica.'),
                (N'EN', N'legal.basis.zob-2-u-2', N'Banking Law, Article 2, paragraph u, item 2', N'A person with a holding of at least 5% in the bank or in entities belonging to the banking group of which the bank is a member, and their immediate family.'),
                (N'DE', N'legal.basis.zob-2-u-2', N'Bankengesetz, Artikel 2, Absatz u, Nummer 2', N'Person mit einer Beteiligung von mindestens 5 % an der Bank oder an Mitgliedern der Bankengruppe, der die Bank angehört, sowie deren enge Familienangehörige.'),
                (N'BS', N'legal.basis.zob-2-v-1', N'Zakon o bankama, član 2, paragraf v, odjeljak 1', N'Član bankarske grupe u kojoj je banka.'),
                (N'EN', N'legal.basis.zob-2-v-1', N'Banking Law, Article 2, paragraph v, item 1', N'A member of the banking group to which the bank belongs.'),
                (N'DE', N'legal.basis.zob-2-v-1', N'Bankengesetz, Artikel 2, Absatz v, Nummer 1', N'Mitglied der Bankengruppe, der die Bank angehört.'),
                (N'BS', N'legal.basis.zob-2-v-3', N'Zakon o bankama, član 2, paragraf v, odjeljak 3', N'Pravno lice u kojem banka ima kvalifikovano učešće.'),
                (N'EN', N'legal.basis.zob-2-v-3', N'Banking Law, Article 2, paragraph v, item 3', N'A legal entity in which the bank has a qualifying holding.'),
                (N'DE', N'legal.basis.zob-2-v-3', N'Bankengesetz, Artikel 2, Absatz v, Nummer 3', N'Juristische Person, an der die Bank eine qualifizierte Beteiligung hält.'),
                (N'BS', N'legal.basis.zob-2-v-4', N'Zakon o bankama, član 2, paragraf v, odjeljak 4', N'Pravno lice u kojem član uprave, član nadzornog odbora ili prokurista banke i članovi uže porodice tih lica imaju kvalifikovano učešće.'),
                (N'EN', N'legal.basis.zob-2-v-4', N'Banking Law, Article 2, paragraph v, item 4', N'A legal entity in which a member of the management or supervisory board, a holder of procuration of the bank, or their immediate family holds a qualifying holding.'),
                (N'DE', N'legal.basis.zob-2-v-4', N'Bankengesetz, Artikel 2, Absatz v, Nummer 4', N'Juristische Person, an der ein Vorstands- oder Aufsichtsratsmitglied oder ein Prokurist der Bank oder deren enge Familienangehörige eine qualifizierte Beteiligung halten.'),
                (N'BS', N'legal.basis.zob-2-v-5', N'Zakon o bankama, član 2, paragraf v, odjeljak 5', N'Član nadzornog odbora, član uprave banke, nosilac ključnih funkcija banke, prokurista banke i članovi uže porodice tih lica.'),
                (N'EN', N'legal.basis.zob-2-v-5', N'Banking Law, Article 2, paragraph v, item 5', N'A member of the supervisory board or management board, a holder of a key function, a holder of procuration of the bank, and their immediate family.'),
                (N'DE', N'legal.basis.zob-2-v-5', N'Bankengesetz, Artikel 2, Absatz v, Nummer 5', N'Aufsichtsrats- oder Vorstandsmitglied, Inhaber einer Schlüsselfunktion oder Prokurist der Bank sowie deren enge Familienangehörige.'),
                (N'BS', N'legal.basis.zob-2-v-7', N'Zakon o bankama, član 2, paragraf v, odjeljak 7', N'Član organa upravljanja i rukovođenja i prokurista člana bankarske grupe u kojoj je banka i članovi uže porodice tih lica.'),
                (N'EN', N'legal.basis.zob-2-v-7', N'Banking Law, Article 2, paragraph v, item 7', N'A member of the governing or management bodies or a holder of procuration of a member of the banking group to which the bank belongs, and their immediate family.'),
                (N'DE', N'legal.basis.zob-2-v-7', N'Bankengesetz, Artikel 2, Absatz v, Nummer 7', N'Mitglied eines Leitungsorgans oder Prokurist eines Mitglieds der Bankengruppe, der die Bank angehört, sowie deren enge Familienangehörige.'),
                (N'BS', N'legal.basis.zob-2-v-8', N'Zakon o bankama, član 2, paragraf v, odjeljak 8', N'Lice koje sa bankom ima zaključen ugovor o radu koji tom licu omogućuje značajan uticaj na poslovanje banke ili određuje naknadu po posebnim kriterijima različitim od standardnih ugovora o radu, pod uslovom da se ne radi o licima iz podtačaka 2) i 5) ove tačke. Obuhvata i drugo lice koje zbog prirode odnosa sa bankom predstavlja lice u sukobu interesa u poslovanju sa bankom ili lice koje ima značajan uticaj na vršenje aktivnosti u banci.'),
                (N'EN', N'legal.basis.zob-2-v-8', N'Banking Law, Article 2, paragraph v, item 8', N'A person whose employment agreement with the bank gives them significant influence over its operations or sets remuneration using criteria different from standard employment agreements, provided they are outside sub-items (2) and (5). This also includes another person whose relationship with the bank creates a conflict of interest in dealings with it or who has significant influence over activities at the bank.'),
                (N'DE', N'legal.basis.zob-2-v-8', N'Bankengesetz, Artikel 2, Absatz v, Nummer 8', N'Person, deren Arbeitsvertrag mit der Bank ihr erheblichen Einfluss auf deren Geschäftstätigkeit gewährt oder eine Vergütung nach vom Standard abweichenden Kriterien vorsieht, sofern sie nicht unter die Unterpunkte 2 und 5 fällt. Erfasst ist auch eine weitere Person, deren Beziehung zur Bank einen Interessenkonflikt im Geschäftsverkehr mit ihr begründet oder die erheblichen Einfluss auf Tätigkeiten in der Bank hat.')
        ) seed(LanguageCode, ResourceKey, Label, Description)
        WHERE @packs IS NOT NULL AND NOT EXISTS (
            SELECT 1 FROM dbo.CODELIST_VALUES entry
            WHERE entry.DEFINITION_ID = @packs AND entry.ID12 = seed.LanguageCode AND entry.ID13 = seed.ResourceKey);

        UPDATE dbo.CODELIST_DEFINITIONS SET MODIFIED_AT = SYSUTCDATETIME() WHERE ID = @packs;
        """);

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Seeded references can be used by existing registry records.
    }
}
