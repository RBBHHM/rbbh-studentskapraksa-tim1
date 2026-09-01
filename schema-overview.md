# SQL Server shema

`ConnectedPartiesDbContext` i migracija `InitialSqlServer` predstavljaju kompletnu aktivnu shemu: transakcije, korisnike i role, šifarnike, audit, fizička i pravna povezana lica, članove porodice, limite, period locks/unlock zahtjeve, ovlaštenja i izvještaje.

API nikada ne pokreće migraciju pri startupu. Idempotentni SQL generira se `dotnet ef migrations script --idempotent`, pregleda u release/DBA procesu i kontrolirano primjenjuje na vanjski SQL Server kroz SSMS.
