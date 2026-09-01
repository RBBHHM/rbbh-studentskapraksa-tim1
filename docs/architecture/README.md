# Arhitektura

Tok zahtjeva je `Browser → React (src/Web) → HTTPS/OpenAPI → .NET API (RelatedPartiesRegister) → EF Core → SQL Server`. React prikazuje podatke i stanje korisničkog interfejsa; API provodi poslovna pravila i jedini pristupa bazi. Keycloak izdaje tokene, frontend ih šalje API-ju, a API validira potpis i claimove. U Development režimu nedostupne vanjske zavisnosti zamjenjuju se eksplicitnim lokalnim adapterima.

Frontend koristi jedan API klijent za JSON i preuzimanje fajlova. OpenAPI je ugovor između Reacta i .NET-a; generisani tipovi smanjuju mogućnost da frontend pošalje pogrešan oblik podataka. Backend centralno vraća sigurne poslovne greške, a tehnički detalj zapisuje samo u log.

Poslovne vrijednosti za padajuće menije dolaze iz SQL Server šifrarnika (`CodeLists` i `CodeListDefinitions`). Nakon izmjene React invalidira samo pogođeni cache upit, pa se tabela, status ili izvještaj odmah ponovo učitavaju bez ručnog osvježavanja cijele stranice. Excel uvoz vraća rezultat po redu, a izvještajni endpoint vraća gotov `.xlsx` fajl kroz isti HTTP klijent.

Autorizacija je funkcionalna, ne sektorska. Keycloak claimovi se svode na četiri dozvoljena pristupa (`physical-persons`, `legal-persons`, `limits`, `regulatory-reporting`), a korisnik može imati više pristupa. API ponovo provjerava pristup; skrivanje kartice ili menija u Reactu nije sigurnosna kontrola.

```text
Korisnik → Keycloak → token sa 1–4 funkcionalna pristupa
                         ↓
React ruta/kartica → jedinstveni HTTP klijent → autorizirani .NET endpoint
                                                ↓
                                      validacija → EF Core → SQL Server
```

Backend OCP artefakt je kontejnerska slika. Frontend IIS artefakt je ZIP statičkih HTML/CSS/JavaScript fajlova i nije .NET aplikacija.
