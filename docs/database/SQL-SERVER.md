# SQL Server, SSMS i EF Core migracije

Aplikacija ima jedan način konfiguracije baze. `ConnectionHelper` iz ovih
vrijednosti pravi sigurni SQL Server connection string:

```text
Database__ServerName=SQL-SERVER-ILI-SERVER,1433
Database__Name=RPR
Database__IntegratedSecurity=false
Database__User=<OCP Secret>
Database__Password=<OCP Secret>
```

Za lokalni SSMS/Windows identitet koristite npr. `localhost\SQLEXPRESS`, naziv
`RPR_Local` i `Database__IntegratedSecurity=true`; tada User/Password nisu
potrebni. Ako su i ServerName i Name prazni, Development koristi seedovanu
InMemory bazu. Ako su vrijednosti unesene, ali SQL nije dostupan zbog mreže,
lokalni Development nakon kratke provjere ispisuje upozorenje i prelazi na
InMemory. Djelimična konfiguracija se odbija jasnom greškom, a UAT/Production
nikada ne koriste fallback i bez SQL konfiguracije ne startaju. `Encrypt=true` je uvijek uključen;
`TrustServerCertificate=true` dozvoljen je automatski samo u Developmentu.

## Promjena modela

EF Core ne smije sam izmišljati migraciju na produkcijskom startupu. Nakon
izmjene entiteta developer generiše, pregleda i commituje verzionisani fajl:

```powershell
$env:Database__ServerName='localhost\SQLEXPRESS'
$env:Database__Name='RPR_Local'
$env:Database__IntegratedSecurity='true'
dotnet ef migrations add NazivPromjene `
  --project RelatedPartiesRegister/RelatedPartiesRegister.csproj `
  --startup-project RelatedPartiesRegister/RelatedPartiesRegister.csproj
```

U Development režimu backend primjenjuje samo još neprimijenjene migracije.
UAT i produkcija imaju `Database__ApplyMigrations=false` i ne izvršavaju DDL iz
aplikacijskog procesa. Ako baza kasni za kodom, servis prekida startup jasnom
porukom umjesto da radi nad nekompatibilnom shemom. Odobreni idempotentni SQL
izvršava se kroz centralni DB repozitorij prema
[DB change workflowu](DB-CHANGE-WORKFLOW.md). Historija ostaje u
`__EFMigrationsHistory`. Demo seed se izvršava samo nad Development InMemory
bazom; produkcijski podaci se unose kroz aplikaciju ili kontrolisani DB proces.
