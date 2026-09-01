# Lokalni razvoj

Pokrenuti API i Web u dva terminala prema korijenskom README-u. `pnpm dev:api`
prvo pokušava konfigurisani SQL Server, a u lokalnom Development režimu prelazi
na seedovanu InMemory bazu ako server nije dostupan. OpenAPI generiranje
zahtijeva pokrenut API. InMemory podaci se ponovo seeduju nakon svakog
restartovanja i nisu trajni.
