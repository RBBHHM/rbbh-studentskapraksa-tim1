# SQL Server shema

Aktivnu shemu predstavljaju `ConnectedPartiesDbContext` i migracije u `Backend/DL/Migrations` (od `InitialSqlServer` do najnovije, uključujući role requests i notifikacije, GCC i veliku izloženost, historiju kapitala, lokalizacijske šifarnike i vlasništvo nad zahtjevima za otključavanje perioda). Entitete čine korisnike i role, šifarnike, audit, fizička i pravna povezana lica, limite, kapital, period locks i izvještaje.

API ne pokreće migraciju pri startupu u UAT/Production. Idempotentni SQL generira se `dotnet ef migrations script --idempotent`, pregleda u release/DBA procesu i kontrolirano primjenjuje na vanjski SQL Server (vidi `docs/database/DB-CHANGE-WORKFLOW.md`). Lokalno se migracije primjenjuju samo eksplicitno (`Database__ApplyMigrations=true` u Developmentu ili `dotnet ef database update`).
