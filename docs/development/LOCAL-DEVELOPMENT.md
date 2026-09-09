# Lokalni razvoj

Pokrenuti API i Web u dva terminala prema korijenskom README-u. `pnpm dev:api`
koristi konfigurisani SQL Server i prekida startup ako veza ne uspije. Seedovana
InMemory baza koristi se samo kada SQL postavke nisu unesene. OpenAPI generiranje
zahtijeva pokrenut API. InMemory podaci se ponovo seeduju nakon svakog restartovanja
i nisu trajni.
