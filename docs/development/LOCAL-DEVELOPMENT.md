# Lokalni razvoj

Pokrenuti API i Web u dva terminala prema korijenskom README-u. `pnpm dev:api`
koristi konfigurisani SQL Server. Aplikacija pri startupu ne primjenjuje migracije,
ne kreira bazu i ne izvršava seed. Migracije i eventualni jednokratni seed pokreću
se eksplicitno prije starta aplikacije. Ako SQL postavke nisu unesene, Development
koristi praznu InMemory bazu. OpenAPI generiranje zahtijeva pokrenut API.
